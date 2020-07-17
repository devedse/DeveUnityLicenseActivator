using DeveUnityLicenseActivator.CLI;
using DeveUnityLicenseActivator.Config;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DeveUnityLicenseActivator
{
    public class LicenseActivator
    {
        public async Task<int> Run(CLIOptions cliOptions)
        {
            if (!File.Exists(cliOptions.LicenseFile))
            {
                Console.WriteLine($"Error: File not found: ${cliOptions.LicenseFile}");
                if (cliOptions.LicenseFile.StartsWith("'"))
                {
                    Console.WriteLine("' is not supported for the LicenseFile path. Please use \"");
                }
                throw new FileNotFoundException(cliOptions.LicenseFile);
            }

            var slowerTypeOptions = new TypeOptions()
            {
                Delay = 5
            };

            Console.WriteLine("Downloading browser");
            var refInfo = await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Console.WriteLine($"Obtained Chrome: {refInfo.Revision}");

            string[] args = Array.Empty<string>();

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Console.WriteLine($"Disabling Chrome Sandbox because we're running in a Docker container...");
                args = new string[] { "--no-sandbox", "--disable-setuid-sandbox" };
            }

            using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = !cliOptions.ShowWindow,
                Args = args,
            }))
            {

                Console.WriteLine("Opening page...");

                using (var page = await browser.NewPageAsync())
                {
                    await page.SetViewportAsync(new ViewPortOptions() { Width = 1280, Height = 1024 });
                    await page.GoToAsync(Constants.UnityLicenseUrl);

                    try
                    {
                        //Login
                        await page.WaitForSelectorAsync("#conversations_create_session_form_email");
                        await page.WaitForSelectorAsync("#conversations_create_session_form_password");
                        Console.WriteLine("Logging in...");

                        await page.TypeAsync("#conversations_create_session_form_email", cliOptions.Email, slowerTypeOptions);
                        await page.TypeAsync("#conversations_create_session_form_password", cliOptions.Password, slowerTypeOptions);

                        await page.ClickAsync("#new_conversations_create_session_form input[value='Sign in']");

                        //Upload file
                        await page.WaitForSelectorAsync("#licenseFile");
                        Console.WriteLine("Uploading file...");

                        var fileChooserTask = page.WaitForFileChooserAsync();
                        await page.ClickAsync("#licenseFile");

                        var fileChooser = await fileChooserTask;

                        await fileChooser.AcceptAsync(cliOptions.LicenseFile);

                        await page.ClickAsync("input[value='Next']");

                        //Activate your license
                        var unityPersonalEditionButton = await page.WaitForSelectorAsync("label[for='type_personal']");
                        Console.WriteLine("Selecting edition...");

                        await unityPersonalEditionButton.ClickAsync();

                        var notUseUnityInProfessionalCapacity = await page.WaitForSelectorAsync("label[for='option3']");
                        await notUseUnityInProfessionalCapacity.ClickAsync();

                        var nextButton = await page.WaitForSelectorAsync(".selected input[value='Next']");
                        await nextButton.ClickAsync();

                        //Download license file
                        await page.WaitForSelectorAsync("input[value='Download license file']");
                        Console.WriteLine("Downloading license file...");

                        var downloadManager = new DownloadManager(Directory.GetCurrentDirectory());
                        await downloadManager.SetupPageAsync(page);


                        await page.ClickAsync("input[value='Download license file']");
                        var response = await page.WaitForResponseAsync(r => r.Url.Equals($"https://license.unity3d.com/genesis/activation/download-license", StringComparison.OrdinalIgnoreCase));

                        var data = await response.JsonAsync();
                        var xmlData = data["xml"].ToString();
                        var fileName = data["name"].ToString();

                        File.WriteAllText("Unity_lic.ulf", xmlData);

                        Console.WriteLine($"File 'Unity_lic.ulf' created. Size: {new FileInfo("Unity_lic.ulf").Length}");
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                        var ssstream = await page.ScreenshotStreamAsync();

                        using (var fileStream = File.Create("error.png"))
                        {
                            ssstream.Seek(0, SeekOrigin.Begin);
                            ssstream.CopyTo(fileStream);
                        }
                        return 1;
                    }
                }
            }
        }
    }
}
