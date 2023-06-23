
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
using Spindler.Services;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Spindler.CustomControls;

public partial class WebScraperBrowser : WebView
{
    public static readonly BindableProperty VisibleProperty =
                BindableProperty.Create(nameof(Visible), typeof(bool), typeof(WebScraperBrowser), defaultValue: true);

    /// <summary>
    /// Overrides the visibility options of a normal WebView
    /// </summary>
    public bool Visible
    {
        get => (bool)GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    /// <summary>
    /// Get the html of the current page using a JavaScript Evaluation
    /// </summary>
    /// <returns>Html of the given page</returns>
    public async Task<string> GetHtml()
    {
        string html = await EvaluateJavaScriptAsync(
            "'<html>'+document.getElementsByTagName('html')[0].innerHTML+'</html>';") ?? "";
        html = Regex.Unescape(html);
        return html;
    }

    /// <summary>
    /// Waits <paramref name="selector"/> is matched by the current page or <paramref name="timeout"/>
    /// </summary>
    /// <param name="selector">The selector to wait for </param>
    /// <param name="retryDelay">Delay before trying to find the selector again</param>
    /// <param name="timeout">The duration before <see cref="WaitUntilValid(Models.Path, TimeSpan, TimeSpan)"/> times out</param>
    /// <returns>Whether or not WaitUntilValid was able to find a matching html sequence</returns>
    public async Task<bool> WaitUntilValid(Models.Path selector, TimeSpan retryDelay, TimeSpan timeout)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < timeout)
        {
            string html = await GetHtml();
            HtmlDocument doc = new();
            doc.LoadHtml(html);
            string textString = ConfigService.PrettyWrapSelector(doc, selector, ConfigService.SelectorType.Text);
            if (textString != string.Empty)
            {
                timer.Stop();
                return true;
            }
            await Task.Delay(retryDelay);
        }
        timer.Stop();
        return false;
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

    public WebScraperBrowser()
    {
        InitializeComponent();
    }

    private WebScraperBrowser(object _)
    {
    }

    public static WebScraperBrowser CreateHeadless()
    {
        return new WebScraperBrowser(new object());
    }


}