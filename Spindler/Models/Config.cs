using HtmlAgilityPack;
using Newtonsoft.Json;
using Spindler.Services;
using SQLite;
using System.Xml.XPath;

namespace Spindler.Models;

/// <summary>
/// A Model representing configurations between the sqlite database and the backend code
/// </summary>
public class Config : IIndexedModel
{
    /// <summary>
    /// UID of the Config
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int GetId() => Id;
    public string Name { get => DomainName; }

    /// <summary>
    /// The domain name associated with the config (IE: example.com)
    /// </summary>
    public string DomainName { get; set; } = "";

    /// <summary>
    /// The path pointing to the title element (if specified) otherwise null
    /// </summary>
    public string TitlePath { get; set; } = "";

    /// <summary>
    /// The path pointing to the main content of the website
    /// </summary>
    public string ContentPath { get; set; } = "";

    /// <summary>
    /// The path pointing to the url denoting the "next chapter"
    /// </summary>
    public string NextUrlPath { get; set; } = "";

    /// <summary>
    /// The path pointing to the url denoting the "previous chapter"
    /// </summary>
    public string PrevUrlPath { get; set; } = "";

    /// <summary>
    /// The path pointing to an image url for any potential book
    /// </summary>
    public string ImageUrlPath { get; set; } = "";


    private string _pathType = "";
    /// <summary>
    /// The type of path that is denoted in the configuration (usually xpath)
    /// </summary>
    public string PathType { get => _pathType; private set => _pathType = value; }

    /// <summary>
    /// The extra configs in string form. Use <see ref="ExtraConfigs"/> instead
    /// </summary>
    public string ExtraConfigsBlobbed { get; set; } = "";

    /// <summary>
    /// A dictionary containing extra configuration settings
    /// </summary>
    [Ignore]
    public Dictionary<string, object> ExtraConfigs
    {
        get => JsonConvert.DeserializeObject<Dictionary<string, object>>(ExtraConfigsBlobbed) ?? new();
        set
        {
            ExtraConfigsBlobbed = JsonConvert.SerializeObject(value);
        }
    }

    [Ignore]
    public string Separator
    {
        get => (string)ExtraConfigs.GetValueOrDefault("separator", "\n");
        set
        {
            var tempExtraConfigs = ExtraConfigs;
            tempExtraConfigs["separator"] = value;
            ExtraConfigs = tempExtraConfigs;
        }
    }

    [Ignore]
    public bool UsesWebview
    {
        get => (bool)ExtraConfigs.GetValueOrDefault("webview", false);
        set
        {
            var tempExtraConfigs = ExtraConfigs;
            tempExtraConfigs["webview"] = value;
            ExtraConfigs = tempExtraConfigs;
        }
    }

    [Ignore]
    public bool UsesHeadless
    {
        get => (bool)ExtraConfigs.GetValueOrDefault("headless", false);
        set
        {
            var tempExtraConfigs = ExtraConfigs;
            tempExtraConfigs["headless"] = value;
            ExtraConfigs = tempExtraConfigs;
        }
    }

    [Ignore]
    public bool HasAutoscrollAnimation
    {
        get => (bool)ExtraConfigs.GetValueOrDefault("autoscrollanimation", true);
        set
        {
            var tempExtraConfigs = ExtraConfigs;
            tempExtraConfigs["autoscrollanimation"] = value;
            ExtraConfigs = tempExtraConfigs;
        }
    }


    public void SetPathType(string pathType)
    {
        PathType = pathType;
    }

    public static async Task<Config?> FindValidConfig(string url, string? html = null)
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        return await FindValidConfig(client, url, html);
    }
    /// <summary>
    /// Find a valid website configuration based on <paramref name="url"/>
    /// </summary>
    /// <param name="url">The url of the targeted website (handles http/https)</param>
    /// <returns>A valid configuration, or null if no valid configuration was found</returns>
    public static async Task<Config?> FindValidConfig(HttpClient client, string url, string? html = null)
    {
        try
        {
            Config c = await App.Database.GetConfigByDomainNameAsync(new UriBuilder(url).Host);

            if (c != null) return c;

            HtmlDocument doc = new();
            doc.LoadHtml(html ?? await client.GetStringAsync(url));

            // Parallel index through all generalized configs
            Config? selectedConfig = null;
            Parallel.ForEach(await App.Database.GetAllItemsAsync<GeneralizedConfig>(), (GeneralizedConfig config, ParallelLoopState state) =>
            {
                try
                {
                    if (ConfigService.PrettyWrapSelector(doc, new Path(config.MatchPath), ConfigService.SelectorType.Text) != string.Empty)
                    {
                        selectedConfig = config;
                        state.Stop();
                    }
                }
                catch (XPathException) { }
            });
            return selectedConfig;
        }
        catch (Exception e) when (
        e is IOException ||
        e is TaskCanceledException ||
        e is HttpRequestException ||
        e is UriFormatException ||
        e is System.Net.WebException)
        {
            return null;
        }
    }
}
