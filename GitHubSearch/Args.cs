using CommandLine;

namespace GitHubMassUpdater.GitHubSearch
{
    public class Args
    {
        [Option('o', "output-folder", Required = true, HelpText = "Output folder")]
        public string OutputFolder { get; set; }

        [Option('i', "input-folder", Required = true, HelpText = "Input folder")]
        public string InputFolder { get; set; }

        [Option('a', "auth-token", Required = true, HelpText = "GitHub user auth token")]
        public string AuthToken { get; set; }

        [Option('b', "branch", Required = true, HelpText = "Repos branch to check")]
        public string Branch { get; set; }
    }
}