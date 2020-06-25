using CommandLine;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubMassUpdater.GitHubUpdate
{
    class Program
    {
        private static GitHubClient github;

        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Args>(args)
            .WithParsedAsync<Args>(async o =>
            {
                var prBodyText = File.ReadAllText(o.PRBodyFilePath);

                var files = Directory.GetFiles(o.InputFolder);
                var reposToFix = new Dictionary<(string, string), List<string>>();
                foreach (var filepath in files)
                {
                    var fileLines = await File.ReadAllLinesAsync(filepath);
                    var distinctFileLines = fileLines.Distinct();
                    foreach (var line in distinctFileLines)
                    {
                        var login = line.Split(",")[0];
                        var repo = line.Split(",")[1];
                        var relativeFilePathInRepo = line.Split(",")[2];

                        if (reposToFix.ContainsKey((login, repo)))
                        {
                            reposToFix[(login, repo)].Add(relativeFilePathInRepo);
                        }
                        else
                        {
                            reposToFix[(login, repo)] = new List<string> { relativeFilePathInRepo };
                        }
                    }
                }

                await RunWorkflowAsync(reposToFix, o.AuthToken, o.Branch, o.CommitMessage, o.PRMessage, prBodyText, o.OutputFile);
            });

            Console.WriteLine($"Done");
            Console.ReadKey();
        }

        private static async Task RunWorkflowAsync(Dictionary<(string, string), List<string>> filesInRepos, string authToken, string branch, string commitMessage, string prMessage, string prBody, string outputFilepath)
        {
            var credentials = new Credentials(authToken, AuthenticationType.Oauth);
            github = new GitHubClient(new ProductHeaderValue("GitHubMassUpdater.GitHubUpdate")) { Credentials = credentials };

            foreach (var repoToFix in filesInRepos)
            {
                var login = repoToFix.Key.Item1;
                var repoName = repoToFix.Key.Item2;

                var forkRepo = await CreateForksAsync(login, repoName);

                if (forkRepo == null)
                    continue;

                var isNeedPR = false;
                foreach (var relativeFilePath in repoToFix.Value)
                {
                    try
                    {
                        await Task.Delay(800);
                        var existingFile = await github.Repository.Content.GetAllContentsByRef(forkRepo.Id, relativeFilePath, branch);
                        var gitFile = existingFile.FirstOrDefault();
                        if (gitFile != null)
                        {
                            if (!IsNeedToBeFixed(gitFile.Content))
                                continue;

                            var newContent = GetNewContent(gitFile.Content);

                            if (string.IsNullOrEmpty(newContent))
                                continue;

                            var commit = await CreateCommitAsync(forkRepo, relativeFilePath, commitMessage, newContent, gitFile.Sha);

                            if (commit != null)
                                isNeedPR = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                if (isNeedPR)
                {
                    var result = await CreatePRAsync(login, repoName, forkRepo.Owner.Login, branch, prMessage, prBody);
                    Console.WriteLine(result);
                    await File.AppendAllLinesAsync(outputFilepath, new List<string> { result });
                }
            }
        }

        private static async Task<Repository> CreateForksAsync(string user, string repo)
        {
            try
            {
                await Task.Delay(800);
                return await github.Repository.Forks.Create(user, repo, new NewRepositoryFork());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return null;
            }
        }

        private static async Task<Commit> CreateCommitAsync(Repository repository, string relativeFilepath, string message, string content, string sha)
        {
            try
            {
                var filename = Path.GetFileName(relativeFilepath);
                var updateFileRequest = new UpdateFileRequest($"{message} [{filename}]", content, sha);
                await Task.Delay(800);
                var result = await github.Repository.Content.UpdateFile(repository.Id, relativeFilepath, updateFileRequest);
                return result.Commit;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private static async Task<string> CreatePRAsync(string orignRepoUser, string orignRepoName, string forkRepoUser, string branch, string prMessage, string prBody)
        {
            try
            {
                var pr = new NewPullRequest(prMessage, $"{forkRepoUser}:{branch}", $"{branch}");
                pr.Body = prBody;
                pr.Draft = false;
                await Task.Delay(800);
                var result = await github.PullRequest.Create(orignRepoUser, orignRepoName, pr);

                return $"https://github.com/{orignRepoUser}/{orignRepoName}/pulls";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return null;
            }
        }

        private static bool IsNeedToBeFixed(string content)
        {
            return !content.Contains("<SymbolPackageFormat>snupkg</SymbolPackageFormat>", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetNewContent(string content)
        {
            if (content.Contains("<SymbolPackageFormat>snupkg</SymbolPackageFormat>", StringComparison.OrdinalIgnoreCase))
                return content;

            var newLine = "\n";
            var searchWord = "</PropertyGroup>";
            var searchWordWithParagraph = $"{newLine}  {searchWord}";
            var firstPropertyGroupIndex = content.IndexOf(searchWordWithParagraph);
            var exectSearchWord = searchWordWithParagraph;

            if (firstPropertyGroupIndex == -1)
            {
                firstPropertyGroupIndex = content.IndexOf(searchWord);

                if (firstPropertyGroupIndex == -1)
                    return null;
                else
                    exectSearchWord = searchWord;
            }

            var textToInsert = newLine +
                               "    <PublishRepositoryUrl>true</PublishRepositoryUrl>" + newLine +
                               "    <IncludeSymbols>true</IncludeSymbols>" + newLine +
                               "    <SymbolPackageFormat>snupkg</SymbolPackageFormat>" + newLine +
                               "  </PropertyGroup>" + newLine +
                               newLine +
                               "  <ItemGroup>" + newLine +
                               "    <PackageReference Include=\"Microsoft.SourceLink.GitHub\" Version=\"1.0.0\" PrivateAssets=\"All\" />" + newLine +
                               "  </ItemGroup>";

            content = content.Remove(firstPropertyGroupIndex, exectSearchWord.Length);
            content = content.Insert(firstPropertyGroupIndex, textToInsert);

            return content;
        }
    }
}
