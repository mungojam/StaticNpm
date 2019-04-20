using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace static_npm
{
    public class PackageArchive
    {

        public async Task<JsonDocument> GetPackageJsonAsync(FileInfo file)
        {
            await using var inStream = file.OpenRead();
            await using var gzipStream = new GZipInputStream(inStream);

            using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);

            var tempDir = GetTemporaryDirectory();

            tarArchive.ExtractContents(tempDir);

            await using var packageJsonStream = File.OpenRead(Path.Combine(tempDir, "package", "package.json"));

            return await JsonDocument.ParseAsync(packageJsonStream);
        }


        public string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
