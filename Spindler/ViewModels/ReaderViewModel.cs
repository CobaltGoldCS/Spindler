using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Reader_Pages;
using System.Diagnostics;
using System.Xml.XPath;

namespace Spindler.ViewModels;

public partial class ReaderViewModel : ObservableObject, IReader, IRecipient<BookmarkClickedMessage>
{
    #region Class Attributes
    /// <summary>
    /// The Service managing reader data
    /// </summary>
    public ReaderDataService ReaderService = new(new Config(), new StandardWebService(new()));
    /// <summary>
    /// The reference to the underlying database
    /// </summary>
    private readonly IDataService Database;

    /// <summary>
    /// The HttpClient used for Loading book information and/or Determining the correct config
    /// </summary>
    private readonly HttpClient Client;

    /// <summary>
    /// The Book to load ReaderPage information from
    /// </summary>
    public Book CurrentBook = new() { Title = "Loading" };

    /// <summary>
    /// A cancellation token for the next chapter background service
    /// </summary>
    public CancellationTokenRegistration nextChapterToken = new();

    /// <summary>
    /// The button to open the Bookmark menu
    /// </summary>
    public Button BookmarkButton = default!;
    /// <summary>
    /// The Scroll View that manages the reader
    /// </summary>
    public ScrollView ReadingLayout = default!;

    #region Bindable Properties

    /// <summary>
    /// The Current Data the User interface uses to populate various UI elements
    /// </summary>
    [ObservableProperty]
    public LoadedData? currentData = new() { Title = "Loading" };

    /// <summary>
    /// Flag for whether the View Model is loading book data
    /// </summary>
    [ObservableProperty]
    private bool isLoading;


    /// <summary>
    /// Visibility of the 'Previous' Button
    /// </summary>
    [ObservableProperty]
    public bool prevButtonIsVisible = false;

    /// <summary>
    /// Visibility of the 'Next' Button
    /// </summary>
    [ObservableProperty]
    public bool nextButtonIsVisible = false;

    /// <summary>
    /// The current Scroll position of the <see cref="ReadingLayout"/>
    /// </summary>
    double ReaderScrollPosition = 0;

    /// <summary>
    /// An event to update the <see cref="ReaderScrollPosition"/> whenever <see cref="ReadingLayout"/> is scrolled
    /// </summary>
    /// <param name="sender"><see cref="ReadingLayout"/></param>
    /// <param name="e">The events sent by the Scroll view</param>
    public async void Scrolled(object? sender, ScrolledEventArgs e)
    {
        await Task.Run(() =>
        {
            ReaderScrollPosition = e.ScrollY;
            if (ReaderScrollPosition.IsZeroOrNaN())
                return;

            CurrentBook.Position = ReaderScrollPosition;
        });
    }
    #endregion
    #endregion

    #region Initialization Functions
    /// <summary>
    /// Creates a new ReaderViewModel Instance (please use ReaderViewModelBuilder)
    /// </summary>
    public ReaderViewModel(IDataService database, HttpClient client)
    {
        IsLoading = true;
        Database = database;
        Client = client;
        Shell.Current.Navigating += OnShellNavigating;
        WeakReferenceMessenger.Default.Register(this);
    }



    /// <summary>
    /// Starts initalizing information from CurrentBook
    /// </summary>
    /// <returns>A task indicating the state of the inital load</returns>
    public async Task StartLoad()
    {
        NextChapterService chapterService = new(Client);
        await CurrentBook.UpdateViewTimeAndSave(Database);
        IDispatcher? dispatcher = Dispatcher.GetForCurrentThread();
        _ = Task.Run(async () =>
        {
            IEnumerable<Book> updateQueue = await chapterService.CheckChaptersInBookList(CurrentBook, nextChapterToken.Token);
            await dispatcher!.DispatchAsync(async () => await App.Database.SaveItemsAsync(updateQueue));
        });

        var data = await ReaderService.LoadUrl(CurrentBook!.Url);
        switch (data)
        {
            case Invalid<LoadedData> error:
                await SafeAssert(false, error.Value.getMessage());
                return;
            case Ok<LoadedData> value:
                CurrentData = value!.Value;
                break;
        };

        // Get image url from load
        if (string.IsNullOrEmpty(CurrentBook!.ImageUrl) || CurrentBook!.ImageUrl == "no_image.jpg")
        {
            var html = await ReaderService.WebService.GetHtmlFromUrl(CurrentBook.Url);

            if (html is Invalid<string>) return;

            try
            {
                CurrentBook.ImageUrl = ReaderService.ConfigService.PrettyWrapSelector(
                                    (html as Ok<string>)!.Value, ConfigService.Selector.ImageUrl, ConfigService.SelectorType.Link);
            }
            catch (XPathException) { }
        }
        DataChanged();
    }
    #endregion

    #region Command Definitions

    /// <summary>
    /// A command to change the chapter, activated by the next and previous buttons
    /// </summary>
    /// <param name="selector">Whether the target chapter is the next or previous chapter</param>
    /// <exception cref="InvalidDataException">If Somehow another invalid selector type was used</exception>
    [RelayCommand]
    private async void ChangeChapter(ReaderDataService.UrlType selector)
    {
        if (IsLoading)
            return;
        
        IsLoading = true;
        PrevButtonIsVisible = false;
        NextButtonIsVisible = false;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        CurrentBook.Position = 0;

        CurrentBook.Url = selector switch
        {
            ReaderDataService.UrlType.Next => CurrentData!.nextUrl,
            ReaderDataService.UrlType.Previous => CurrentData!.prevUrl,
            _ => throw new InvalidDataException("Invalid value for selector; selector must be prev or next url")
        };

        var dataResult = await ReaderService.GetLoadedData(selector, CurrentData!);
        if(dataResult is Invalid<LoadedData> error)
        {
            await SafeAssert(false, error.Value.getMessage());
            return;
        }
        CurrentData = (dataResult as Ok<LoadedData>)!.Value;
        DataChanged();
    }

    /// <summary>
    /// The Click event handler for the <see cref="BookmarkButton"/>
    /// </summary>
    [RelayCommand]
    public void Bookmark()
    {
        if (CurrentData is null || IsLoading)
            return;
        Popup view = new BookmarkDialog(
            Database, 
            CurrentBook, 
            getNewBookmark: () => new Bookmark(CurrentData.Title!, ReaderScrollPosition, CurrentData.currentUrl!)
        );

        WeakReferenceMessenger.Default.Send(new CreateBottomSheetMessage(view));
    }

    public async void Receive(BookmarkClickedMessage message)
    {
        if (message.Value is null)
            return;
        var bookmark = message.Value;
        // UI Changes
        IsLoading = true;
        PrevButtonIsVisible = false;
        NextButtonIsVisible = false;

        // Preloaded Data is no longer valid for the bookmark
        ReaderService.InvalidatePreloadedData();

        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        var data = await ReaderService.LoadUrl(bookmark!.Url);
        switch (data)
        {
            case Invalid<LoadedData> error:
                await SafeAssert(false, error.Value.getMessage());
                return;
            case Ok<LoadedData> value:
                CurrentData = value!.Value;
                break;
        };

        // Cleanup 
        IsLoading = false;
        DataChanged();
        if (bookmark.Position > 0)
            await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, bookmark.Position, false);
        CurrentBook = await Database.GetItemByIdAsync<Book>(CurrentBook.Id);
    }

    /// <summary>
    /// Scrolls to the bottom of the <see cref="ReadingLayout"/>
    /// </summary>
    [RelayCommand]
    public async void ScrollBottom()
    {
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, ReadingLayout.Content.Height - ReadingLayout.Bounds.Bottom, true);
    }
    #endregion

    

    /// <summary>
    /// Overrides navigation methods
    /// </summary>
    /// <param name="sender">The sender of the navigation events</param>
    /// <param name="e">The navigation events</param>
    public async void OnShellNavigating(object? sender,
                       ShellNavigatingEventArgs e)
    {
        if (e.Target.Location.OriginalString == "..")
        {
            CurrentBook.HasNextChapter = NextButtonIsVisible;
            await App.Database.SaveItemAsync(CurrentBook);
        }
        ReadingLayout.Scrolled -= Scrolled;
        Shell.Current.Navigating -= OnShellNavigating;
    }

    #region Helperfunctions

    /// <summary>
    /// Updates the UI with current information from <see cref="CurrentData"/>
    /// </summary>
    private async void DataChanged()
    {
        // Database updates
        CurrentBook!.Url = CurrentData!.currentUrl!;
        await CurrentBook.UpdateViewTimeAndSave(Database);

        NextButtonIsVisible = WebUtilities.IsUrl(CurrentData.nextUrl);
        PrevButtonIsVisible = WebUtilities.IsUrl(CurrentData.prevUrl);

        // Turn relative uris into absolutes
        var baseUri = new Uri(CurrentData.currentUrl!);
        CurrentData.prevUrl = new Uri(baseUri: baseUri, CurrentData.prevUrl).ToString();
        CurrentData.nextUrl = new Uri(baseUri: baseUri, CurrentData.nextUrl).ToString();

        IsLoading = false;
        
        if (CurrentBook.Position > 0)
        {
            await ReadingLayout.ScrollToAsync(
                    0,
                    CurrentBook.Position,
                    ReaderService.Config.HasAutoscrollAnimation);
        }
    }

    #region Error Handlers

    public async Task<bool> SafeAssert(bool condition, string message)
    {
        if (condition)
            return true;

        Dictionary<string, object?> parameters = new()
        {
            { "errormessage", message },
            { "config", ReaderService.Config }
        };
        await Shell.Current.GoToAsync($"{nameof(ErrorPage)}", parameters);
        return false;
    }

    #endregion
    #endregion
}



/// <summary>
/// A Builder for ReaderViewModel. Enforces establishment of all important values
/// </summary>
public class ReaderViewModelBuilder
{
    private readonly ReaderViewModel Target;
    public ReaderViewModelBuilder(IDataService database, HttpClient client)
    {
        Target = new ReaderViewModel(database, client);
    }

    /// <summary>
    /// Sets the data service that is in charge of managing data
    /// </summary>
    /// <param name="readerService">The service in charge of data management</param>
    /// <returns>The Reader View Model Builder</returns>
    public ReaderViewModelBuilder SetRequiredInfo(ReaderDataService readerService)
    {
        Target.ReaderService = readerService;
        return this;
    }

    /// <summary>
    /// Sets relevant book information for the ViewModel
    /// </summary>
    /// <param name="book">The book to reference</param>
    /// <returns>The Reader view model builder</returns>
    public ReaderViewModelBuilder SetCurrentBook(Book book)
    {
        Target.CurrentBook = book;
        return this;
    }

    /// <summary>
    /// Sets references to important Views
    /// </summary>
    /// <param name="readingLayout">The Scroll View of the UI</param>
    /// <param name="bookmarkButton">The bookmarking button</param>
    /// <returns>Itself</returns>
    public ReaderViewModelBuilder SetReferencesToUI(ScrollView readingLayout, Button bookmarkButton)
    {
        Target.ReadingLayout = readingLayout;
        Target.BookmarkButton = bookmarkButton;
        return this;
    }

    /// <summary>
    /// Builds the ReaderViewModel
    /// </summary>
    /// <returns>The Fully assembled ReaderViewModel</returns>
    public ReaderViewModel Build()
    {
        Debug.Assert(Target.ReadingLayout is not null);
        Debug.Assert(Target.BookmarkButton is not null);
        Debug.Assert(Target.CurrentBook is not null);
        Debug.Assert(Target.ReaderService is not null);
        return Target;
    }
}
