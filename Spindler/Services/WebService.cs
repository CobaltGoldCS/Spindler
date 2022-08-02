using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Maui.Controls;
using Spindler.Models;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;
using Path = Spindler.Models.Path;

namespace Spindler.Services;

public class WebService
{
    #region Class Attributes
    private ConfigService pathService;
    private Config config;
    #endregion
    #region Public-Facing APIs
    /// <summary>
    /// The constructor that should be used for WebService
    /// </summary>
    public WebService()
    {
    }

    public WebService(Config config)
    {
        AttachConfig(config);
    }
    public void AttachConfig(Config config)
    {
        this.config = config;
        if (pathService == null)
            pathService = new ConfigService(config);
    }
    /// <summary>
    /// Preload the next and previous urls with valid values into LoadedData
    /// </summary>
    /// <param name="prevUrl">The previous url (will be loaded into index 0)</param>
    /// <param name="nextUrl">The next url (will be loaded into index 1)</param>
    /// <returns>A Task containing a LoadedData array of length 2 [prevdata, nextdata]</returns>
    public async Task<LoadedData[]> PreloadData(string prevUrl, string nextUrl)
    {
        Task<LoadedData> prevTask = PreloadUrl(prevUrl);
        Task<LoadedData> nextTask = PreloadUrl(nextUrl);
        var loaded = await Task.WhenAll(prevTask, nextTask);
        return loaded;
    }

    /// <summary>
    /// Obtain data from <paramref name="url"/>
    /// </summary>
    /// <param name="url">The url to obtain data from</param>
    /// <returns>A LoadedData task holding either a null LoadedData, or a LoadedData with valid values</returns>
    public async Task<LoadedData> PreloadUrl(string url)
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
            url = FormatRelativeUrlProperly(url);

            var html = await client.GetStringAsync(url);
            return await LoadHTML(url, html);
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

    #region HelperFunctions
    private static string FormatRelativeUrlProperly(string url)
    {
        // Ridiculous workaround because HttpClient class doesn't know how to deal with 'improperly' formatted relative urls
        Uri uri;
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri) &&
            !uri.IsAbsoluteUri &&
            uri.ToString().StartsWith("/"))
        {
            url = uri.ToString()[1..];
        }

        return url;
    }

    private async Task<LoadedData> LoadHTML(string url, string html)
    {
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        Task<string> text = Task.Run(() => { return pathService.GetContent(doc); });

        LoadedData data = new()
        {
            text = await text,
            nextUrl = ConfigService.PrettyWrapSelector(doc, pathService.nextpath, type: ConfigService.SelectorType.Link),
            prevUrl = ConfigService.PrettyWrapSelector(doc, pathService.previouspath, type: ConfigService.SelectorType.Link),
            title = pathService.GetTitle(doc),
            config = config,
            currentUrl = new Uri(client.BaseAddress, url).ToString()
        };

        return data;
    }
    #endregion
}
