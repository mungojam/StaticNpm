using System;
using System.Threading.Tasks;
using CommandLine;
using static_npm;

namespace StaticNpm
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {

            try
            {
                await Parser.Default.ParseArguments<AddOptions, InitOptions>(args)
                    .MapResult<AddOptions, InitOptions, Task>(
                        RunAdd,
                        RunInit,
                        errs => throw new InvalidOperationException()
                    );
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }

            return 0;

        }

        private static Task RunInit(InitOptions initOptions)
        {
            PackageRepository().Initialize();
            return Task.CompletedTask;
        }

        private static async Task RunAdd(AddOptions options)
        {
            var packageRepo = PackageRepository();

            await packageRepo.Add(options.Source);
        }

        private static PackageRepository PackageRepository()
        {
            var extractPath = @"C:\Users\mark\Dropbox\Marks\Desktop\static-npm-repo";

            var repoOptions = new PackageRepositoryOptions(extractPath, new Uri("http://localhost:9000"));

            var packageRepo = new PackageRepository(repoOptions);
            return packageRepo;
        }
    }
}
