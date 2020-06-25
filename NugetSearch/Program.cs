using CommandLine;
using GitHubMassUpdater.NugerSearch;
using HtmlAgilityPack;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using SimpleLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHubMassUpdater.NugetSearch
{
    class Program
    {
        private static int githubReposCount = 0;
        private static Logger logger;

        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Args>(args)
                        .WithParsedAsync<Args>(async o =>
                        {
                            var pageNum = o.FirstPageIndex;
                            var lastPageNum = o.LastPageIndex;
                            githubReposCount = 0;
                            logger = new Logger($"{o.OutputFolder}\\log_f{o.FirstPageIndex}_l{o.LastPageIndex}.txt");

                            while (pageNum < lastPageNum + 1)
                            {
                                Console.WriteLine($"PageNum = {pageNum}");
                                Console.WriteLine($"GitHub repos = {githubReposCount}");

                                var repos = await SearchNugetsAsync(pageNum, o.PageSize);
                                if (repos.Any())
                                {
                                    Directory.CreateDirectory(o.OutputFolder);
                                    await File.WriteAllLinesAsync($"{o.OutputFolder}\\page_{pageNum}.txt", repos);
                                }

                                pageNum++;
                            }
                        });

            Console.WriteLine($"Done");
            Console.ReadKey();
        }

        private static async Task<IEnumerable<string>> SearchNugetsAsync(int pageNum, int pageSize)
        {
            var result = new List<string>();

            var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await repository.GetResourceAsync<PackageSearchResource>();
            var searchFilter = new SearchFilter(includePrerelease: true);

            var requestResults = await resource.SearchAsync(
                string.Empty,
                searchFilter,
                skip: pageNum * pageSize,
                take: pageSize,
                NullLogger.Instance,
                CancellationToken.None);

            foreach (IPackageSearchMetadata requestResult in requestResults)
            {
                try
                {
                    var repositoryUrl = requestResult.ProjectUrl?.ToString().ToLower();
                    if (!string.IsNullOrEmpty(repositoryUrl) && repositoryUrl.Contains("github.com"))
                    {
                        result.Add(repositoryUrl);
                        githubReposCount++;

                        Console.WriteLine($"#{githubReposCount} {repositoryUrl}");
                    }
                    else
                    {
                        var web = new HtmlWeb();
                        var htmlDoc = web.Load($"https://www.nuget.org/packages/{requestResult.Identity.Id}/");
                        var nodes = htmlDoc.DocumentNode.SelectNodes("//a[@data-track='outbound-repository-url']");
                        var firstNode = nodes?.FirstOrDefault();
                        if (firstNode != null && firstNode.Attributes.Contains("href"))
                        {
                            repositoryUrl = firstNode.Attributes["href"].Value?.ToLower();
                            if (!string.IsNullOrEmpty(repositoryUrl) && repositoryUrl.Contains("github.com"))
                            {
                                result.Add(repositoryUrl);
                                githubReposCount++;

                                Console.WriteLine($"#{githubReposCount} {repositoryUrl}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    logger.Log(ex);

                    continue;
                }
            }

            return result;
        }
    }
}