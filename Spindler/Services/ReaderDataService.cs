using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Spindler.Models;
using Spindler.Utilities;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;
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

    private Task<Result<LoadedData>>[] LoadingDataTask = new Task<Result<LoadedData>>[] 
    { 
        Task.FromResult(Result.Error<LoadedData>("Uninitialized Data")),
        Task.FromResult(Result.Error<LoadedData>("Uninitialized Data"))
    };

    public enum UrlType
    {
        Previous = 0,
        Next = 1,
    }

    public ReaderDataService(Config config, IWebService webService)
    {
        Config = config;
        ConfigService = new(config);
        WebService = webService;
    }

    public void InvalidatePreloadedData()
    {
        LoadingDataTask[0] = Task.FromResult(Result.Error<LoadedData>("Uninitialized Data"));
        LoadingDataTask[1] = Task.FromResult(Result.Error<LoadedData>("Uninitialized Data"));
    }

    public async Task<Result<LoadedData>> GetLoadedData(UrlType urlType, LoadedData currentData)
    {

        var targetData = await LoadingDataTask[(int)urlType];
        // Attempt to reload the return data if it fails the first time
        Result<LoadedData> returnData = targetData switch
        {
            Result<LoadedData>.Ok => targetData,
            Result<LoadedData>.Err => await LoadUrl(urlType == UrlType.Previous ? currentData.prevUrl : currentData.nextUrl),
            _ => throw new NotImplementedException("Result must be ok or invalid")
        };

        if (returnData is Result<LoadedData>.Err)
        {
            return returnData;
        }

        var data = (returnData as Result<LoadedData>.Ok)!.Value;

        LoadingDataTask[(int)urlType] = LoadUrl(urlType == UrlType.Previous ? data.prevUrl : data.nextUrl);
        // This task holds just the previously loaded data in the array
        LoadingDataTask[( (int)urlType + 1 ) % LoadingDataTask.Length] = Task.FromResult(Result.Success(currentData));

        return returnData;
    }

    /// <summary>
    /// Obtain data from <paramref name="url"/>
    /// </summary>
    /// <param name="url">The url to obtain data from</param>
    /// <returns>A LoadedData task holding either a null LoadedData, or a LoadedData with valid values</returns>
    public async Task<Result<LoadedData>> LoadUrl(string url)
    {
        if (!WebUtilities.IsUrl(url))
        {
            return Result.Error<LoadedData>($"'{url}' is not a valid url");
        }
        if (!WebUtilities.HasBaseUrl())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return Result.Error<LoadedData>($"'{url}' is not a valid url");
            WebUtilities.SetBaseUrl(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute));
        }
        url = WebUtilities.MakeAbsoluteUrl(url).ToString();

        var html = await WebService.GetHtmlFromUrl(url);
        // Format the html of a failed request
        if (html is Result<string>.Err error)
        {
            HtmlDocument invalidHtml = new HtmlDocument();
            invalidHtml.LoadHtml(error.Message);
            string innerText = invalidHtml.DocumentNode.InnerText.Trim();
            return Result.Error<LoadedData>(MatchNewlines().Replace(innerText, Environment.NewLine));
        }
        return await LoadReaderData(url, (html as Result<string>.Ok)!.Value);
    }

    /// <summary>
    /// Loads all necessary reader data into a LoadedData object
    /// </summary>
    /// <param name="url">The url used to obtain the reader data (this is not processed in this function)</param>
    /// <param name="html">The html to search for relevant data</param>
    /// <returns>A loaded data object containing the required data from the target website</returns>
    public async Task<Result<LoadedData>> LoadReaderData(string url, string html)
    {

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        if (!WebUtilities.HasBaseUrl())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return Result.Error<LoadedData>($"'{url}' is not a valid url");
            WebUtilities.SetBaseUrl(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute));
        }

        try
        {
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

            return Result.Success(data);
        }
        catch (XPathException)
        {
            return Result.Error<LoadedData>("Invalid XPath");
        }
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
            return HttpUtility.HtmlDecode(node.InnerText).Replace("\n", Config.Separator).Trim();
        }

        // Node contains child nodes, so we must get the text of each
        StringWriter stringWriter = new();

        foreach (HtmlNode child in node.ChildNodes)
        {
            string innerText = WhitespaceOnlyRegex().Replace(HttpUtility.HtmlDecode(child.InnerText), string.Empty);
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
        return stringWriter.ToString().Trim();
    }

    /// <summary>
    /// Gets the content as HTML
    /// </summary>
    /// <param name="nav">The Document to Get the Content Of</param>
    /// <returns><see cref="ConfigService.Selector.Content"/> as clean Html</returns>
    public string GetContentHtml(HtmlDocument nav)
    {
        return ConfigService.PrettyWrapSelector(nav.DocumentNode.InnerHtml, ConfigService.Selector.Content, ConfigService.SelectorType.Html);
    }


    /// <summary>
    /// Get title from <paramref name="html"/>
    /// </summary>
    /// <param name="html">The html to get the title from</param>
    /// <returns>A title determined by the title selector</returns>
    public string GetTitle(string html)
    {
        return HttpUtility.HtmlDecode(ConfigService.PrettyWrapSelector(html, ConfigService.Selector.Title, type: ConfigService.SelectorType.Text)).Trim();
    }


    [GeneratedRegex("^\\s+$", RegexOptions.Multiline)]
    private static partial Regex WhitespaceOnlyRegex();

    [GeneratedRegex("[\\s]{2,}")]
    private static partial Regex MatchNewlines();
}
