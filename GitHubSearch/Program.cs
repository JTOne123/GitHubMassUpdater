using CommandLine;
using Octokit;
using SimpleLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubMassUpdater.GitHubSearch
{
    class Program
    {
        private static GitHubClient github;
        private static Logger logger;

        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Args>(args)
                        .WithParsedAsync<Args>(async o =>
                        {
                            var credentials = new Credentials(o.AuthToken, AuthenticationType.Oauth);
                            github = new GitHubClient(new ProductHeaderValue("SourceLinkAutoUpdater.GitHubSearch")) { Credentials = credentials };

                            var files = Directory.GetFiles(o.InputFolder);
                            var lines = new List<string>();
                            foreach (var filepath in files)
                            {
                                var fileLines = await File.ReadAllLinesAsync(filepath);
                                lines.AddRange(fileLines);
                            }
                            var linesWithoutDuplicates = lines.Distinct();

                            var isContinuesPaging = true;
                            var pageNum = 0;
                            logger = new Logger($"{o.OutputFolder}\\log_p{pageNum}.txt");

                            while (isContinuesPaging)
                            {
                                Console.WriteLine($"PageNum = {pageNum}");
                                var pageSize = 200;
                                var pageItems = linesWithoutDuplicates.Skip(pageNum * pageSize).Take(pageSize);
                                isContinuesPaging = pageItems.Any();

                                if (!isContinuesPaging)
                                    break;

                                var data = await SearchOnThePage(pageItems, o.Branch);
                                pageNum++;

                                var totalPages = linesWithoutDuplicates.Count() / pageSize;
                                Console.WriteLine($"Current page - {pageNum - 1}, total pages - {totalPages}");

                                Directory.CreateDirectory(o.OutputFolder);
                                await File.AppendAllLinesAsync($"{o.OutputFolder}\\result_{pageNum - 1}.txt", data);
                            }
                        });

            Console.WriteLine($"Done");
            Console.ReadKey();
        }

        private static async Task<IEnumerable<string>> SearchOnThePage(IEnumerable<string> pageItems, string branch)
        {
            var repoCollection = new RepositoryCollection();
            foreach (var githubRepo in pageItems)
            {
                if (Uri.IsWellFormedUriString(githubRepo, UriKind.Absolute))
                {
                    var uri = new Uri(githubRepo);

                    if (uri.Segments.Count() < 3)
                        continue;

                    var user = uri.Segments[1].Replace("/", "");
                    var repo = uri.Segments[2].Replace("/", "").Replace(".git", "");

                    Console.WriteLine($"user {user}, repo {repo}");

                    if(string.IsNullOrEmpty(user) || string.IsNullOrEmpty(repo))
                    {
                        Console.WriteLine($"Bad repo - {user}/{repo}");
                        continue;
                    }

                    repoCollection.Add(user, repo);
                }
            }

            var data = await SearchInGitHubAsync(repoCollection, branch);
            return data;
        }

        private static async Task<IEnumerable<string>> SearchInGitHubAsync(RepositoryCollection repos, string branch)
        {
            var results = new List<string>();
            SearchCodeResult files = null;
            try
            {
                await Task.Delay(800);
                files = await github.Search.SearchCode(new SearchCodeRequest
                {
                    Repos = repos,
                    Extensions = new List<string> { ".csproj" },
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                logger.Log(ex);
                return null;
            }

            foreach (var item in files.Items)
            {
                var filepath = item.Path;

                if (filepath.Contains("test", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (filepath.Contains("build", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (filepath.Contains("samples", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (filepath.Contains("examples", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (filepath.Contains("demo", StringComparison.OrdinalIgnoreCase))
                    continue;

                var gitHubUser = item.Repository.Owner.Login;
                var gitHubRepo = item.Repository.Name;

                try
                {
                    await Task.Delay(800);
                    var existingFile = await github.Repository.Content.GetAllContentsByRef(gitHubUser, gitHubRepo, filepath, branch);
                    var firstExistingFile = existingFile.FirstOrDefault();

                    if (firstExistingFile == null)
                        continue;

                    var content = firstExistingFile.Content;

                    if (content.Contains("Sdk=\"Microsoft.NET.Sdk\"", StringComparison.OrdinalIgnoreCase) && !content.Contains("<SymbolPackageFormat>snupkg</SymbolPackageFormat>", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"should be fixed - {filepath}");

                        results.Add($"{gitHubUser},{gitHubRepo},{filepath}");
                    }
                    else
                    {
                        Console.WriteLine($"ready or not supported - {filepath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    logger.Log(ex);
                }
            }

            return results.Distinct();
        }
    }
}