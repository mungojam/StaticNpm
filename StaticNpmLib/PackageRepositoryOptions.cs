using System;

namespace static_npm
{
    public class PackageRepositoryOptions
    {
        public PackageRepositoryOptions(string location, Uri baseUri)
        {
            Location = location;
            BaseUri = baseUri;
        }

        public Uri BaseUri { get; }

        public string Location { get; }
    }
}