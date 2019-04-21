using CommandLine;

namespace StaticNpm
{
    [Verb("add")]
    internal class AddOptions
    {
        [Option('s', "source", Required = true, HelpText = "The package file or folder of files to be processed.")]
        public string Source { get; set; }
    }

    [Verb("init")]
    internal class InitOptions
    {

    }
}