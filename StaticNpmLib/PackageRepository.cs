using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace static_npm
{
    public class PackageRepository
    {
        public PackageRepository(PackageRepositoryOptions options)
        {
            Options = options;
        }

        private PackageRepositoryOptions Options { get; }


        public static async Task<JObject> GetPackageJsonAsync(FileInfo file)
        {
            await using var inStream = file.OpenRead();
            await using var gzipStream = new GZipInputStream(inStream);

            using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);

            var tempDir = GetTemporaryDirectory();

            tarArchive.ExtractContents(tempDir);

            var packageJsonText = await File.ReadAllTextAsync(Path.Combine(tempDir, "package", "package.json"));
            var jsonObj = JsonConvert.DeserializeObject<JObject>(packageJsonText);

            return jsonObj;
        }


        private static string GetTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public void Initialize()
        {
            Directory.CreateDirectory(Options.Location);
        }

        public bool IsInitialized => Directory.Exists(Options.Location);

        public async Task Add(string packageSource)
        {
            if (!IsInitialized)
            {
                throw new ApplicationException("Repository must be initialized first");
            }

            if (Directory.Exists(packageSource))
            {
                foreach (var packageFile in Directory.EnumerateFiles(packageSource, "*.tgz"))
                {
                    await AddFile(packageFile);
                }
            } 
            else if (File.Exists(packageSource))
            {
                await AddFile(packageSource);
            }
            else
            {
                throw new ArgumentException($"Provided path is not a file or folder: '{packageSource}'", nameof(packageSource));
            }


            async Task AddFile(string packageFile)
            {
                var packageFileInfo = new FileInfo(packageFile);

                var packageJson = await GetPackageJsonAsync(packageFileInfo);

                var packageName = packageJson["name"].ToString();//.RootElement.GetProperty("name").GetString();
                var packageVersion = packageJson["version"].ToString();//RootElement.GetProperty("version").GetString();

                var packageExtractDir = Path.Combine(Options.Location, packageName, packageVersion);

                Directory.CreateDirectory(packageExtractDir);

                packageFileInfo.CopyTo(Path.Combine(packageExtractDir, packageFileInfo.Name), true);
                await File.WriteAllTextAsync(Path.Combine(packageExtractDir, "package.json"), packageJson.ToString());
            }
        }
    }
}
