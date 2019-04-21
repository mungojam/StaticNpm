using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
            Directory.CreateDirectory(PackagesPath);
        }

        private string PackagesPath => Path.Combine(Options.Location, "_packages");

        public async Task Add(string packageSource)
        {
            Initialize();

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


            async Task AddFile(string packageFile, bool updateIndex = true)
            {
                var packageFileInfo = new FileInfo(packageFile);

                var packageJson = await GetPackageJsonAsync(packageFileInfo);

                var packageName = packageJson["name"].ToString();
                var packageVersion = packageJson["version"].ToString();

                var packageExtractDir = Path.Combine(PackageDirectory(packageName), packageVersion);

                Directory.CreateDirectory(packageExtractDir);
                

                packageFileInfo.CopyTo(Path.Combine(PackagesPath, packageFileInfo.Name), true);
                await File.WriteAllTextAsync(Path.Combine(packageExtractDir, "package.json"), packageJson.ToString());

                if (updateIndex)
                {
                    UpdatePackageIndex(packageName);
                }
            }
        }

        private string PackageDirectory(string packageName) => Path.Combine(Options.Location, packageName);

        private void UpdatePackageIndex(string packageName)
        {
            var packageDir = PackageDirectory(packageName);

            var versionDirs = Directory.EnumerateDirectories(packageDir);

            var versionedPackageDetails = versionDirs.ToImmutableSortedDictionary(
                x => new DirectoryInfo(x).Name,
                versionDir =>
                {
                    var version = new DirectoryInfo(versionDir).Name;
                    var packageJson = Path.Combine(versionDir, "package.json");
                    var rawJson = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(packageJson));
                    rawJson.Add("dist", JObject.FromObject(new {tarball = PackageUrl(packageName, version)}));

                    return rawJson;
                });

            var packageDetails = new
            {
                _id = "react-redux",
                name = "react-redux",
                versions = versionedPackageDetails
            };

            var packageDetailsJson = JsonConvert.SerializeObject(packageDetails, Formatting.Indented);

            File.WriteAllText(Path.Combine(packageDir, "index.html"), packageDetailsJson);
        }

        private Uri PackageUrl(string packageName, string version)
        {
            var fileName = $"{packageName}-{version}.tgz";
            var uri = new Uri(Options.BaseUri, $"_packages/{fileName}");

            return uri;
        }
    }
}
