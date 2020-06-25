using CommandLine;

namespace GitHubMassUpdater.GitHubUpdate
{
    public class Args
    {
        [Option('i', "input-folder", Required = true, HelpText = "Input folder")]
        public string InputFolder { get; set; }

        [Option('o', "output-file", Required = true, HelpText = "Output file")]
        public string OutputFile { get; set; }

        [Option('a', "auth-token", Required = true, HelpText = "GitHub user auth token")]
        public string AuthToken { get; set; }

        [Option('b', "branch", Required = true, HelpText = "Repos branch to check")]
        public string Branch { get; set; }

        [Option('c', "commit-message", Required = true, HelpText = "Commit message")]
        public string CommitMessage { get; set; }

        [Option('p', "pr-message", Required = true, HelpText = "PR message")]
        public string PRMessage { get; set; }

        [Option('d', "pr-body-filepath", Required = true, HelpText = "PR body file path")]
        public string PRBodyFilePath { get; set; }
    }
}