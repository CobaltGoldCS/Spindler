using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using Spindler.Services.Web;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Reader_Pages;
using System.Diagnostics;
using System.Xml.XPath;

namespace Spindler.ViewModels;

public partial class ReaderViewModel : SpindlerViewModel, IReader
{
    #region Class Attributes
    

    /// <summary>
    /// The HttpClient used for Loading book information and/or Determining the correct config
    /// </summary>
    private readonly HttpClient Client;

    /// <summary>
    /// The Book to load ReaderPage information from
    /// </summary>
    public Book CurrentBook = new() { Title = "Loading" };


    private readonly WebScraperBrowser NextChapterBrowser;

    /// <summary>
    /// A cancellation token for the next chapter background service
    /// </summary>
    private CancellationTokenSource nextChapterToken = new();

    #region Bindable Properties

    /// <summary>
    /// The Service managing reader data
    /// </summary>
    [ObservableProperty]
    public ReaderDataService readerService = new(new Config(), new StandardWebService(new()));

    /// <summary>
    /// The Current Data the User interface uses to populate various UI elements
    /// </summary>
    [ObservableProperty]
    public LoadedData currentData = LoadedData.CreatePlaceholder();

    /// <summary>
    /// Flag for whether the View Model is loading book data
    /// </summary>
    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    int firstVisibleParagraphIndex = 0;

    #endregion
    #endregion

    #region Initialization Functions
    /// <summary>
    /// Creates a new ReaderViewModel Instance (please use ReaderViewModelBuilder)
    /// </summary>
    internal ReaderViewModel(IDataService database, HttpClient client, WebScraperBrowser nextChapterBrowser) : base(database)
    {
        Client = client;
        NextChapterBrowser = nextChapterBrowser;
        WeakReferenceMessenger.Default.Send(new StatusColorUpdateMessage("CardBackground"));
        Shell.Current.Navigating += OnShellNavigating;
    }



    /// <summary>
    /// Starts initalizing information from CurrentBook
    /// </summary>
    /// <returns>A task indicating the state of the inital load</returns>
    public async Task StartLoad()
    {
        IsLoading = true;

        var data = await ReaderService.LoadUrl(CurrentBook!.Url);
        switch (data)
        {
            case Result<LoadedData>.Err error:
                await SafeAssert(false, error.Message);
                return;
            case Result<LoadedData>.Ok value:
                CurrentData = value!.Value;
                break;
        };

        IsLoading = false;

        await AutoPopulateImageIfUnknown();

        if (CurrentBook.ParagraphIndex > 0)
        {
            ScrollReader(CurrentBook.ParagraphIndex);
        }

        StartBookChecking();
    }

    private async Task AutoPopulateImageIfUnknown()
    {

        if (!string.IsNullOrEmpty(CurrentBook!.ImageUrl) && CurrentBook!.ImageUrl != "no_image.jpg")
        {
            return;
        }
        var html = await ReaderService.WebService.GetHtmlFromUrl(CurrentBook.Url);

        html.HandleError((error) =>
        {
            return;
        });

        try
        {
            CurrentBook.ImageUrl = ReaderService.ConfigService.PrettyWrapSelector(
                                (html as Result<string>.Ok)!.Value, ConfigService.Selector.ImageUrl, SelectorType.Link);
        }
        catch (XPathException) {
            Toast.Make("Invalid Image Url Selector");
        }
    }

    /// <summary>
    /// Start checking books to see if they have been updated
    /// </summary>
    private async void StartBookChecking()
    {
        List<Book> books = await Database.GetAllItemsAsync<Book>();
        books.RemoveAll(book => book.Completed);
        books.Remove(CurrentBook);

        NextChapterService chapterService = new(Client, NextChapterBrowser, Database);
        _ = Task.Run(async () => await chapterService.SaveBooks(books, nextChapterToken.Token));
    }
    #endregion

    #region Command Definitions

    /// <summary>
    /// A command to change the chapter, activated by the next and previous buttons
    /// </summary>
    /// <param name="selector">Whether the target chapter is the next or previous chapter</param>
    /// <exception cref="InvalidDataException">If Somehow another invalid selector type was used</exception>
    [RelayCommand]
    private async Task ChangeChapter(ReaderDataService.UrlType selector)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ScrollReader(0, isAnimated: false);

        CurrentBook.Url = selector switch
        {
            ReaderDataService.UrlType.Next => CurrentData!.nextUrl,
            ReaderDataService.UrlType.Previous => CurrentData!.prevUrl,
            _ => throw new InvalidDataException("Invalid value for selector; selector must be prev or next url")
        };

        var dataResult = await ReaderService.GetLoadedData(selector, CurrentData!);
        if (dataResult is Result<LoadedData>.Err error)
        {
            await SafeAssert(false, error.Message);
            return;
        }
        IsLoading = false;
        CurrentData = (dataResult as Result<LoadedData>.Ok)!.Value;
        await CurrentBook.SaveInfo(Database);
    }

    /// <summary>
    /// The Click event handler for the Bookmark Button
    /// </summary>
    [RelayCommand]
    public async Task Bookmark()
    {
        if (CurrentData is null || IsLoading)
        {
            return;
        }
        Popup view = new BookmarkDialog(
            Database,
            CurrentBook,
            getNewBookmark: () => new Bookmark(CurrentData.Title!, FirstVisibleParagraphIndex, CurrentData.currentUrl!)
        );

        var result = await Shell.Current.ShowPopupAsync<Bookmark>(view, options: new PopupOptions
        {
            Shape = null
        });


        if (result.Result is not Bookmark bookmark)
        {
            return;
        }

        // UI Changes
        IsLoading = true;

        // Preloaded Data is no longer valid for the bookmark
        ReaderService.InvalidatePreloadedData();

        var data = await ReaderService.LoadUrl(bookmark!.Url);
        switch (data)
        {
            case Result<LoadedData>.Err error:
                await SafeAssert(false, error.Message);
                return;
            case Result<LoadedData>.Ok value:
                CurrentData = value!.Value;
                break;
        };

        // Cleanup 
        IsLoading = false;

        if (bookmark.ParagraphIndex > 0)
        {
            ScrollReader(bookmark.ParagraphIndex);
        }

        CurrentBook!.Url = CurrentData!.currentUrl!;
        await CurrentBook.SaveInfo(Database);
    }

    /// <summary>
    /// Scrolls to the bottom of the view
    /// </summary>
    [RelayCommand]
    public void ScrollBottom() => 
        WeakReferenceMessenger.Default.Send(new ChangeScrollMessage(
            ScrollChangedArgs.ScrollBottom(ReaderService.Config.HasAutoscrollAnimation)));

    private void ScrollReader(int index, bool? isAnimated = null)
    {
        isAnimated ??= ReaderService.Config.HasAutoscrollAnimation;
        WeakReferenceMessenger.Default.Send(
            new ChangeScrollMessage(new(index, (bool)isAnimated)));
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
        nextChapterToken.Cancel();
        if (e.Target.Location.OriginalString == "..")
        {
            CurrentBook.ParagraphIndex = FirstVisibleParagraphIndex;
            CurrentBook.HasNextChapter = CurrentData!.NextUrlValid && !CurrentBook.Completed;
            await CurrentBook.SaveInfo(Database);
        }

        // Set statusbar color back
        WeakReferenceMessenger.Default.Send(new StatusColorUpdateMessage(null));
        Shell.Current.Navigating -= OnShellNavigating;
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
        await NavigateTo(route: $"{nameof(ErrorPage)}", parameters: parameters);
        return false;
    }

    #endregion
}



/// <summary>
/// A Builder for ReaderViewModel. Enforces establishment of all important values
/// </summary>
public class ReaderViewModelBuilder
{
    private readonly ReaderViewModel Target;
    public ReaderViewModelBuilder(IDataService database, HttpClient client, WebScraperBrowser NextChapterBrowser)
    {
        Target = new ReaderViewModel(database, client, NextChapterBrowser);
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
    /// Builds the ReaderViewModel
    /// </summary>
    /// <returns>The Fully assembled ReaderViewModel</returns>
    public ReaderViewModel Build()
    {
        Debug.Assert(Target.CurrentBook is not null);
        Debug.Assert(Target.ReaderService is not null);
        return Target;
    }
}

/// <summary>
/// Message for changing reader scroll position
/// </summary>
/// <remarks>
/// Create a new ScrollChangedMessage
/// </remarks>
/// <param name="value">Arguements relating to scrolling; use ScrollBottom for scrolling to the bottom of view</param>
class ChangeScrollMessage(ScrollChangedArgs value) : ValueChangedMessage<ScrollChangedArgs>(value)
{
}

record ScrollChangedArgs(int index, bool IsAnimated)
{
    public static ScrollChangedArgs ScrollBottom(bool isAnimated) => new(-1, isAnimated);
}
