using System;

namespace static_npm
{
    public class PackageRepositoryOptions
    {
        public PackageRepositoryOptions(
                string packagesDir, Uri basePackagesUri, 
                string registryDir, string infoBaseExtractDir
                )
        {
            PackagesDir = packagesDir;
            BasePackagesUri = basePackagesUri;
            RegistryDir = registryDir;
            PackageInfoBaseExtractDir = infoBaseExtractDir;
        }

        public Uri BasePackagesUri { get; }

        public string PackagesDir { get; }
        public string RegistryDir { get; }
        public string PackageInfoBaseExtractDir { get; }
    }
}