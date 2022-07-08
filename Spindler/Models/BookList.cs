using SQLite;

namespace Spindler.Models;

/// <summary>
/// A Model representing configurations between the sqlite database and the backend code
/// </summary>
public class BookList : IIndexedModel
{
    /// <summary>
    /// UID of the Book list
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int GetId() => Id;

    /// <summary>
    /// Name of the book list
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Url pointing to the image to be used for the book list
    /// </summary>
    public string ImageUrl { get; set; }

    /// <summary>
    /// The datetime instance in which this book list was last accessed
    /// </summary>
    public DateTime LastAccessed { get; set; }

    /*public BookList(int id, string name, string imageUrl = null)
    {
        Id = id;
        Name = name;
        ImageUrl = imageUrl != null ? imageUrl : GetRandomPlaceholderImageUrl();
        LastAccessed = DateTime.Now;
    }*/

    /// <summary>
    /// Gives a new Book List with associated values
    /// </summary>
    public BookList()
    {
        Id = -1;
        Name = null;
        LastAccessed = DateTime.MinValue;
        ImageUrl = GetRandomPlaceholderImageUrl();
    }



    /// <summary>
    /// Chooses a random placeholder image url to use for a Book list
    /// </summary>
    /// <returns>The random placeholder image url as a string</returns>
    public static String GetRandomPlaceholderImageUrl()
    {
        Random rand = new Random();
        List<string> images = new List<string>();
        images.Add("https://images.unsplash.com/photo-1553532434-5ab5b6b84993?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1074&q=80");
        images.Add("https://images.unsplash.com/flagged/photo-1579268351234-073f85929562?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1074&q=80");
        images.Add("https://images.unsplash.com/photo-1555679427-1f6dfcce943b?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=735&q=80");
        int index = rand.Next(images.Count - 1);
        return images[index];
    }
}
