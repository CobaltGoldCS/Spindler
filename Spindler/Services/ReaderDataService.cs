﻿using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using System.Web;
using HtmlOrError = Spindler.Utilities.Result<string, string>;
using Path = Spindler.Models.Path;

namespace Spindler.Services;

public class ReaderDataService
{
    // This is so other things can access lower level APIs
    /// <summary>
    /// Internal ConfigService
    /// </summary>
    public ConfigService ConfigService { get; private set; }
    /// <summary>
    /// Internal WebService
    /// </summary>
    public WebService WebService { get; private set; }
    /// <summary>
    /// Internal Config
    /// </summary>
    public Config Config { get; private set; }

    public ReaderDataService(Config config)
    {
        Config = config;
        ConfigService = new(config);
        WebService = new();
    }

    /// <summary>
    /// Preload the next and previous urls with valid values into LoadedData
    /// </summary>
    /// <param name="prevUrl">The previous url (will be loaded into index 0)</param>
    /// <param name="nextUrl">The next url (will be loaded into index 1)</param>
    /// <returns>A Task containing a LoadedData array of length 2 [prevdata, nextdata]</returns>
    public async Task<LoadedData?[]> LoadData(string prevUrl, string nextUrl)
    {
        Task<LoadedData?> prevTask = LoadUrl(prevUrl);
        Task<LoadedData?> nextTask = LoadUrl(nextUrl);
        var loaded = await Task.WhenAll(prevTask, nextTask);
        return loaded;
    }

    /// <summary>
    /// Obtain data from <paramref name="url"/>
    /// </summary>
    /// <param name="url">The url to obtain data from</param>
    /// <returns>A LoadedData task holding either a null LoadedData, or a LoadedData with valid values</returns>
    public async Task<LoadedData?> LoadUrl(string url)
    {
        if (!WebService.IsUrl(url))
        {
            return null;
        }
        if (!WebService.HasBaseAddress())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return null;
            WebService.SetBaseAddress(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute));
        }
        try
        {
            url = WebService.MakeAbsoluteUrl(new(url)).ToString();

            HtmlOrError html = await WebService.HtmlOrError(url);
            if (html is HtmlOrError.Error error)
            {
                return MakeError(error.Value);
            }
            return await LoadReaderData(url, html.AsOk().Value);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    /// <summary>
    /// Loads all necessary reader data into a LoadedData object
    /// </summary>
    /// <param name="url">The url used to obtain the reader data (this is not processed in this function)</param>
    /// <param name="html">The html to search for relevant data</param>
    /// <returns>A loaded data object containing the required data from the target website</returns>
    public async Task<LoadedData> LoadReaderData(string url, string html)
    {

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        if (!WebService.HasBaseAddress())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return MakeError(url);
            WebService.SetBaseAddress(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute));
        }

        Task<string> text = Task.Run(() => { return GetContent(doc); });
        LoadedData data = new()
        {
            Text = await text,
            nextUrl = ConfigService.PrettyWrapSelector(html, ConfigService.Selector.NextUrl, type: ConfigService.SelectorType.Link),
            prevUrl = ConfigService.PrettyWrapSelector(html, ConfigService.Selector.PrevUrl, type: ConfigService.SelectorType.Link),
            Title = GetTitle(html),
            currentUrl = url
        };

        return data;
    }

    public string GetContent(HtmlDocument nav)
    {
        Path contentPath = ConfigService.GetPath(ConfigService.Selector.Content);
        HtmlNode node = contentPath.type switch
        {
            Path.Type.Css => nav.QuerySelector(contentPath.path),
            Path.Type.XPath => nav.DocumentNode.SelectSingleNode(contentPath.path),
            _ => throw new NotImplementedException("This path type has not been implemented {ConfigService.GetContent}"),
        };

        if (node == null) return string.Empty;
        if (!node.HasChildNodes)
        {
            return HttpUtility.HtmlDecode(node.InnerText);
        }

        // Node contains child nodes, so we must get the text of each
        StringWriter stringWriter = new();
        string separator = (string)ConfigService.GetExtraConfigs()!.GetValueOrDefault("separator", "\n");
        foreach (HtmlNode child in node.ChildNodes)
        {
            if (child.OriginalName == "br")
            {
                if (child.NextSibling?.OriginalName != "br")
                    stringWriter.Write("\n");
                continue;
            }
            stringWriter.WriteLine($"\t\t{HttpUtility.HtmlDecode(child.InnerText)}{separator}");
        }
        return stringWriter.ToString();
    }


    /// <summary>
    /// Get title from <paramref name="html"/>
    /// </summary>
    /// <param name="html">The html to get the title from</param>
    /// <returns>A title determined by the title selector</returns>
    public string GetTitle(string html)
    {
        return HttpUtility.HtmlDecode(ConfigService.PrettyWrapSelector(html, ConfigService.Selector.Title, type: ConfigService.SelectorType.Text));
    }


    /// <summary>
    /// Make an error output with an optional message
    /// </summary>
    /// <param name="message">An optional error message</param>
    /// <returns>LoadedData in error form</returns>
    private LoadedData MakeError(string message = "") => new LoadedData()
    {
        Title = "afb-4893",
        Text = message,
    };
}