﻿using CommunityToolkit.Maui.Views;
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

public partial class ReaderViewModel : ObservableObject, IReader
{
    #region Class Attributes
    
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
    /// The current Scroll position of the view
    /// </summary>
    [ObservableProperty]
    double readerScrollPosition = 0;

    #endregion
    #endregion

    #region Initialization Functions
    /// <summary>
    /// Creates a new ReaderViewModel Instance (please use ReaderViewModelBuilder)
    /// </summary>
    internal ReaderViewModel(IDataService database, HttpClient client, WebScraperBrowser nextChapterBrowser)
    {
        IsLoading = true;
        Database = database;
        Client = client;
        NextChapterBrowser = nextChapterBrowser;
        Shell.Current.Navigating += OnShellNavigating;
    }



    /// <summary>
    /// Starts initalizing information from CurrentBook
    /// </summary>
    /// <returns>A task indicating the state of the inital load</returns>
    public async Task StartLoad()
    {

        await CurrentBook.UpdateViewTimeAndSave(Database);

        NextChapterService chapterService = new(Client, NextChapterBrowser, Database);

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

        // Get image url from load
        if (string.IsNullOrEmpty(CurrentBook!.ImageUrl) || CurrentBook!.ImageUrl == "no_image.jpg")
        {
            await SetImageUrl();
        }
        ChangeChapter();

        List<Book> books = await Database.GetAllItemsAsync<Book>();
        books.Remove(CurrentBook);
        _ = Task.Run(async () => await chapterService.SaveBooks(books, nextChapterToken.Token));
    }

    private async Task SetImageUrl()
    {
        var html = await ReaderService.WebService.GetHtmlFromUrl(CurrentBook.Url);

        if (html is Result<string>.Err) return;

        try
        {
            CurrentBook.ImageUrl = ReaderService.ConfigService.PrettyWrapSelector(
                                (html as Result<string>.Ok)!.Value, ConfigService.Selector.ImageUrl, SelectorType.Link);
        }
        catch (XPathException) { }
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
            return;

        IsLoading = true;
        PrevButtonIsVisible = false;
        NextButtonIsVisible = false;
        WeakReferenceMessenger.Default.Send(new ChangeScrollMessage((0, false)));
        CurrentBook.Position = 0;

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
        CurrentData = (dataResult as Result<LoadedData>.Ok)!.Value;
        ChangeChapter();
    }

    /// <summary>
    /// The Click event handler for the Bookmark Button
    /// </summary>
    [RelayCommand]
    public async Task Bookmark()
    {
        if (CurrentData is null || IsLoading)
            return;
        Popup view = new BookmarkDialog(
            Database,
            CurrentBook,
            getNewBookmark: () => new Bookmark(CurrentData.Title!, ReaderScrollPosition, CurrentData.currentUrl!)
        );

        WeakReferenceMessenger.Default.Send(new CreateBottomSheetMessage(view));
        if (await PopupExtensions.ShowPopupAsync(Shell.Current.CurrentPage, view) is not Bookmark bookmark)
        {
            return;
        }

        // UI Changes
        IsLoading = true;
        PrevButtonIsVisible = false;
        NextButtonIsVisible = false;

        // Preloaded Data is no longer valid for the bookmark
        ReaderService.InvalidatePreloadedData();

        WeakReferenceMessenger.Default.Send(new ChangeScrollMessage((0, false)));
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
        ChangeChapter();
        if (bookmark.Position > 0)
            WeakReferenceMessenger.Default.Send(new ChangeScrollMessage((bookmark.Position, false)));
        CurrentBook = await Database.GetItemByIdAsync<Book>(CurrentBook.Id);
    }

    /// <summary>
    /// Scrolls to the bottom of the view
    /// </summary>
    [RelayCommand]
    public static void ScrollBottom()
    {
        WeakReferenceMessenger.Default.Send(new ChangeScrollMessage((-1, true)));
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
            CurrentBook.Position = ReaderScrollPosition;
            CurrentBook.HasNextChapter = NextButtonIsVisible;
            await Database.SaveItemAsync(CurrentBook);
        }
        Shell.Current.Navigating -= OnShellNavigating;
    }

    #region Helperfunctions

    /// <summary>
    /// Updates the UI with current information from <see cref="CurrentData"/>
    /// </summary>
    private async void ChangeChapter()
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
            WeakReferenceMessenger.Default.Send(new ChangeScrollMessage((CurrentBook.Position, ReaderService.Config.HasAutoscrollAnimation)));
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
/// A message containing position (double) and isAnimated (bool)
/// </summary>
/// <remarks>
/// Create a new ScrollChangedMessage
/// </remarks>
/// <param name="value">position (double) and isAnimated (bool). If position is negative, scroll to the bottom of the view</param>
class ChangeScrollMessage((double, bool) value) : ValueChangedMessage<(double, bool)>(value)
{
}
