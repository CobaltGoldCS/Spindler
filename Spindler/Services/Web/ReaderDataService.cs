using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using Spindler.Models;
using Spindler.Services.Web;
using Spindler.Utilities;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;

namespace Spindler.Services;

public partial class ReaderDataService : ObservableObject
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

    [ObservableProperty]
    public bool isContentHtml = false;


    private readonly WebUtilities WebUtilities = new();

    private readonly Task<Result<LoadedData>>[] LoadingDataTask =
    [
        Task.FromResult(Result.Error<LoadedData>("Uninitialized Data")),
        Task.FromResult(Result.Error<LoadedData>("Uninitialized Data"))
    ];

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

        returnData.HandleSuccess((data) =>
        {
            LoadingDataTask[(int)urlType] = LoadUrl(urlType == UrlType.Previous ? data.prevUrl : data.nextUrl);
            // This task holds just the previously loaded data in the array
            LoadingDataTask[((int)urlType + 1) % LoadingDataTask.Length] = Task.FromResult(Result.Success(currentData));
        });

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

            BaseContentExtractor contentExtractor = (TargetType)Config.ContentType switch
            {
                TargetType.Text => new TextContentExtractor(),
                TargetType.Html => new HtmlContentExtractor(),
                TargetType.All_Tags_Matching_Path => new AllTagsContentExtractor(),
                _ => throw new InvalidDataException("Content Type Not Supported")
            };

            IsContentHtml = contentExtractor is HtmlExtractor;

            Task<string>[] selectorOperations =
            [
                Task.Run(() => contentExtractor.GetContent(doc, Config, ConfigService)),
                Task.Run(() => GetTitle(html)),
                Task.Run(() => ConfigService.PrettyWrapSelector(html, ConfigService.Selector.NextUrl, SelectorType.Link)),
                Task.Run(() => ConfigService.PrettyWrapSelector(html, ConfigService.Selector.PrevUrl, SelectorType.Link)),
            ];
            string[] content = await Task.WhenAll(selectorOperations);

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
    /// Get title from <paramref name="html"/>
    /// </summary>
    /// <param name="html">The html to get the title from</param>
    /// <returns>A title determined by the title selector</returns>
    public string GetTitle(string html)
    {
        return HttpUtility.HtmlDecode(ConfigService.PrettyWrapSelector(html, ConfigService.Selector.Title, type: SelectorType.Text)).Trim();
    }

    [GeneratedRegex("[\\s]{2,}")]
    private static partial Regex MatchNewlines();
}
