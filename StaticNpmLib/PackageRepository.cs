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
            PackagesDir = options.PackagesDir;
            RegistryDir = options.RegistryDir;
            PackageInfoBaseExtractDir = options.PackageInfoBaseExtractDir;
            BasePackagesUri = options.BasePackagesUri;
        }

        private string RegistryDir { get; }
        private Uri BasePackagesUri { get; }
        private string PackagesDir { get; }
        private string PackageInfoBaseExtractDir { get; }

        private void Initialize()
        {
            Directory.CreateDirectory(PackagesDir);
            Directory.CreateDirectory(RegistryDir);
            Directory.CreateDirectory(PackageInfoBaseExtractDir);
        }

        public async Task Add(string source)
        {
            Initialize();

            if (Directory.Exists(source))
            {
                foreach (var packageFile in Directory.EnumerateFiles(source, "*.tgz"))
                {
                    await AddPackage(new FileInfo(packageFile));
                }
            }
            else if (File.Exists(source))
            {
                await AddPackage(new FileInfo(source));
            }
            else
            {
                throw new ArgumentException($"Provided path is not a file or folder: '{source}'", nameof(source));
            }


            async Task AddPackage(FileInfo file)
            {
                var packageJson = await GetPackageJsonAsync(file);

                var packageName = packageJson["name"].ToString();
                var packageVersion = packageJson["version"].ToString();

                //Now that we know the package looks valid, we can copy it to the packages dir (if required)
                if (!SameDirectory(file.DirectoryName, PackagesDir))
                    file.CopyTo(Path.Combine(PackagesDir, file.Name), true);

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

        private async Task UpdatePackageExtractCache(string name, string version, JObject info)
        {
            var packageJsonExtractFile = PackageJsonExtractFile(name, version);
            EnsureParentDirExists(packageJsonExtractFile);

            await File.WriteAllTextAsync(packageJsonExtractFile.FullName, info.ToString());
        }

        private static void EnsureParentDirExists(FileInfo file)
        {
            var parentDirectory = file.Directory ??
                                  throw new ApplicationException(
                                      $"Unexpected issue with path: {file}");

            parentDirectory.Create();
        }

        private static void EnsureParentDirExists(string file)
        {
            EnsureParentDirExists(new FileInfo(file));
        }

        private static async Task<JObject> GetPackageJsonAsync(FileInfo file)
        {
            await using var inStream = file.OpenRead();
            await using var gzipStream = new GZipInputStream(inStream);

            using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);

            var tempDir = GetTemporaryDirectory();

            tarArchive.ExtractContents(tempDir);

            var packageDir = Directory.EnumerateDirectories(tempDir).Single();

            var packageJsonText = await File.ReadAllTextAsync(Path.Combine(packageDir, "package.json"));
            var jsonObj = JsonConvert.DeserializeObject<JObject>(packageJsonText);

            return jsonObj;

            static string GetTemporaryDirectory()
            {
                var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                return tempDirectory;
            }
        }

        /// <summary>
        /// The top level extract folder for the specified package. The package.json files for each version
        /// are extracted here as a cache so that they do not need to be re-extracted when recreating the index for
        /// a new package version
        /// added
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string PackageJsonExtractDir(string name) => Path.Combine(PackageInfoBaseExtractDir, name.Replace("/", "%2f"));

        private FileInfo PackageJsonExtractFile(string name, string version) 
            => new FileInfo(
                Path.Combine(
                    PackageJsonExtractDir(name), 
                    version, 
                    "package.json"
                )
            );

        private string PackageRegistryFile(string name) => Path.Combine(RegistryDir, name);

        private void UpdatePackageIndex(string name)
        {
            var packageDir = PackageJsonExtractDir(name);

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
                        tarball = PackageUrl(name, versionString),
                        shasum = ShaSum(name, versionString)
                    }));

                    return rawJson;
                });

            var packageDetails = new Dictionary<string, object>
            {
                { "_id", name},
                { "name", name},
                { "dist-tags", new {
                    latest = versionDetails.Last().version.ToString()
                }},
            {"versions", versionedPackageDetails}
            }.ToImmutableDictionary();

            var packageDetailsJson = JsonConvert.SerializeObject(packageDetails);

            var registryFile = PackageRegistryFile(name);
            EnsureParentDirExists(registryFile);

            File.WriteAllText(registryFile, packageDetailsJson);
        }

        private string ShaSum(string name, string versionString)
        {
            var packageFile = Path.Combine(PackagesDir, PackageFileName(name, versionString));

            return ComputeShaSum(File.ReadAllBytes(packageFile));

            static string ComputeShaSum(byte[] sourceBytes)
            {   
                using var shaHash = SHA512.Create();

                // ComputeHash - returns byte array  
                var bytes = shaHash.ComputeHash(sourceBytes);

                // Convert byte array to a string   
                var builder = new StringBuilder();
                foreach (var t in bytes)
                {
                    builder.Append(t.ToString("x2"));
                }

                return builder.ToString();
            }
        }


        private Uri PackageUrl(string name, string version)
        {
            var fileName = PackageFileName(name, version);
            var uri = new Uri(BasePackagesUri, fileName);

            return uri;
        }

        private static string PackageFileName(string name, string version)
        {
            return $"{name.Replace("/", "-")}-{version}.tgz";
        }

    }
}
