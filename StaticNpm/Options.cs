using System;
using CommandLine;
using static_npm;

namespace StaticNpm
{
    [Verb("add")]
    internal class AddOptions
    {
        [Option('s', "source", Required = true, HelpText = "The package file or folder of files to be processed.")]
        public string Source { get; set; }

        [Option('p', "packages", Required = true, HelpText = "The folder to place the package in")]
        public string PackagesDir { get; set; }

        [Option('u', "url", Required = true, HelpText = "The base URL under which the package files will be hosted")]
        public string BasePackagesUri { get; set; }

        [Option('r', "registry", Required = false, HelpText = "The folder to place the package registry in")]
        public string? RegistryDir { get; set; }

        [Option('i', "info", Required = false, HelpText = "The folder to place the extracted package.json files from each package")]
        public string? InfoBaseExtractDir { get; set; }

        public PackageRepositoryOptions RepositoryOptions =>
            new PackageRepositoryOptions(PackagesDir, new Uri(BasePackagesUri), RegistryDir ?? PackagesDir, InfoBaseExtractDir ?? PackagesDir);

        
    }
}