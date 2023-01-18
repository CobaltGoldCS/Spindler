using SQLite;

namespace Spindler.Models;

/// <summary>
/// A Model representing configurations between the SQLite database and the backend code
/// </summary>
public class BookList : IIndexedModel
{

    /// <summary>
    /// UID of the Book list
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; } = -1;

    /// <summary>
    /// Name of the book list
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Url pointing to the image to be used for the book list
    /// </summary>
    public string ImageUrl { get; set; } = GetRandomPlaceholderImageUrl();

    /// <summary>
    /// The datetime instance in which this book list was last accessed
    /// </summary>
    public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// String Representing the top left color of the book list gradient
    /// </summary>
    public string ColorString1 { get; set; } = "#00000000";
    /// <summary>
    /// String Representing the bottom right color of the book list gradient
    /// </summary>
    public string ColorString2 { get; set; } = "#00000000";



    /// <summary>
    /// Chooses a random placeholder image url to use for a Book list
    /// </summary>
    /// <returns>The random placeholder image url as a string</returns>
    public static string GetRandomPlaceholderImageUrl()
    {
        Random rand = new Random();
        List<string> images = new List<string>
        {
            "https://images.unsplash.com/photo-1553532434-5ab5b6b84993?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1074&q=80",
            "https://images.unsplash.com/flagged/photo-1579268351234-073f85929562?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1074&q=80",
            "https://images.unsplash.com/photo-1555679427-1f6dfcce943b?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=735&q=80"
        };
        int index = rand.Next(images.Count - 1);
        return images[index];
    }

    /// <summary>
    /// Updates <see cref="LastAccessed"/> to the current time, and saves booklist in the database.
    /// </summary>
    /// <returns>An awaitable <see cref="Task"/></returns>
    public async Task UpdateAccessTimeToNow()
    {
        LastAccessed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(this);
    }

    public int GetId() => Id;

    /// <summary>
    /// Gets the background as a linear gradient brush
    /// </summary>
    /// <returns>A linear gradient based on color1 and color2</returns>
    [Ignore]
    public Brush Background
    {
        get
        {
            var gradientStops = new GradientStopCollection
            {
                new GradientStop(Color1, 0.0f),
                new GradientStop(Color2, 1.0f)
            };

            var bgBrush = new LinearGradientBrush(
                gradientStops,
                new Point(0.0, 0.0),
                new Point(1.0, 1.0));

            return bgBrush;
        }
    }

    [Ignore]
    public Color Color1
    {
        get => Color.FromArgb(ColorString1);
        set => ColorString1 = value.ToArgbHex(true);
    }

    [Ignore]
    public Color Color2
    {
        get => Color.FromArgb(ColorString2);
        set => ColorString2 = value.ToArgbHex(true);
    }
}
