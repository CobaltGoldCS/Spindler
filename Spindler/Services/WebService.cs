using HtmlAgilityPack;
using Spindler.Models;
using Spindler.Utils;
using Path = Spindler.Models.Path;

namespace Spindler.Services;

public class WebService
{
    #region Class Attributes
    private ConfigService pathService;
    private Config config;
    #endregion
    #region Public-Facing APIs

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
    public async Task<LoadedData[]> LoadData(string prevUrl, string nextUrl)
    {
        Task<LoadedData> prevTask = LoadUrl(prevUrl);
        Task<LoadedData> nextTask = LoadUrl(nextUrl);
        var loaded = await Task.WhenAll(prevTask, nextTask);
        return loaded;
    }

    /// <summary>
    /// Obtain data from <paramref name="url"/>
    /// </summary>
    /// <param name="url">The url to obtain data from</param>
    /// <returns>A LoadedData task holding either a null LoadedData, or a LoadedData with valid values</returns>
    public async Task<LoadedData> LoadUrl(string url)
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
            url = TrimRelativeUrl(url);

            ErrorOr<string> html = await HtmlOrError(url);
            if (html is ErrorOr<string>.Error error)
            {
                return MakeError(url, error.message);
            }
            return await LoadHTML(url, ((ErrorOr<string>.Success)html).value);
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

    public static async Task<Config> FindValidConfig(string url)
    {
        Config c = await App.Database.GetConfigByDomainNameAsync(new UriBuilder(url).Host);
        
        if (c != null) return c;
        try
        {
            var html = await new HttpClient().GetStringAsync(url);
            HtmlDocument doc = new();
            doc.LoadHtml(html);
            Config selectedConfig = null;
            Parallel.ForEach(await App.Database.GetAllItemsAsync<GeneralizedConfig>(), (GeneralizedConfig config, ParallelLoopState state) =>
            {
                if (ConfigService.PrettyWrapSelector(doc, new Path(config.MatchPath), ConfigService.SelectorType.Text) != null)
                {
                    selectedConfig = config;
                    state.Stop();
                }
            });
            return selectedConfig;
        }
        catch(IOException)
        {
            return null;
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

    #region HelperFunctions

    private async Task<ErrorOr<string>> HtmlOrError(string url)
    {
        try
        {
            return new ErrorOr<string>.Success(await client.GetStringAsync(url));
        }
        catch (IOException e)
        {
            return new ErrorOr<string>.Error($"Invalid request format: {e}");
        }
        catch (InvalidOperationException e)
        {
            return new ErrorOr<string>.Error($"Invalid Operation: {e}");
        }
        catch (TaskCanceledException e)
        {
            return new ErrorOr<string>.Error($"Task Cancelled: {e}");
        }
    }
    /// <summary>
    /// Trims the leading '/' of a relative url if present for HttpClient
    /// </summary>
    /// <param name="url">The url to trim</param>
    /// <returns>A trimmed url, or normal absolute url</returns>
    private static string TrimRelativeUrl(string url)
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

    private LoadedData MakeError(string currenturl, string message) => new()
    {
        text = message,
        title = "An Error Has occurred",
        prevUrl = null,
        nextUrl = null,
        currentUrl = currenturl,
        config = config,
    };
    #endregion
}
