using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;

namespace static_npm
{
    public class PackageRepository
    {
        public PackageRepository(PackageRepositoryOptions options)
        {
            Options = options;
        }

        private PackageRepositoryOptions Options { get; }

        public void Initialize()
        {
            Directory.CreateDirectory(Options.Location);
            Directory.CreateDirectory(PackagesDir);
        }

        public async Task Add(string packageSource)
        {
            Initialize();

            if (Directory.Exists(packageSource))
            {
                foreach (var packageFile in Directory.EnumerateFiles(packageSource, "*.tgz"))
                {
                    await AddFile(new FileInfo(packageFile));
                }
            }
            else if (File.Exists(packageSource))
            {
                await AddFile(new FileInfo(packageSource));
            }
            else
            {
                throw new ArgumentException($"Provided path is not a file or folder: '{packageSource}'", nameof(packageSource));
            }


            async Task AddFile(FileInfo packageFile)
            {
                var packageJson = await GetPackageJsonAsync(packageFile);

                var packageName = packageJson["name"].ToString();
                var packageVersion = packageJson["version"].ToString();

                //Now that we know the package looks valid, we can copy it to the packages dir (if required)
                if (!SameDirectory(packageFile.DirectoryName, PackagesDir))
                    packageFile.CopyTo(Path.Combine(PackagesDir, packageFile.Name), true);

                await UpdatePackageExtractCache(packageName, packageVersion, packageJson);

                UpdatePackageIndex(packageName);

                static bool SameDirectory(string path1, string path2)
                {
                    return string.Equals(
                                Path.GetFullPath(path1).TrimEnd('\\'),
                                Path.GetFullPath(path2).TrimEnd('\\'),
                                StringComparison.InvariantCultureIgnoreCase)
                        ;
                }
            }
        }

        private async Task UpdatePackageExtractCache(string packageName, string packageVersion, JObject packageJson)
        {
            var packageExtractDir = PackageExtractDir(packageName, packageVersion);

            Directory.CreateDirectory(packageExtractDir);

            await File.WriteAllTextAsync(Path.Combine(packageExtractDir, "package.json"), packageJson.ToString());
        }


        private static async Task<JObject> GetPackageJsonAsync(FileInfo file)
        {
            await using var inStream = file.OpenRead();
            await using var gzipStream = new GZipInputStream(inStream);

            using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);

            var tempDir = GetTemporaryDirectory();

            tarArchive.ExtractContents(tempDir);

            var packageJsonText = await File.ReadAllTextAsync(Path.Combine(tempDir, "package", "package.json"));
            var jsonObj = JsonConvert.DeserializeObject<JObject>(packageJsonText);

            return jsonObj;

            static string GetTemporaryDirectory()
            {
                var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                return tempDirectory;
            }
        }


        private string PackagesDir => Path.Combine(Options.Location, "_packages");

        /// <summary>
        /// The top level extract folder for the specified package. The package.json files for each version
        /// are extracted here as a cache so that they do not need to be re-extracted when recreating the index for
        /// a new package version
        /// added
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        private string PackageExtractDir(string packageName) => Path.Combine(Options.Location, packageName.Replace("/", "%2f"));
        private string PackageExtractDir(string packageName, string packageVersion) => Path.Combine(PackageExtractDir(packageName), packageVersion);


        private void UpdatePackageIndex(string packageName)
        {
            var packageDir = PackageExtractDir(packageName);

            var versionDirs = Directory.EnumerateDirectories(packageDir);

            var versionDetails = 
                versionDirs.Select(versionDir =>
                {
                    var versionString = new DirectoryInfo(versionDir).Name;
                    return (versionDir, versionString, version: SemVersion.Parse(versionString));
                })
                .OrderBy(x => x.version.Major)
                .ThenBy(x => x.version.Minor)
                .ThenBy(x => x.version.Patch)
                .ToImmutableArray();

            var versionedPackageDetails = versionDetails.
                ToImmutableSortedDictionary(
                x => x.versionString,
                x =>
                {
                    var (versionDir, versionString, _) = x;
                    var packageJson = Path.Combine(versionDir, "package.json");
                    var rawJson = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(packageJson));
                    rawJson.Add("dist", JObject.FromObject(new
                    {
                        tarball = PackageUrl(packageName, versionString),
                        shasum = ShaSum(packageName, versionString)
                    }));

                    return rawJson;
                });

            var packageDetails = new Dictionary<string, object>
            {
                { "_id", packageName},
                { "name", packageName},
                { "dist-tags", new {
                    latest = versionDetails.Last().version.ToString()
                }},
            {"versions", versionedPackageDetails}
            }.ToImmutableDictionary();

            var packageDetailsJson = JsonConvert.SerializeObject(packageDetails);

            File.WriteAllText(Path.Combine(packageDir, "index.html"), packageDetailsJson);
        }

        private string ShaSum(string packageName, string versionString)
        {
            var packageFile = Path.Combine(PackagesDir, PackageFileName(packageName, versionString));

            return ComputeShaSum(File.ReadAllBytes(packageFile));

            static string ComputeShaSum(byte[] sourceBytes)
            {
                // Create a SHA256   
                using var sha256Hash = SHA1.Create();

                // ComputeHash - returns byte array  
                var bytes = sha256Hash.ComputeHash(sourceBytes);

                // Convert byte array to a string   
                var builder = new StringBuilder();
                foreach (var t in bytes)
                {
                    builder.Append(t.ToString("x2"));
                }

                return builder.ToString();
            }
        }


        private Uri PackageUrl(string packageName, string version)
        {
            var fileName = PackageFileName(packageName, version);
            var uri = new Uri(Options.BaseUri, $"_packages/{fileName}");

            return uri;
        }

        private static string PackageFileName(string packageName, string version)
        {
            return $"{packageName.Replace("/", "-")}-{version}.tgz";
        }

    }
}
