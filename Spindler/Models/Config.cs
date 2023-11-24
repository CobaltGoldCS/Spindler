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

    /// <summary>
    /// The extra configs in string form. Use <see ref="ExtraConfigs"/> instead
    /// </summary>
    public string ExtraConfigsBlobbed { get; set; } = "";

    /// <summary>
    /// A dictionary containing extra configuration settings
    /// </summary>
    [Ignore]
    [JsonIgnore]
    public Dictionary<string, object> ExtraConfigs
    {
        get => JsonConvert.DeserializeObject<Dictionary<string, object>>(ExtraConfigsBlobbed) ?? [];
        set
        {
            ExtraConfigsBlobbed = JsonConvert.SerializeObject(value);
        }
    }

    [Ignore]
    [JsonIgnore]
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
    [JsonIgnore]
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
    [JsonIgnore]
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
    [JsonIgnore]
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

    [Ignore]
    [JsonIgnore]
    public bool FilteringContentEnabled
    {
        get => (bool)ExtraConfigs.GetValueOrDefault("filteringcontentenabled", true);
        set
        {
            var tempExtraConfigs = ExtraConfigs;
            tempExtraConfigs["filteringcontentenabled"] = value;
            ExtraConfigs = tempExtraConfigs;
        }
    }

    [Ignore]
    [JsonIgnore]
    public bool HtmlContentEnabled
    {
        get => (bool)ExtraConfigs.GetValueOrDefault("htmlcontentenabled", false);
        set
        {
            var tempExtraConfigs = ExtraConfigs;
            tempExtraConfigs["htmlcontentenabled"] = value;
            ExtraConfigs = tempExtraConfigs;
        }
    }

    [Ignore]
    [JsonIgnore]
    public int ContentType
    {
        get => Convert.ToInt32(ExtraConfigs.GetValueOrDefault("contenttype", 0));
        set
        {
            var tempExtraConfigs = ExtraConfigs;
            tempExtraConfigs["contenttype"] = value;
            ExtraConfigs = tempExtraConfigs;
        }
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
                    if (config.MatchPath.AsPath().Select(doc, SelectorType.Text) != string.Empty)
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
