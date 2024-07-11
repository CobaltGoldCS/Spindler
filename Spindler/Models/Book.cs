using Newtonsoft.Json;
using Spindler.Services;
using SQLite;

namespace Spindler.Models;

/// <summary>
/// A Model representing configurations between the sqlite database and the backend code
/// </summary>
public record Book : IIndexedModel
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
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// A url where data should be fetched in order to read this book
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Image Url of the book image
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// The Pinned state of the book; whether it should show up in the pinned books view
    /// </summary>
    public bool Pinned { get; set; } = false;

    /// <summary>
    /// Whether the book has been completed by the original author
    /// </summary>
    public bool Completed { get; set; } = false;

    [Ignore]
    public IList<Bookmark> Bookmarks
    {
        get => JsonConvert.DeserializeObject<IList<Bookmark>>(BookmarksBlobbed) ?? new List<Bookmark>();
        set => BookmarksBlobbed = JsonConvert.SerializeObject(value);
    }

    [Ignore]
    public bool HasImageUrl { get => !string.IsNullOrEmpty(ImageUrl) && ImageUrl != "no_image.jpg"; }

    public string BookmarksBlobbed { get; set; } = string.Empty;

    /// <summary>
    /// A date time representing when this book was last opened
    /// </summary>
    public DateTime LastViewed { get; set; } = DateTime.UtcNow;

    public double Position { get; set; }

    public bool HasNextChapter { get; set; } = false;

    /// <summary>
    /// Saves a book
    /// </summary>
    /// <returns>An awaitable <see cref="Task"/></returns>
    public async Task SaveInfo(IDataService service)
    {
        LastViewed = DateTime.UtcNow;
        await service.SaveItemAsync(this);
    }
}


public record Bookmark
{
    public Bookmark(string Name, double Position, string Url)
    {
        this.Name = Name;
        this.Position = Position;
        this.Url = Url;
    }

    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public double Position { get; set; } = 0;
}
