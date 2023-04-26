using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using Spindler.Utilities;
using System.Text.RegularExpressions;
using System.Web;
using Path = Spindler.Models.Path;

namespace Spindler.Services;

public partial class ReaderDataService
{
    // This is so other things can access lower level APIs
    /// <summary>
    /// Internal ConfigService
    /// </summary>
    public ConfigService ConfigService { get; private set; }
    /// <summary>
    /// Internal WebService
    /// </summary>
    public IWebService WebService { get; private set; }
    /// <summary>
    /// Internal Config
    /// </summary>
    public Config Config { get; private set; }

    private WebUtilities WebUtilities = new();

    public ReaderDataService(Config config, IWebService webService)
    {
        Config = config;
        ConfigService = new(config);
        WebService = webService;
    }

    public async Task<bool> setConfigFromUrl(string url)
    {
        var html = await WebService.GetHtmlFromUrl(url);

        if (html is Invalid<string>)
            return false;

        var config = await Config.FindValidConfig(url, (html as Ok<string>)!.Value);

        if (config is null)
            return false;

        Config = config;
        ConfigService = new(Config);

        return true;
    }

    /// <summary>
    /// Preload the next and previous urls with valid values into LoadedData
    /// </summary>
    /// <param name="prevUrl">The previous url (will be loaded into index 0)</param>
    /// <param name="nextUrl">The next url (will be loaded into index 1)</param>
    /// <returns>A Task containing a LoadedData array of length 2 [prevdata, nextdata]</returns>
    public async Task<IResult<LoadedData>[]> LoadData(string prevUrl, string nextUrl)
    {
        var prevTask = LoadUrl(WebUtilities.MakeAbsoluteUrl(prevUrl).ToString());
        var nextTask = LoadUrl(WebUtilities.MakeAbsoluteUrl(nextUrl).ToString());
        var loaded = await Task.WhenAll(prevTask, nextTask);
        return loaded;
    }

    /// <summary>
    /// Obtain data from <paramref name="url"/>
    /// </summary>
    /// <param name="url">The url to obtain data from</param>
    /// <returns>A LoadedData task holding either a null LoadedData, or a LoadedData with valid values</returns>
    public async Task<IResult<LoadedData>> LoadUrl(string url)
    {
        if (!WebUtilities.IsUrl(url))
        {
            return new Invalid<LoadedData>(new Error($"'{url}' is not a valid"));
        }
        if (!WebUtilities.HasBaseUrl())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return new Invalid<LoadedData>(new Error($"'{url}' is not a valid"));
            WebUtilities.SetBaseUrl(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute));
        }
        url = WebUtilities.MakeAbsoluteUrl(url).ToString();

        var html = await WebService.GetHtmlFromUrl(url);
        if (html is Invalid<string> error)
        {
            return new Invalid<LoadedData>(error.value);
        }
        return await LoadReaderData(url, (html as Ok<string>)!.Value);
    }

    /// <summary>
    /// Loads all necessary reader data into a LoadedData object
    /// </summary>
    /// <param name="url">The url used to obtain the reader data (this is not processed in this function)</param>
    /// <param name="html">The html to search for relevant data</param>
    /// <returns>A loaded data object containing the required data from the target website</returns>
    public async Task<IResult<LoadedData>> LoadReaderData(string url, string html)
    {

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        if (!WebUtilities.HasBaseUrl())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return new Invalid<LoadedData>(new Error($"'{url}' is not a valid url"));
            WebUtilities.SetBaseUrl(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute));
        }

        Task<string>[] selections = new Task<string>[4];
        selections[0] = Task.Run(() => GetContent(doc));
        selections[1] = Task.Run(() => GetTitle(html));
        selections[2] = Task.Run(() => ConfigService.PrettyWrapSelector(html, ConfigService.Selector.NextUrl, type: ConfigService.SelectorType.Link));
        selections[3] = Task.Run(() => ConfigService.PrettyWrapSelector(html, ConfigService.Selector.PrevUrl, type: ConfigService.SelectorType.Link));
        string[] content = await Task.WhenAll(selections);

        LoadedData data = new()
        {
            Text = content[0],
            Title = content[1],
            nextUrl = content[2],
            prevUrl = content[3],
            currentUrl = url
        };

        return new Ok<LoadedData>(data);
    }

    /// <summary>
    /// Smart Get Content that matches given content path using <see cref="Path"/>
    /// </summary>
    /// <param name="nav">The HtmlDocument to evaluate for matches</param>
    /// <returns>A String containing the text of the content matched by contentpath</returns>
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
            return HttpUtility.HtmlDecode(node.InnerText).Replace("\n", Config.Separator);
        }

        // Node contains child nodes, so we must get the text of each
        StringWriter stringWriter = new();

        foreach (HtmlNode child in node.ChildNodes)
        {
            string innerText = whiteSpaceOnly.Replace(HttpUtility.HtmlDecode(child.InnerText), string.Empty);
            if (innerText.Length == 0)
            {
                if (child.OriginalName == "br" && child.NextSibling?.OriginalName != "br")
                {
                    stringWriter.Write("\n");
                }
                continue;
            }
            stringWriter.Write($"\t\t{HttpUtility.HtmlDecode(child.InnerText).Replace("\n", Config.Separator)}");
            stringWriter.Write(Config.Separator);
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

    private static readonly Regex whiteSpaceOnly = new("^\\s+$", RegexOptions.Multiline);
}
