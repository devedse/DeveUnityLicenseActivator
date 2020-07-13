using CommandLine;

namespace DeveUnityLicenseActivator.CLI
{
    public class CLIOptions
    {
        [Option('e', "email", Required = true, HelpText = "Unity login email address.")]
        public string Email { get; set; }

        [Option('p', "password", Required = true, HelpText = "Unity login password.")]
        public string Password { get; set; }
    }
}
