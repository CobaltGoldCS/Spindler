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


    [Ignore]
    public IList<Bookmark> Bookmarks
    {
        get => JsonConvert.DeserializeObject<IList<Bookmark>>(BookmarksBlobbed) ?? new List<Bookmark>();
        set => BookmarksBlobbed = JsonConvert.SerializeObject(value);
    }

    public string BookmarksBlobbed { get; set; } = string.Empty;

    /// <summary>
    /// A date time representing when this book was last opened
    /// </summary>
    public DateTime LastViewed { get; set; } = DateTime.UtcNow;

    public double Position { get; set; }

    public bool HasNextChapter { get; set; } = false;

    /// <summary>
    /// Updates <see cref="LastViewed"/> to the current time, and saves booklist in the database.
    /// </summary>
    /// <returns>An awaitable <see cref="Task"/></returns>
    public async Task UpdateViewTimeAndSave(IDataService service)
    {
        LastViewed = DateTime.UtcNow;
        await service.SaveItemAsync(this);
    }
}


public record Bookmark : IIndexedModel
{
    public Bookmark(string name, double position, string url)
    {
        Name = name;
        Position = position;
        Url = url;
    }

    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public double Position { get; set; } = 0;

    public int GetId()
    {
        throw new NotImplementedException();
    }
}
