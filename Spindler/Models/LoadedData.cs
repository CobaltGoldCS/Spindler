using CommunityToolkit.Mvvm.ComponentModel;
using Spindler.Utilities;

namespace Spindler.Models;

/// <summary>
/// A model whoose job is to hold the loaded data of a given webpage for display in the user interface
/// </summary>
public partial class LoadedData : ObservableObject
{
    /// <summary>
    /// The text content of the loaded page
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// A url pointing to the "next chapter" of the book
    /// </summary>
    public string nextUrl = string.Empty;

    /// <summary>
    /// A url pointing to the "previous chapter" of the book
    /// </summary>
    public string prevUrl = string.Empty;

    /// <summary>
    /// A url pointing to the current location the data was obtained from
    /// </summary>
    public string currentUrl;

    /// <summary>
    /// The title, scraped from the webpage
    /// </summary> 
    public string Title { get; set; }


    [ObservableProperty]
    private bool prevUrlValid;

    [ObservableProperty]
    private bool nextUrlValid;


    public LoadedData(string title, string text, string currentUrl, string prevUrl, string nextUrl)
    {
        this.Title = title;
        this.Text = text;
        this.currentUrl = currentUrl;
        this.prevUrl = prevUrl;
        this.nextUrl = nextUrl;

        ConvertUrlsToAbsolute();

        NextUrlValid = WebUtilities.IsUrl(nextUrl);
        PrevUrlValid = WebUtilities.IsUrl(prevUrl);
    }

    public static LoadedData CreatePlaceholder() => new("Loading", "Loading");

    public void InvalidateNextUrl() => NextUrlValid = false;
    public void InvalidatePrevUrl() => PrevUrlValid = false;

    private LoadedData(string title, string text)
    {
        this.Title = title;
        this.Text = text;
        this.currentUrl = string.Empty;
    }

    private void ConvertUrlsToAbsolute()
    {
        Uri baseUrl = new(currentUrl);

        nextUrl = new Uri(baseUri: baseUrl, nextUrl).ToString();
        prevUrl = new Uri(baseUri: baseUrl, prevUrl).ToString();
    }
}
