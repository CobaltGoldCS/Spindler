using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;

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
            // Rediculous workaround
            Uri uri;
            Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri);
            if (!uri.IsAbsoluteUri && uri.ToString().StartsWith("/"))
                url = uri.ToString().Substring(1);
            var html = await client.GetStringAsync(url);
            return await LoadHTML(url, config, html);
        } catch(System.Net.Http.HttpRequestException e) {
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
    #endregion

    #region client
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
    /// <summary>
    /// Attempt to get text from element pointed to by xpath
    /// </summary>
    /// <param name="nav">The HtmlDocument to get the text from</param>
    /// <param name="path">A string representation of the target's xpath</param>
    /// <returns cref="String">A string containing the target text, or an empty string if nothing is found</returns>
    /// <exception cref="XPathException">If there is any error in the xpath</exception>
    private static string PrettyWrapSelector(HtmlDocument nav, string path)
    {
        try
        {
            string value;
            if (path.StartsWith("/"))
            {
                value = HttpUtility.HtmlDecode(nav.DocumentNode.SelectSingleNode(path)?.CreateNavigator().Value);
            }
            else
            {
                value = HttpUtility.HtmlDecode(cssElementHandler(nav, path));
            }
            return value;
        }
        catch (XPathException e)
        {
            throw new XPathException($"Error on path {path}: {e}");
        }
    }

    private static string cssElementHandler(HtmlDocument nav, string path)
    {

        MatchCollection attributes = Regex.Matches(path, "(.+) \\$(.+)");
        if (attributes.Any())
        {
            var cleanpath = attributes[0].Groups[1].Value;
            var modifier = attributes[0].Groups[2].Value;
            HtmlNode node = nav.QuerySelector(cleanpath);
            return node?.GetAttributeValue(modifier, null);
        }
        return nav.QuerySelector(path)?.CreateNavigator().Value;
    }

    /// <summary>
    /// Smart Get Content that matches given content path using xpath
    /// </summary>
    /// <param name="nav">The HtmlDocument to evaluate for matches</param>
    /// <param name="path">An xpath pointing to the content of the HtmlDocument <paramref name="nav"/></param>
    /// <returns>A String containing the text of the content matched by <paramref name="path"/></returns>
    private static string GetContent(HtmlDocument nav, string path)
    {
        HtmlNode node;
        if (path.StartsWith("/"))
            node = nav.DocumentNode.SelectSingleNode(path);
        else
            node = nav.QuerySelector(path);
        if (node == null) return "";
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerText);
        }
        StringWriter stringWriter = new();
        foreach (HtmlNode child in node.ChildNodes)
        {
            if (child.Name == "br")
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
            nextUrl = PrettyWrapSelector(doc, config.NextUrlPath),
            prevUrl = PrettyWrapSelector(doc, config.PrevUrlPath),
            title   = PrettyWrapSelector(doc, config.TitlePath),
            config = config,
            currentUrl = new Uri(this.client.BaseAddress, url).ToString()
        };

        return data;
    }
}
