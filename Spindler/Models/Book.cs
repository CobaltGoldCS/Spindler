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
    public int Id { get; set; }
    public int GetId() => Id;

    /// <summary>
    /// UID of the <see cref="BookList"/> that contains this book
    /// </summary>
    public int BookListId { get; set; }

    /// <summary>
    /// The name given to this book
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// A url where data should be fetched in order to read this book
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// A date time representing when this book was last opened
    /// </summary>
    public DateTime LastViewed { get; set; }
}
