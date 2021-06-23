using PuppeteerSharp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DeveUnityLicenseActivator.Helpers
{
    public class SelectorAction
    {
        public string Selector { get; set; }
        public Action Action { get; set; }
    }

    public static class PageExtensions
    {
        public static Task<ElementHandle[]> WaitForAllSelectors(this Page page, WaitForSelectorOptions options = null, params string[] selectors)
        {
            return Task.WhenAll(selectors.Select(t => page.WaitForSelectorAsync(t, options)).ToArray());
        }

        public static async Task WaitForAnySelectors(this Page page, WaitForSelectorOptions options = null, params SelectorAction[] selectors)
        {
            while (selectors.Length > 0)
            {
                foreach (var sel in selectors)
                {
                    var result = await page.QuerySelectorAsync(sel.Selector);
                    if (result != null)
                    {
                        sel.Action();
                        return;
                    }
                }
                await Task.Delay(100);
            }
        }

        public static async Task WaitForAnySelectors(this Page page, WaitForSelectorOptions options = null, params string[] selectors)
        {
            while (selectors.Length > 0)
            {
                foreach (var sel in selectors)
                {
                    var result = await page.QuerySelectorAsync(sel);
                    if (result != null)
                    {
                        return;
                    }
                }
                await Task.Delay(100);
            }
        }
    }
}
