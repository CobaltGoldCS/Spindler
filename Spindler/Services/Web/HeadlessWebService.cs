using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Utilities;
using System.Diagnostics;
using System.Xml.XPath;

namespace Spindler.Services.Web
{
    public class HeadlessWebService : IWebService
    {
        readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(15);

        WebScraperBrowser WebScraperBrowser { get; set; }
        Stopwatch Timer = new();
        CancellationToken Token = new();
        TaskCompletionSource<Result<string>> HtmlResult { get; set; } = new TaskCompletionSource<Result<string>>();

        public HeadlessWebService(WebScraperBrowser browser)
        {
            WebScraperBrowser = browser;
            WebScraperBrowser.Navigated += WebScraperBrowser_Navigated;
        }

        ~HeadlessWebService()
        {
            WebScraperBrowser.Navigated -= WebScraperBrowser_Navigated;
        }

        public async Task<Result<string>> GetHtmlFromUrl(string url, CancellationToken? token = null)
        {
            token ??= new CancellationToken();
            Token = token.Value;
            Timer = Stopwatch.StartNew();

            await MainThread.InvokeOnMainThreadAsync(() => WebScraperBrowser.Source = url);


            string html;
            // Attempt to bypass cloudflare
            Models.Path cloudflareDetectPath = new("body.no-js > div.main-wrapper > div.main-content > h2#challenge-running");
            try
            {

                string cloudFlareString = string.Empty;
                do
                {
                    HtmlResult = new TaskCompletionSource<Result<string>>();
                    if (Timer.Elapsed > TIMEOUT || Token.IsCancellationRequested)
                    {
                        Timer.Reset();
                        return Result.Error<string>("Website timed out");
                    }

                    html = await HtmlResult.Task switch
                    {
                        Result<string>.Err => string.Empty,
                        Result<string>.Ok okResult => okResult.Value,
                        _ => throw new NotImplementedException()
                    };

                    cloudFlareString = cloudflareDetectPath.Select(html, SelectorType.Text);
                }
                while (!string.IsNullOrEmpty(cloudFlareString) || html.Length < 300);
            }
            catch (XPathException)
            {
                Timer.Reset();
                return Result.Error<string>("X Path is invalid");
            }
            finally
            {
                Timer.Reset();
            }

            return Result.Success(html);
        }

        // [WebView] java.lang.Throwable: A WebView method was called on thread 'Thread-10'. All WebView methods must be called on the same thread. (Expected Looper Looper (main, tid 2) {8ff3b30} called on null, FYI main Looper is Looper (main, tid 2) {8ff3b30})
        async void WebScraperBrowser_Navigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Cancel)
            {
                HtmlResult.SetResult(Result.Error<string>("Headless navigation cancelled"));
                return;
            }
            if (e.Result == WebNavigationResult.Failure)
            {
                HtmlResult.SetResult(Result.Error<string>("Headless navigation failed"));
                return;
            }
            string html = await MainThread.InvokeOnMainThreadAsync(async () => await WebScraperBrowser.GetHtml());
            if (HtmlResult.Task.IsCompleted || Token.IsCancellationRequested)
            {
                return;
            }
            HtmlResult.SetResult(Result.Success(html));
        }
    }
}
