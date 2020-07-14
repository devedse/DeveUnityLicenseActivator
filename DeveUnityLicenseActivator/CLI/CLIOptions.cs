using CommandLine;

namespace DeveUnityLicenseActivator.CLI
{
    public class CLIOptions
    {
        [Option('e', "email", Required = true, HelpText = "Unity login email address.")]
        public string Email { get; set; }

        [Option('p', "password", Required = true, HelpText = "Unity login password.")]
        public string Password { get; set; }

        [Option('l', "licenseFile", Required = true, HelpText = "Unity .asl license file path.")]
        public string LicenseFile { get; set; }

        [Option('s', "showwindow", Required = false, HelpText = "Shows the google chrome window (don't run headless).")]
        public bool ShowWindow { get; set; }
    }
}
