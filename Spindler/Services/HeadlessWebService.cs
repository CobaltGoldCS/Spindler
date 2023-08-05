using Spindler.CustomControls;
using Spindler.Utilities;
using System.Diagnostics;
using System.Xml.XPath;

namespace Spindler.Services
{
    public class HeadlessWebService : IWebService
    {
        WebScraperBrowser WebScraperBrowser { get; set; }

        Result<string>? ReturnResult;
        string html = string.Empty;
        public HeadlessWebService(WebScraperBrowser browser)
        {
            WebScraperBrowser = browser;
            WebScraperBrowser.Navigated += WebScraperBrowser_Navigated;
        }

        public async Task<Result<string>> GetHtmlFromUrl(string url)
        {


            WebScraperBrowser.Source = url;


            // Attempt to bypass cloudflare
            Models.Path cloudflareDetectPath = new Models.Path("body.no-js > div.main-wrapper > div.main-content > h2#challenge-running");
            try
            {
                var cloudflareString = ConfigService.PrettyWrapSelector(html, cloudflareDetectPath, ConfigService.SelectorType.Text);
                Stopwatch timer = Stopwatch.StartNew();

                while (!string.IsNullOrEmpty(cloudflareString) || html.Length < 300)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    if (cloudflareString == null)
                        break;

                    cloudflareString = ConfigService.PrettyWrapSelector(html, cloudflareDetectPath, ConfigService.SelectorType.Text);
                    if (timer.Elapsed >= TimeSpan.FromSeconds(20))
                    {
                        return Result<string>.Error("Cloudlflare bypass timed out");
                    }
                }
            }
            catch (XPathException)
            {
                return Result<string>.Error("X Path is invalid");
            }

            ReturnResult = Result<string>.Success(html);
            html = string.Empty;
            return ReturnResult;
        }


        async void WebScraperBrowser_Navigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Cancel)
            {
                ReturnResult = Result<string>.Error("Headless navigation cancelled");
                return;
            }
            if (e.Result == WebNavigationResult.Failure)
            {
                ReturnResult = Result<string>.Error("Headless navigation failed");
                return;
            }

            html = await WebScraperBrowser.GetHtml();
        }
    }
}
