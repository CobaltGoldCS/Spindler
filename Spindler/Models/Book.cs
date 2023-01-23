using HtmlAgilityPack;
using Java.Net;
using Spindler.Services;
using SQLite;

namespace Spindler.Models;

/// <summary>
/// A Model representing configurations between the sqlite database and the backend code
/// </summary>
public class Book : IIndexedModel
{
    /// <summary>
    /// UID of this Book
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; } = -1;
    public int GetId() => Id;
    public string Name { get => Title; }

    /// <summary>
    /// UID of the <see cref="BookList"/> that contains this book
    /// </summary>
    public int BookListId { get; set; }

    /// <summary>
    /// The name given to this book
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// A url where data should be fetched in order to read this book
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary>
    /// Image Url of the book image
    /// </summary>
    public string ImageUrl { get; set; } = "no_image.jpg";

    public bool Pinned { get; set; } = false;

    /// <summary>
    /// A date time representing when this book was last opened
    /// </summary>
    public DateTime LastViewed { get; set; } = DateTime.UtcNow;

    public double Position { get; set; }

    [Ignore]
    private Config? config { get; set; }

    [Ignore]
    public Config? Config
    {
        get
        {
            if (config is null)
                FindConfig();
            return config;
        }
    }
    /// <summary>
    /// Find a matching config for the given book and store it in <see cref="Book.Config"/>
    /// </summary>
    /// <param name="html">An optional string parameter for searching for generalized configs</param>
    public async void FindConfig(string? html = null)
    {
        if (config is not null) return;

        Config c = await App.Database.GetConfigByDomainNameAsync(new UriBuilder(Url).Host);

        if (c != null) config = c;
        try
        {
            HttpClient client = new()
            {
                Timeout = new TimeSpan(0, 0, 10)
            };
            HtmlDocument doc = new();
            doc.LoadHtml(html is not null ? html : await client.GetStringAsync(Url));

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
            config =  selectedConfig;
        }
        catch (Exception e) when (
        e is IOException ||
        e is TaskCanceledException ||
        e is System.Net.WebException)
        {
            config = null;
        }
    }
    /// <summary>
    /// Updates <see cref="LastViewed"/> to the current time, and saves booklist in the database.
    /// </summary>
    /// <returns>An awaitable <see cref="Task"/></returns>
    public async Task UpdateLastViewedToNow()
    {
        LastViewed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(this);
    }
}
