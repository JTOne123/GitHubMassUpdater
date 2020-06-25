using CommandLine;

namespace GitHubMassUpdater.NugerSearch
{
    public class Args
    {
        [Option('o', "output-folder", Required = true, HelpText = "Output folder")]
        public string OutputFolder { get; set; }

        [Option('f', "first-page", Required = true, HelpText = "First page index of items on the nuget")]
        public int FirstPageIndex { get; set; }
        
        [Option('l', "last-page", Required = true, HelpText = "Last page index of items on the nuget")]
        public int LastPageIndex { get; set; }

        [Option('s', "page-size", Required = true, HelpText = "Page size")]
        public int PageSize { get; set; }
    }
}