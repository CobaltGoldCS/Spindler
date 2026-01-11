using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using Spindler.Models;
using Spindler.Services.Web;
using Spindler.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.XPath;

namespace Spindler.Services;

public partial class ReaderDataService : ObservableObject
{
    // This is so other things can access lower level APIs
    /// <summary>
    /// Internal SelectionService
    /// </summary>
    public SelectionService ConfigService { get; private set; }
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


    private readonly UrlBuilder UrlBuilder = new();
    private PersistantChapterDataStore Chapters = new();

    /// <summary>
    /// The Extractor in charge of extracting the main content of the chapter.
    /// </summary>
    private BaseContentExtractor ContentExtractor { get; set; }

    public enum UrlType
    {
        Previous = 0,
        Next = 1,
    }

    public ReaderDataService(Config config, IWebService webService)
    {
        Config = config;
        WebService = webService;
        ConfigService = new(config);

        ContentExtractor = (TargetType)Config.ContentType switch
        {
            TargetType.Text => new TextContentExtractor(),
            TargetType.Html => new HtmlContentExtractor(),
            TargetType.All_Tags_Matching_Path => new AllTagsContentExtractor(),
            _ => throw new InvalidDataException("Content Type Not Supported")
        };

        IsContentHtml = ContentExtractor is HtmlExtractor;
    }

    /// <summary>
    /// Obtain data from <paramref name="url"/>
    /// </summary>
    /// <param name="url">The url to obtain data from</param>
    /// <returns>A Chapter task holding either a null Chapter, or a Chapter with valid values</returns>
    public async Task<Result<Chapter>> LoadChapter(string url)
    {
        if (!UrlBuilder.IsUrl(url))
        {
            return Result.Error<Chapter>($"'{url}' is not a valid url");
        }
        var baseUrlResult = UrlBuilder.SetBaseUrlSafe(url);
        if (baseUrlResult is Result<string>.Err error)
        {
            return Result.Error<Chapter>(error.Message);
        }

        url = UrlBuilder.MakeAbsoluteUrl(url).ToString();

        if (Chapters.TryGetValue(url, out Chapter? chapter))
        {
            return Result.Success(chapter!);
        }

        var html = await WebService.GetHtmlFromUrl(url);

        Result<Chapter> returnValue;

        if (html is Result<string>.Err invalidMessage)
        {
            HtmlDocument invalidHtml = new();
            invalidHtml.LoadHtml(invalidMessage.Message);
            string innerText = invalidHtml.DocumentNode.InnerText.Trim();
            returnValue = Result.Error<Chapter>(MatchNewlines().Replace(innerText, Environment.NewLine));
        }
        else
        {
            var okResult = html as Result<string>.Ok;
            returnValue = await ExtractFromHtml(url, okResult!.Value);
        }

        if (returnValue is Result<Chapter>.Ok data)
        {
            Chapters[url] = data.Value;
        }
        return returnValue;
    }

    CancellationTokenSource tokenSource = new();
    /// <summary>
    /// Load Chapter Given previously known chapter. This overload is given in order to optimize
    /// chapter fetching by giving Spindler a direction to load new chapters into. Use if possible.
    /// </summary>
    /// <param name="direction">The next chapter (relative) to load</param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Result<Chapter>> LoadChapter(UrlType direction, Chapter currentChapter)
    {

        var chapterUrl = direction switch
        {
            UrlType.Previous => currentChapter.prevUrl,
            UrlType.Next => currentChapter.nextUrl,
            _ => throw new ArgumentException("Unknown UrlType (LOADCHAPTER)")
        };

        Result<Chapter> returnChapter = await LoadChapter(chapterUrl);
        tokenSource.Cancel();
        tokenSource = new();
        StartPreloadDataThread(currentChapter, direction, tokenSource.Token);

        return returnChapter;
    }

    /// <summary>
    /// Loads all necessary reader data into a Chapter object
    /// </summary>
    /// <param name="url">The url used to obtain the reader data (this is not processed in this function)</param>
    /// <param name="html">The html to search for relevant data</param>
    /// <returns>A loaded data object containing the required data from the target website</returns>
    public async Task<Result<Chapter>> ExtractFromHtml(string url, string html)
    {

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        try
        {

            Task<IEnumerable<string>> textTask = Task.Run(() => ContentExtractor.GetContent(doc, Config, ConfigService));

            Task<string>[] selectorOperations =
            [
                Task.Run(() => GetTitle(html)),
                Task.Run(() => ConfigService.Select(html, SelectionService.Selector.NextUrl, SelectorType.Link)),
                Task.Run(() => ConfigService.Select(html, SelectionService.Selector.PrevUrl, SelectorType.Link)),
            ];
            string[] content = await Task.WhenAll(selectorOperations);

            Chapter data = new(
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
            return Result.Error<Chapter>("Invalid XPath");
        }
    }


    /// <summary>
    /// Get title from <paramref name="html"/>
    /// </summary>
    /// <param name="html">The html to get the title from</param>
    /// <returns>A title determined by the title selector</returns>
    public string GetTitle(string html)
    {
        return HttpUtility.HtmlDecode(ConfigService.Select(html, SelectionService.Selector.Title, type: SelectorType.Text)).Trim();
    }

    [GeneratedRegex("[\\s]{2,}")]
    private static partial Regex MatchNewlines();

    private void StartPreloadDataThread(Chapter? anchor, UrlType direction, CancellationToken token)
    {
        Task.Run((Func<Task?>)(async () =>
        {

            if (anchor == null)
            {
                return;
            }

            Chapter currentInterestedData = direction switch
            {
                UrlType.Previous => Chapters.FindAbsoluteFirstChapter(anchor),
                UrlType.Next => Chapters.FindAbsoluteLastChapter(anchor),
                _ => throw new NullReferenceException("Unknown UrlType (PreloadDataThread)"),
            };

            switch (direction)
            {
                case UrlType.Previous:
                    while (currentInterestedData.PrevUrlValid && !token.IsCancellationRequested)
                    {
                        Result<Chapter> result = await LoadChapter(currentInterestedData.prevUrl);
                        result.HandleSuccess((data) =>
                        {
                            currentInterestedData = data;
                            Chapters[data.currentUrl] = data;
                        });
                    }
                    break;
                case UrlType.Next:
                    while (currentInterestedData.NextUrlValid && !token.IsCancellationRequested)
                    {
                        Result<Chapter> result = await LoadChapter(currentInterestedData.nextUrl);
                        result.HandleSuccess((data) =>
                        {
                            currentInterestedData = data;
                            Chapters[data.currentUrl] = data;
                        });
                    }
                    break;
                default:
                    Debug.WriteLine("Invalid URLTYPE given: PreloadDataThread");
                    return;
            }
        }), token);
    }
}

internal partial class PersistantChapterDataStore : ConcurrentDictionary<string, Chapter>
{

    public Chapter FindAbsoluteFirstChapter(Chapter anchor)
    {
        var firstKnownChapter = anchor;
        while (ContainsKey(firstKnownChapter.prevUrl))
        {
            firstKnownChapter = this[firstKnownChapter.prevUrl];
        }

        return firstKnownChapter;
    }

    public Chapter FindAbsoluteLastChapter(Chapter anchor)
    {
        var lastKnownChapter = anchor;
        while (ContainsKey(lastKnownChapter.nextUrl))
        {
            lastKnownChapter = this[lastKnownChapter.nextUrl];
        }

        return lastKnownChapter;
    }
}