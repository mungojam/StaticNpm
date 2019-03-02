using System;
using System.IO;
using System.IO.Compression;
using static_npm;

namespace StaticNpm
{
    class Program
    {
        static void Main(string[] args)
        {
            var zipPath = @"C:\Users\mark\Dropbox\marks\Desktop\react-redux-6.0.1.tgz";

            var extractPath = @"C:\Users\mark\source\repos\static-npm\tt";

            // Normalizes the path.
            extractPath = Path.GetFullPath(extractPath);

            new PackageArchive().GetPackageJson(new FileInfo(zipPath));
        }
    }
}
