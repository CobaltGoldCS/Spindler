using HtmlAgilityPack;
using Spindler.Models;
using HtmlOrError = Spindler.Utilities.Result<string, string>;
using Path = Spindler.Models.Path;

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
            url = new Uri(client.BaseAddress, url!).ToString();

            HtmlOrError html = await HtmlOrError(url);
            if (html is HtmlOrError.Error error)
            {
                return MakeError(error.Value);
            }
            return await LoadWebData(url, html.AsOk().Value);
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

    #endregion

    #region Client
    private HttpClient? _client;
    private HttpClient client
    {
        get
        {
            if (_client is null)
            {
                HttpClientHandler handler = new();
                _client = new(handler) { };
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
    /// <returns>Returns an ErrorOr object either containing the html or an error value string</returns>
    public async Task<HtmlOrError> HtmlOrError(string url)
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
    /// Loads all necessary web data into a LoadedData object
    /// </summary>
    /// <param name="url">The url used to obtain the web data (this is not processed in this function)</param>
    /// <param name="html">The html to search for relevant data</param>
    /// <returns>A loaded data object containing the required data from the website</returns>
    public async Task<LoadedData> LoadWebData(string url, string html)
    {

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        if (client.BaseAddress == null)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return MakeError(url);
            client.BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
        }

        Task<string> text = Task.Run(() => { return configService!.GetContent(doc); });
        LoadedData data = new()
        {
            Text = await text,
            nextUrl = ConfigService.PrettyWrapSelector(doc, configService!.nextpath!, type: ConfigService.SelectorType.Link),
            prevUrl = ConfigService.PrettyWrapSelector(doc, configService.previouspath!, type: ConfigService.SelectorType.Link),
            Title = configService.GetTitle(doc),
            config = config,
            currentUrl = url
        };

        return data;
    }

    /// <summary>
    /// Make an error output with an optional message
    /// </summary>
    /// <param name="message">An optional error message</param>
    /// <returns>LoadedData in error form</returns>
    private LoadedData MakeError(string message = "") => new()
    {
        Title = "afb-4893",
        Text = message,
        config = config,
    };
    #endregion
}
