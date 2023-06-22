using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using HtmlAgilityPack;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;

namespace Spindler.Views.Book_Pages;

[QueryProperty(nameof(BooklistId), "bookListId")]
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
    /// The book list id to attach to any books created in Book Searcher Page
    /// </summary>
    public int BooklistId { get; set; } = new();

    /// <summary>
    /// The currently used configuration for detecting books
    /// </summary>
    public Config? Config { get; set; } = null;

    /// <summary>
    /// Whether <see cref="SearchBrowser"/> is busy loading and checking a page or not
    /// </summary>
    public bool IsLoading = false;


    private string source = "https://example.com";
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


    public BookSearcherPage()
    {
        InitializeComponent();
        SwitchUiBasedOnState(State.BookNotFound);
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
        if (e.Result != WebNavigationResult.Success) return;
        await SearchProgress.ProgressTo(.5, 500, Easing.BounceOut);
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
        await SearchProgress.ProgressTo(0, 0, Easing.BounceOut);
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
        string html = await SearchBrowser.GetHtml();

        Config = await Config.FindValidConfig(!string.IsNullOrEmpty(url) ? url : SearchBrowser.GetUrl(), html);
        if (Config is null) return;

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        try
        {
            string content = ConfigService.PrettyWrapSelector(doc, new Models.Path(Config.ContentPath), ConfigService.SelectorType.Text);

            SwitchUiBasedOnState(!string.IsNullOrEmpty(content) ? State.BookFound : State.BookNotFound);
        } catch (Exception e)
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
    ///     BookListId: <see cref="BooklistId"/>,  
    ///     Title: <see cref="Config.TitlePath"/> or ""  
    ///     Url: <see cref="Source"/>   
    /// }  
    /// </code>
    /// </summary>
    [RelayCommand]
    private async void CreateBookFromConfigInformation()
    {
        string html = await SearchBrowser.GetHtml();

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        string title = ConfigService.PrettyWrapSelector(doc, new Models.Path(Config!.TitlePath), ConfigService.SelectorType.Text);
        await App.Database.SaveItemAsync(
            new Book
            {
                BookListId = BooklistId,
                Title = title,
                Url = Source
            }
        );
        SwitchUiBasedOnState(State.BookSaved);
    }

    /// <summary>
    /// Creates a popup that allows you to choose a configuration to load the url of
    /// </summary>
    [RelayCommand]
    private async void UseConfigAsDomainUrl()
    {
        if (IsLoading) return;
        PickerPopup popup = new("Choose a config to search", await App.Database.GetAllItemsAsync<Config>());
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
        if (IsLoading || Source is null) return;
        if (!Source.StartsWith("http"))
            Source = "https://" + Source;
        SearchBrowser.Source = source;
        IsLoading = true;
    }
    #endregion
}