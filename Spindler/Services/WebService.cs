using HtmlAgilityPack;
using Spindler.Models;
using Spindler.Utils;

using Path = Spindler.Models.Path;
using HtmlOrError = Spindler.Utils.Result<string, string>;

namespace Spindler.Services;

public class WebService
{
    #region Class Attributes
    private readonly ConfigService configService;
    private readonly Config config;
    #endregion
    #region Public-Facing APIs

    public WebService(Config config)
    {
        this.config = config;
        configService ??= new ConfigService(config);
    }

    public WebService(Config config, ConfigService configService)
    {
        this.config = config;
        this.configService = configService;
    }
    ~WebService() => ClearCookies();
    
    public static void ClearCookies() => App.SharedValues.cookies = new System.Net.CookieContainer();
    /// <summary>
    /// Preload the next and previous urls with valid values into LoadedData
    /// </summary>
    /// <param name="prevUrl">The previous url (will be loaded into index 0)</param>
    /// <param name="nextUrl">The next url (will be loaded into index 1)</param>
    /// <returns>A Task containing a LoadedData array of length 2 [prevdata, nextdata]</returns>
    public async Task<LoadedData?[]> LoadData(string? prevUrl, string? nextUrl)
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
    public async Task<LoadedData?> LoadUrl(string? url)
    {
        if (!IsUrl(url))
        {
            return null;
        }
        if (client.BaseAddress == null)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return null;
            client.BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
        }
        try
        {
            url = TrimRelativeUrl(url!);

            HtmlOrError html = await HtmlOrError(url);
            if (Result.IsError(html))
            {
                return MakeError(url, html.AsError().message);
            }
            return await LoadWebData(url, html.AsOk().value);
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
    public static bool IsUrl(string? url)
    {
        bool created = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri _);
        return created && (url!.StartsWith("http") || url.StartsWith('/'));
    }
    /// <summary>
    /// Find a valid website configuration based on <paramref name="url"/>
    /// </summary>
    /// <param name="url">The url of the targeted website (handles http/https)</param>
    /// <returns>A valid configuration, or null if no valid configuration was found</returns>
    public static async Task<Config?> FindValidConfig(string url)
    {
        Config c = await App.Database.GetConfigByDomainNameAsync(new UriBuilder(url).Host);
        
        if (c != null) return c;
        try
        {
            HttpClient client = new()
            {
                Timeout = new TimeSpan(0, 0, 10)
            };
            HtmlDocument doc = new();
            doc.LoadHtml(await client.GetStringAsync(url));
            
            // Parallel index through all generalized configs
            Config? selectedConfig = null;
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
        catch (Exception e) when (
        e is IOException           ||
        e is TaskCanceledException ||
        e is System.Net.WebException)
        {
            return null;
        }
    }
    #endregion

    #region Client
    private HttpClient? _client;
    private HttpClient client
    {
        get
        {
            if (_client is null)
            {
                HttpClientHandler handler = new() { CookieContainer = App.SharedValues.cookies };
                _client = new(handler) {};
                _client.DefaultRequestHeaders.Add("User-Agent", App.SharedValues.userAgent);
            }
            return _client;
        }
        set => _client = value;
    }

    #endregion

    #region HelperFunctions
    /// <summary>
    /// Attempt to obtain html from a url
    /// </summary>
    /// <param name="url">The url to attempt to scrape</param>
    /// <returns>Returns an ErrorOr object either containing the html or an error message string</returns>
    private async Task<HtmlOrError> HtmlOrError(string url)
    {
        try
        {
            var message = new HttpRequestMessage(HttpMethod.Get, url);
            var result = await client.SendAsync(message);
            result = result.EnsureSuccessStatusCode();
            return new HtmlOrError.Ok(await result.Content.ReadAsStringAsync());
        }
        catch (HttpRequestException e)
        {
            return new HtmlOrError.Error($"Request Exception {e.StatusCode}: {e.Message}");
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
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri) &&
            !uri.IsAbsoluteUri &&
            uri.ToString().StartsWith("/"))
        {
            url = uri.ToString()[1..];
        }

        return url;
    }
    /// <summary>
    /// Loads all necessary web data into a LoadedData object
    /// </summary>
    /// <param name="url">The url used to obtain the web data (this is not processed in this function)</param>
    /// <param name="html">The html to search for relevant data</param>
    /// <returns>A loaded data object containing the required data from the website</returns>
    private async Task<LoadedData> LoadWebData(string url, string html)
    {

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        Task<string> text = Task.Run(() => { return configService!.GetContent(doc); });
        LoadedData data = new()
        {
            text = await text,
            nextUrl = ConfigService.PrettyWrapSelector(doc, configService!.nextpath!, type: ConfigService.SelectorType.Link),
            prevUrl = ConfigService.PrettyWrapSelector(doc, configService.previouspath!, type: ConfigService.SelectorType.Link),
            title = configService.GetTitle(doc),
            config = config,
            currentUrl = new Uri(client.BaseAddress!, url).ToString()
        };

        return data;
    }

    private LoadedData MakeError(string currenturl, string message) => new()
    {
        text = message,
        title = "afb-4893",
        prevUrl = null,
        nextUrl = null,
        currentUrl = currenturl,
        config = config,
    };
    #endregion
}
