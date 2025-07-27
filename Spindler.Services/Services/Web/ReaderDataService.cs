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

        var priorData = currentData;

        returnData.HandleSuccess((data) =>
        {
            switch (urlType)
            {
                // User Clicked the previous chapter button
                case UrlType.Previous:
                    LoadingDataTask[(int)UrlType.Previous] = LoadUrl(data.prevUrl);
                    LoadingDataTask[(int)UrlType.Next] = Task.FromResult(Result.Success(priorData));
                    break;
                // User Clicked the next chapter button
                case UrlType.Next:
                    LoadingDataTask[(int)UrlType.Next] = LoadUrl(data.nextUrl);
                    LoadingDataTask[(int)UrlType.Previous] = Task.FromResult(Result.Success(priorData));
                    break;
                default:
                    throw new NotImplementedException();
            }
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
        var baseUrlResult = WebUtilities.SetBaseUrlSafe(url);
        if (baseUrlResult is Result<string>.Err error)
        {
            return Result.Error<LoadedData>(error.Message);
        }

        url = WebUtilities.MakeAbsoluteUrl(url).ToString();

        var html = await WebService.GetHtmlFromUrl(url);

        Result<LoadedData> returnValue;

        if (html is Result<string>.Err invalidMessage)
        {
            HtmlDocument invalidHtml = new();
            invalidHtml.LoadHtml(invalidMessage.Message);
            string innerText = invalidHtml.DocumentNode.InnerText.Trim();
            returnValue = Result.Error<LoadedData>(MatchNewlines().Replace(innerText, Environment.NewLine));
        }
        else
        {
            var okResult = html as Result<string>.Ok;
            returnValue = await LoadReaderData(url, okResult!.Value);
        }
        return returnValue;
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

        var baseUrlResult = WebUtilities.SetBaseUrlSafe(url);
        if (baseUrlResult is Result<string>.Err error)
        {
            return Result.Error<LoadedData>(error.Message);
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

            Task<IEnumerable<string>> textTask = Task.Run(() => contentExtractor.GetContent(doc, Config, ConfigService));

            Task<string>[] selectorOperations =
            [
                Task.Run(() => GetTitle(html)),
                Task.Run(() => ConfigService.PrettyWrapSelector(html, ConfigService.Selector.NextUrl, SelectorType.Link)),
                Task.Run(() => ConfigService.PrettyWrapSelector(html, ConfigService.Selector.PrevUrl, SelectorType.Link)),
            ];
            string[] content = await Task.WhenAll(selectorOperations);

            LoadedData data = new(
                title: content[0],
                text: await textTask,
                nextUrl: content[1],
                prevUrl: content[2],
                currentUrl: url
            );

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
