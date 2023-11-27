
/* Unmerged change from project 'Spindler (net7.0-windows10.0.19041.0)'
Before:
using System.Diagnostics;
After:
using HtmlAgilityPack;
using Spindler.Services;
using System.Diagnostics;
*/

/* Unmerged change from project 'Spindler (net7.0-maccatalyst)'
Before:
using System.Diagnostics;
After:
using HtmlAgilityPack;
using Spindler.Services;
using System.Diagnostics;
*/

/* Unmerged change from project 'Spindler (net7.0-ios)'
Before:
using System.Diagnostics;
After:
using HtmlAgilityPack;
using Spindler.Services;
using System.Diagnostics;
*/
using HtmlAgilityPack;
using Spindler.Models;
using Spindler.Services;
using Spindler.Services.Web;
using Spindler.Utilities;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.XPath;

namespace Spindler.CustomControls;

public partial class WebScraperBrowser : WebView, IWebService
{
    public static readonly BindableProperty VisibleProperty =
                BindableProperty.Create(nameof(Visible), typeof(bool), typeof(WebScraperBrowser), defaultValue: true);

    private TaskCompletionSource<Result<string>> _htmlCompletion = new();
    private CancellationToken _cancellationToken = new();
    private readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Overrides the visibility options of a normal WebView
    /// </summary>
    public bool Visible
    {
        get => (bool)GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    /// <summary>
    /// Get the html of the current page using a JavaScript Evaluation.
    /// <para>
    /// As this is based on what the Browser has currently loaded there is a possibility that the html
    /// is incomplete, and unwanted. If you want complete html every time use <see cref="GetHtmlFromUrl(string, CancellationToken?)"/>
    /// </para>
    /// </summary>

    /// <returns>Html of the given page</returns>
    public async Task<string> GetHtmlUnsafe()
    {
        string html = await EvaluateJavaScriptAsync(
            "'<html>'+document.getElementsByTagName('html')[0].innerHTML+'</html>';") ?? "";
        html = Regex.Unescape(html);
        return html;
    }

    /// <summary>
    /// Gets the url of the browser
    /// </summary>
    /// <returns>The url in string format</returns>
    public string GetUrl()
    {
        if (Source is null)
            return string.Empty;

        return ((UrlWebViewSource)Source).Url;
    }

    /// <summary>
    /// Compares the WebScraper url to the <paramref name="lastKnownUrl"/>
    /// </summary>
    /// <param name="lastKnownUrl">The last known outside url</param>
    /// <returns>Whether WebScraperBrowser redirected or not</returns>
    public bool IsRedirect(string lastKnownUrl) => GetUrl() != lastKnownUrl;

    public async Task<Result<string>> GetHtmlFromUrl(string url, CancellationToken? token = null)
    {
        Navigated += PageLoaded;
        Stopwatch timer = Stopwatch.StartNew();

        await MainThread.InvokeOnMainThreadAsync(() => Source = url);


        string html;
        // Attempt to bypass cloudflare
        Models.Path cloudflareDetectPath = new("body.no-js > div.main-wrapper > div.main-content > h2#challenge-running");
        try
        {

            string cloudFlareString = string.Empty;
            do
            {
                _htmlCompletion = new TaskCompletionSource<Result<string>>();
                if (timer.Elapsed > TIMEOUT || _cancellationToken.IsCancellationRequested)
                {
                    timer.Reset();
                    Navigated -= PageLoaded;
                    return Result.Error<string>("Website timed out");
                }

                html = await _htmlCompletion.Task switch
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
            timer.Reset();
            Navigated -= PageLoaded;
            return Result.Error<string>("X Path is invalid");
        }
        finally
        {
            timer.Reset();
        }

        return Result.Success(html);
    }

    public WebScraperBrowser()
    {
        InitializeComponent();
    }

    private async void PageLoaded(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result == WebNavigationResult.Cancel)
        {
            _htmlCompletion.SetResult(Result.Error<string>("Headless navigation cancelled"));
            return;
        }
        if (e.Result == WebNavigationResult.Failure)
        {
            _htmlCompletion.SetResult(Result.Error<string>("Headless navigation failed"));
            return;
        }
        string html = await MainThread.InvokeOnMainThreadAsync(GetHtmlUnsafe);
        if (_htmlCompletion.Task.IsCompleted || _cancellationToken.IsCancellationRequested)
        {
            return;
        }
        _htmlCompletion.SetResult(Result.Success(html));
    }

}