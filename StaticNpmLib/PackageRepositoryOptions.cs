namespace static_npm
{
    public class PackageRepositoryOptions
    {
        public PackageRepositoryOptions(string location)
        {
            Location = location;
        }

        public string Location { get; }
    }
}