using CommunityToolkit.Mvvm.ComponentModel;

namespace Spindler.Models;

/// <summary>
/// A model whoose job is to hold the loaded data of a given webpage for display in the user interface
/// </summary>
public partial class LoadedData : ObservableObject
{
    /// <summary>
    /// The text content of the loaded page
    /// </summary>
    [ObservableProperty]
    private string? text;

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
    public string? currentUrl;

    /// <summary>
    /// The title, scraped from the webpage
    /// </summary>
    [ObservableProperty]
    private string? title;

    /// <summary>
    /// The <see cref="Config"/> that was used as reference in order to scrape page data
    /// </summary>
    public Config? config;
}
