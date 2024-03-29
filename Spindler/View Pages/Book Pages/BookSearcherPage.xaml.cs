using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using HtmlAgilityPack;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using Path = Spindler.Models.Path;

namespace Spindler.Views.Book_Pages;

[QueryProperty(nameof(InitialSource), "source")]
public partial class BookSearcherPage : ContentPage
{
    #region Page Properties
    /// <summary>
    /// The current state of the Book Searcher Page. 
    /// Whether a book is found, saved, or not detected.
    /// </summary>
    private enum State
    {
        BookFound,
        BookNotFound,
        BookSaved
    }

    /// <summary>
    /// The currently used configuration for detecting books
    /// </summary>
    public Config? Config { get; set; } = null;

    public HttpClient Client;
    public IDataService Database;

    /// <summary>
    /// Whether <see cref="SearchBrowser"/> is busy loading and checking a page or not
    /// </summary>
    public bool IsLoading = false;


    private string source = string.Empty;
    /// <summary>
    /// The Source value as shown in the <see cref="addressBar"/>
    /// </summary>
    public string Source
    {
        get => source;
        set
        {
            source = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initial loader variable for setting url when book searcher page is navigated to 
    /// </summary>
    public string InitialSource
    {
        set
        {
            Source = value;
            Return();
        }
    }


    public BookSearcherPage(IDataService database, HttpClient client)
    {
        InitializeComponent();
        SwitchUiBasedOnState(State.BookNotFound);
        Database = database;
        Client = client;
    }

    /// <summary>
    /// Programmatically change which buttons are shown in the ui given a <see cref="State"/>
    /// </summary>
    /// <param name="state">The state context for the ui to update</param>
    /// <seealso cref="CheckButton"/>
    /// <seealso cref="SaveButton"/>
    private void SwitchUiBasedOnState(State state)
    {
        CheckButton.IsVisible = false;
        SaveButton.IsVisible = false;
        switch (state)
        {
            case State.BookNotFound:
                CheckButton.IsVisible = true;
                break;
            case State.BookFound:
                SaveButton.IsVisible = true;
                SaveButton.IsEnabled = true;
                break;
            case State.BookSaved:
                SaveButton.IsVisible = true;
                SaveButton.IsEnabled = false;
                break;
        }
    }
    #endregion

    #region Browser Event Handlers
    /// <summary>
    /// An event handler that triggers when a page is fully loaded by <see cref="SearchBrowser"/>
    /// </summary>
    /// <param name="_">(UNUSED) This should always be search browser</param>
    /// <param name="e">The event arguments associated with the WebNavigatedEvent</param>
    /// 
    private async void PageLoaded(object? _, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success)
        {
            IsLoading = false;
            return;
        }
        await SearchProgress.ProgressTo(.5, 500, Easing.CubicOut);
        await CheckCompatible(e.Url != "about:blank" ? e.Url : "");
    }

    /// <summary>
    /// An event handler that triggers when a page begins to load in <see cref="SearchBrowser"/>
    /// </summary>
    /// <param name="_">(UNUSED) This should always be search browser</param>
    /// <param name="e">The event arguments associated with the WebNavigatingEvent</param>
    private async void PageLoading(object? _, WebNavigatingEventArgs e)
    {
        if (SearchBrowser.IsRedirect(source) && IsLoading)
        {
            e.Cancel = true;
            return;
        }
        Source = e.Url;
        SwitchUiBasedOnState(State.BookNotFound);
        await SearchProgress.ProgressTo(0, 0, Easing.CubicOut);
    }
    #endregion

    #region Click Event Handlers
    /// <summary>
    /// Check if this website matches any of the user defined configurations for a book, and update the user interface if it does
    /// </summary>
    /// <param name="url">An optional parameter to pass in the url of the website</param>
    /// <returns>A task object for awaiting purposes</returns>
    [RelayCommand]
    private async Task CheckCompatible(string url = "")
    {
        string html = await SearchBrowser.GetHtmlUnsafe();

        Config = await Config.FindValidConfig(Database, Client, !string.IsNullOrEmpty(url) ? url : SearchBrowser.GetUrl(), html);
        if (Config is null)
        {
            IsLoading = false;
            return;
        }

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        try
        {
            Path contentPath = Config.ContentPath.AsPath();
            string content = contentPath.Select(doc, SelectorType.Text);

            SwitchUiBasedOnState(!string.IsNullOrEmpty(content) ? State.BookFound : State.BookNotFound);
        }
        catch (Exception e)
        {
            await Toast.Make(e.Message).Show();
        }

        await SearchProgress.ProgressTo(1, 500, Easing.BounceOut);
        IsLoading = false;
    }

    /// <summary>
    /// Create a new book based on information obtained from <see cref="SearchBrowser"/>
    /// <code>
    /// // A Typical book generates as 
    /// new Book  
    /// {  
    ///     Id: -1, // Denotes a new book 
    ///     BookListId: Result of popup
    ///     Title: <see cref="Config.TitlePath"/> or ""  
    ///     Url: <see cref="Source"/>   
    /// }  
    /// </code>
    /// </summary>
    [RelayCommand]
    private async Task CreateBookFromConfigInformation()
    {
        string html = await SearchBrowser.GetHtmlUnsafe();

        PickerPopup bookListPopup = new("Choose Book List of Book", await Database.GetAllItemsAsync<BookList>());
        var result = await this.ShowPopupAsync(bookListPopup);

        if (result is not BookList)
        {
            return;
        }

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        Models.Path titlePath = Config!.TitlePath.AsPath();
        string title = titlePath.Select(doc, SelectorType.Text);
        await Database.SaveItemAsync(
            new Book
            {
                BookListId = ((BookList)result).Id,
                Title = title,
                Url = Source,
            }
        );
        SwitchUiBasedOnState(State.BookSaved);
    }

    /// <summary>
    /// Creates a popup that allows you to choose a configuration to load the url of
    /// </summary>
    [RelayCommand]
    private async Task UseConfigAsDomainUrl()
    {
        if (IsLoading) return;
        PickerPopup popup = new("Choose a config to search", await Database.GetAllItemsAsync<Config>());
        var result = await this.ShowPopupAsync(popup);

        if (result is not Config config ||
            !Uri.TryCreate("https://" + config.DomainName, new UriCreationOptions(), out Uri? url))
            return;
        Source = url.OriginalString ?? "";
        SearchBrowser.Source = url;
        IsLoading = true;
    }

    /// <summary>
    /// Navigate to the next page in <see cref="SearchBrowser"/> history
    /// </summary>
    [RelayCommand]
    public void NavigateForward()
    {
        if (IsLoading || !SearchBrowser.CanGoForward) return;
        SearchBrowser.GoForward();
    }

    /// <summary>
    /// Navigate to the previous page in <see cref="SearchBrowser"/> history
    /// </summary>
    [RelayCommand]
    public void NavigateBackward()
    {
        if (IsLoading || !SearchBrowser.CanGoBack) return;
        SearchBrowser.GoBack();
    }

    /// <summary>
    /// Navigate using <see cref="addressBar"/> text. Used in <see cref="addressBar"/> exclusively
    /// </summary>
    /// <seealso cref="SearchBrowser"/>
    [RelayCommand]
    public void Return()
    {
        if (IsLoading || string.IsNullOrEmpty(Source)) return;
        if (!Source.StartsWith("http"))
            Source = "https://" + Source;
        SearchBrowser.Source = Source;
        IsLoading = true;
    }
    #endregion
}