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

    public bool Pinned { get; set; } = false;

    /// <summary>
    /// A date time representing when this book was last opened
    /// </summary>
    public DateTime LastViewed { get; set; } = DateTime.UtcNow;

    public double Position { get; set; }
}
