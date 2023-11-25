using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Utilities;
using System.Diagnostics;
using System.Xml.XPath;

namespace Spindler.Services.Web
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

        public async Task<Result<string>> GetHtmlFromUrl(string url, CancellationToken? token = null)
        {


            WebScraperBrowser.Source = url;

            token ??= new CancellationToken();

            // Attempt to bypass cloudflare
            Models.Path cloudflareDetectPath = new Models.Path("body.no-js > div.main-wrapper > div.main-content > h2#challenge-running");
            Stopwatch timer = Stopwatch.StartNew();
            try
            {
                var cloudflareString = cloudflareDetectPath.Select(html, SelectorType.Text);

                while (!string.IsNullOrEmpty(cloudflareString) || html.Length < 300 && !token.Value.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    if (cloudflareString == null)
                        break;

                    cloudflareString = cloudflareDetectPath.Select(html, SelectorType.Text);
                    if (timer.Elapsed >= TimeSpan.FromSeconds(20))
                    {
                        timer.Reset();
                        return Result.Error<string>("Cloudlflare bypass timed out");
                    }
                }
            }
            catch (XPathException)
            {
                return Result.Error<string>("X Path is invalid");
            }
            finally
            {
                timer.Reset();
            }

            if (token.Value.IsCancellationRequested)
            {
                return Result.Error<string>("Cancelled");
            }

            ReturnResult = Result.Success(html);
            html = string.Empty;
            return ReturnResult;
        }


        async void WebScraperBrowser_Navigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Cancel)
            {
                ReturnResult = Result.Error<string>("Headless navigation cancelled");
                return;
            }
            if (e.Result == WebNavigationResult.Failure)
            {
                ReturnResult = Result.Error<string>("Headless navigation failed");
                return;
            }

            html = await WebScraperBrowser.GetHtml();
        }
    }
}
