using DeveUnityLicenseActivator.CLI;
using DeveUnityLicenseActivator.Config;
using DeveUnityLicenseActivator.Helpers;
using DeveVipAccess;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

                        await Task.Delay(500);
                        //await page.TypeAsync("#conversations_create_session_form_email", cliOptions.Email, slowerTypeOptions);
                        //await page.TypeAsync("#conversations_create_session_form_password", cliOptions.Password, slowerTypeOptions);
                        await page.EvaluateExpressionAsync($"document.querySelector('#conversations_create_session_form_email').value = \"{cliOptions.Email}\"");
                        await page.EvaluateExpressionAsync($"document.querySelector('#conversations_create_session_form_password').value = \"{cliOptions.Password}\"");

                        await page.ClickAsync("#new_conversations_create_session_form input[value='Sign in']");

                        await page.WaitForExpressionAsync("document.querySelectorAll('#conversations_tfa_required_form_verify_code, #licenseFile').length > 0 || document.querySelectorAll(\"button[name='conversations_accept_updated_tos_form[accept]'\").length > 0");
                        //await page.WaitForAnySelectors(null, "#conversations_accept_updated_tos_form[accept]", "document.querySelectorAll('#conversations_tfa_required_form_verify_code, #licenseFile').length");

                        await AcceptTosIfRequired(page);

                        var twoFactorBox = await page.QuerySelectorAsync("#conversations_tfa_required_form_verify_code");

                        if (twoFactorBox != null)
                        {
                            //2fa
                            Console.WriteLine("Logging in using 2fa...");

                            var code = VipAccess.CreateCurrentTotpKey(cliOptions.Secret2fa);
                            Console.WriteLine($"Using code: {code}");

                            //await twoFactorBox.TypeAsync(code, slowerTypeOptions);
                            await page.EvaluateExpressionAsync($"document.querySelector('#conversations_tfa_required_form_verify_code').value = \"{code}\"");

                            await page.ClickAsync("input[value='Verify']");
                        }

                        await page.WaitForExpressionAsync("document.querySelectorAll('#licenseFile').length > 0 || document.querySelectorAll(\"button[name='conversations_accept_updated_tos_form[accept]'\").length > 0");
                        await AcceptTosIfRequired(page);

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

                        var curDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                        var outputFile = Path.Combine(curDir, "error.png");

                        Console.WriteLine($"Writing error screenshot to: {outputFile}");
                        using (var fileStream = File.Create(outputFile))
                        {
                            ssstream.Seek(0, SeekOrigin.Begin);
                            ssstream.CopyTo(fileStream);
                        }
                        Console.WriteLine($"Done writing error screenshot to: {outputFile}");
                        return 1;
                    }
                }
            }
        }

        private static async Task AcceptTosIfRequired(Page page)
        {
            //*[@id="new_conversations_accept_updated_tos_form"]/div[2]/button[1]

            //Unity has made updates
            var acceptButton = await page.QuerySelectorAsync("button[name='conversations_accept_updated_tos_form[accept]'");

            if (acceptButton != null)
            {
                Console.WriteLine("Accepting terms");

                await acceptButton.ClickAsync();
            }
        }
    }
}
