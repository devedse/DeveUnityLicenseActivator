using CommandLine;
using DeveUnityLicenseActivator.CLI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeveUnityLicenseActivator.ConsoleApp
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var result = await Parser.Default.ParseArguments<CLIOptions>(args).MapResult((opts) =>
                RunOptionsAndReturnExitCode(opts),
                errs => HandleParseError(errs));

            Console.WriteLine($"Exitcode: {result}");
            return result;
        }

        public static async Task<int> RunOptionsAndReturnExitCode(CLIOptions opts)
        {
            var licenseActivator = new LicenseActivator();
            await licenseActivator.Run(opts);

            return 0;
        }

        public static Task<int> HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine(string.Join(Environment.NewLine, errs));
            return Task.FromResult(1);
        }
    }
}
