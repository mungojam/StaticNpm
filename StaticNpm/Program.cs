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
                await Parser.Default.ParseArguments<AddOptions>(args)
                    .MapResult(
                        RunAdd,
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

        private static async Task RunAdd(AddOptions options)
        {
            var packageRepo = new PackageRepository(options.RepositoryOptions);

            await packageRepo.Add(options.Source);
        }
    }
}
