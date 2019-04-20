using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using static_npm;

namespace StaticNpm
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var zipPath = @"C:\Users\mark\Dropbox\marks\Desktop\react-redux-6.0.1.tgz";

            var extractPath = @"C:\Users\mark\source\repos\static-npm\tt";

            // Normalizes the path.
            extractPath = Path.GetFullPath(extractPath);

            var packageJson = await new PackageArchive().GetPackageJsonAsync(new FileInfo(zipPath));
        }
    }
}
