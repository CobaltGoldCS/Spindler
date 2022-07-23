using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;

namespace Spindler.Services;

public class WebService
{
    #region Public-Facing APIs
    /// <summary>
    /// The constructor that should be used for WebService
    /// </summary>
    public WebService()
    {
    }
    /// <summary>
    /// Preload the next and previous urls with valid values into LoadedData
    /// </summary>
    /// <param name="prevUrl">The previous url (will be loaded into index 0)</param>
    /// <param name="nextUrl">The next url (will be loaded into index 1)</param>
    /// <param name="config">The configuration to use for obtaining data from urls</param>
    /// <returns>A Task containing a LoadedData array of length 2 [prevdata, nextdata]</returns>
    public async Task<LoadedData[]> PreloadData(string prevUrl, string nextUrl, Config config)
    {
        Task<LoadedData> prevTask = PreloadUrl(prevUrl, config);
        Task<LoadedData> nextTask = PreloadUrl(nextUrl, config);
        var loaded = await Task.WhenAll(prevTask, nextTask);
        return loaded;
    }

    /// <summary>
    /// Obtain data from <paramref name="url"/> using <paramref name="config"/>
    /// </summary>
    /// <param name="url">The url to obtain data from</param>
    /// <param name="config">The config containg the areas where data is stored</param>
    /// <returns>A LoadedData task holding either a null LoadedData, or a LoadedData with valid values</returns>
    public async Task<LoadedData> PreloadUrl(string url, Config config)
    {
        if (!IsUrl(url))
        {
            return null;
        }
        if (client.BaseAddress == null)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
            client.BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
        }
        try
        {
            // Ridiculous workaround because HttpClient class doesn't know how to deal with 'improperly' formatted relative urls
            Uri uri;
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri) &&
                !uri.IsAbsoluteUri &&
                uri.ToString().StartsWith("/"))
            {
                url = uri.ToString()[1..];
            }

            var html = await client.GetStringAsync(url);
            return await LoadHTML(url, config, html);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    /// <summary>
    /// Check if <paramref name="url"/> is in a valid http or https format
    /// </summary>
    /// <param name="url">The url to test validity of</param>
    /// <returns>if <paramref name="url"/> is valid</returns>
    public static bool IsUrl(string url)
    {
        Uri uriResult;
        bool created = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uriResult);
        return created && (url.StartsWith("http") || url.StartsWith('/'));
    }

    public static bool IsValidSelector(string path)
    {
        HtmlDocument nav = new HtmlDocument();
        try
        {
            if (path.StartsWith("/"))
            {
                var value = XPathExpression.Compile(path);
            }
            // CssPath Handler
            else
            {
                var value = CssElementHandler(nav, path, SelectorType.Link);
            }
            return true;
        }
        catch (Exception _) when (
        _ is XPathException ||
        _ is ArgumentException ||
        _ is ArgumentNullException ||
        _ is InvalidOperationException)
        {
            return false;
        }
    }
    #endregion

    #region Client
    private HttpClient _client;
    private HttpClient client
    {
        get
        {
            if (_client is null)
            {
                _client = new();
            }
            return _client;
        }
    }
    #endregion

    #region Html Selectors

    private enum SelectorType
    {
        /// <summary>
        /// Denotes a preference for links (target href if possible)
        /// </summary>
        Link,
        /// <summary>
        /// Denotes a preference for text of html elements (target text)
        /// </summary>
        Text,
    }
    /// <summary>
    /// Attempt to get text from element pointed to by xpath
    /// </summary>
    /// <param name="nav">The HtmlDocument to get the text from</param>
    /// <param name="path">A string representation of the target's xpath</param>
    /// <returns cref="string">A string containing the target text, or an empty string if nothing is found</returns>
    /// <exception cref="XPathException">If there is any error in the xpath</exception>
    private static string PrettyWrapSelector(HtmlDocument nav, string path, SelectorType type)
    {
        try
        {
            string value;
            // Xpath Handler
            if (path.StartsWith("/"))
            {
                value = HttpUtility.HtmlDecode(nav.DocumentNode.SelectSingleNode(path)?.CreateNavigator().Value);
            }
            // CssPath Handler
            else
            {
                value = HttpUtility.HtmlDecode(CssElementHandler(nav, path, type));
            }
            return value;
        }
        catch (XPathException e)
        {
            throw new XPathException($"Error on path {path}: {e}");
        }
    }

    private static string CssElementHandler(HtmlDocument nav, string path, SelectorType type)
    {

        MatchCollection attributes = Regex.Matches(path, "(.+) \\$(.+)");
        if (attributes.Any())
        {
            var cleanpath = attributes[0].Groups[1].Value;
            var modifier = attributes[0].Groups[2].Value;
            HtmlNode node = nav.QuerySelector(cleanpath);
            return node?.GetAttributeValue(modifier, null);
        }
        return type switch
        {
            SelectorType.Text => nav.QuerySelector(path)?.CreateNavigator().Value,
            SelectorType.Link => nav.QuerySelector(path)?.GetAttributeValue("href", null),
            _ => throw new NotImplementedException("This selectortype is not implemented"),
        };
    }

    /// <summary>
    /// Smart Get Content that matches given content path using xpath
    /// </summary>
    /// <param name="nav">The HtmlDocument to evaluate for matches</param>
    /// <param name="path">An xpath pointing to the content of the HtmlDocument <paramref name="nav"/></param>
    /// <returns>A String containing the text of the content matched by <paramref name="path"/></returns>
    private static string GetContent(HtmlDocument nav, string path)
    {
        HtmlNode node = path.StartsWith("/") ? nav.DocumentNode.SelectSingleNode(path) : nav.QuerySelector(path);

        if (node == null) return String.Empty;
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerText);
        }

        StringWriter stringWriter = new();
        foreach (HtmlNode child in node.ChildNodes)
        {
            if (child.OriginalName == "br")
            {
                stringWriter.Write("\n");
                continue;
            }
            stringWriter.WriteLine($"\t\t{HttpUtility.HtmlDecode(child.InnerText)}");
        }
        return stringWriter.ToString();
    }
    #endregion

    /// <summary>
    /// Obtain data defined by <paramref name="config"/> inside of <paramref name="html"/>
    /// </summary>
    /// <param name="url">The url that links to <paramref name="html"/></param>
    /// <param name="config">The config containing definitions for content within <paramref name="html"/></param>
    /// <param name="html">The text to scan for matches within</param>
    /// <returns>
    /// A LoadedData containing the values matched by <paramref name="config"/>,
    /// or null for everything except config and currentUrl
    /// </returns>
    private async Task<LoadedData> LoadHTML(string url, Config config, string html)
    {
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        if (string.IsNullOrWhiteSpace(config.TitlePath))
            config.TitlePath = "//title";

        Task<string> text = Task.Run(() => { return GetContent(doc, config.ContentPath); });

        LoadedData data = new()
        {
            text = await text,
            nextUrl = PrettyWrapSelector(doc, config.NextUrlPath, type: SelectorType.Link),
            prevUrl = PrettyWrapSelector(doc, config.PrevUrlPath, type: SelectorType.Link),
            title = HttpUtility.HtmlDecode(PrettyWrapSelector(doc, config.TitlePath, type: SelectorType.Text)),
            config = config,
            currentUrl = new Uri(this.client.BaseAddress, url).ToString()
        };

        return data;
    }
}
