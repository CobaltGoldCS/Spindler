using CommunityToolkit.Mvvm.Input;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views.Reader_Pages;
using System.ComponentModel;

namespace Spindler.Views;

public partial class HeadlessReaderPage : ContentPage, INotifyPropertyChanged, IReader, IQueryAttributable
{
    ReaderDataService? ReaderService { get; set; } = null;

    Book Book = new Book { Id = -1 };


    #region Binding Definitions

    LoadedData loadedData = new() { Title = "Loading" };
    public LoadedData LoadedData
    {
        get => loadedData;
        set
        {
            loadedData = value;
            OnPropertyChanged();
        }
    }

    bool prevVisible = false;
    public bool PrevVisible
    {
        get => prevVisible;
        set
        {
            prevVisible = value;
            OnPropertyChanged();
        }
    }

    bool nextVisible = false;
    public bool NextVisible
    {
        get => nextVisible;
        set
        {
            nextVisible = value;
            OnPropertyChanged();
        }
    }

    bool isLoading = true;
    public bool IsLoading
    {
        get => isLoading;
        set
        {
            isLoading = value;
            OnPropertyChanged();
        }
    }

    #endregion

    public HeadlessReaderPage()
    {
        InitializeComponent();
        Shell.Current.Navigating += OnShellNavigated;
    }

    readonly CancellationTokenSource nextChapterTaskToken = new();

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book = (query["book"] as Book)!;

        ReaderService = new ReaderDataService(new Config(), new HeadlessWebService(HeadlessBrowser));


        if (!await SafeAssert(await ReaderService.setConfigFromUrl(Book.Url), "Could not get configuration information from website"))
            return;

        NextChapterService chapterService = new();
        await Book.UpdateViewTimeAndSave();
        await Task.Run(async () =>
        {
            IEnumerable<Book> updateQueue = await chapterService.CheckChaptersInBookList(Book, nextChapterTaskToken.Token);
            await Dispatcher.DispatchAsync(async () => await App.Database.SaveItemsAsync(updateQueue));
        });

        await GetContentAsLoadedData(Book.Url);
        await UpdateUiUsingLoadedData();
    }

    [RelayCommand]
    private async void Bookmark()
    {
        await App.Database.SaveItemAsync<Book>(new()
        {
            BookListId = Book!.BookListId,
            Title = "Bookmark: " + loadedData!.Title,
            Url = loadedData.currentUrl!,
            Position = ReadingLayout!.ScrollY / ReadingLayout.ContentSize.Height,
        });
    }

    [RelayCommand]
    private async void PrevButton()
    {
        IsLoading = true;
        Book!.Url = loadedData!.prevUrl!;
        PrevVisible = false;
        NextVisible = false;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        await GetContentAsLoadedData(Book.Url);
        await UpdateUiUsingLoadedData();
    }

    [RelayCommand]
    private async void NextButton()
    {
        IsLoading = true;
        PrevVisible = false;
        NextVisible = false;
        Book!.Url = loadedData!.nextUrl!;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        await GetContentAsLoadedData(Book.Url);
        await UpdateUiUsingLoadedData();
    }

    /// <summary>
    /// Find and extract content into <see cref="LoadedData"/>
    /// </summary>
    private async Task GetContentAsLoadedData(string url)
    {
        LoadedData? foundMatchingContent = await ReaderService!.LoadUrl(url);

        if (!await SafeAssertNotNull(foundMatchingContent, "Unable to get html specified by configuration"))
            return;

        LoadedData = foundMatchingContent!;

        if (!await SafeAssert(LoadedData.Title != "afb-4893", "Invalid Url"))
            return;

        if (!await SafeAssert(!string.IsNullOrEmpty(LoadedData.Text), "Content path unable to match html"))
            return;

    }

    /// <summary>
    /// Updates the User Interface using <see cref="LoadedData"/>
    /// </summary>
    private async Task UpdateUiUsingLoadedData()
    {
        NextVisible = WebUtilities.IsUrl(LoadedData.nextUrl);
        PrevVisible = WebUtilities.IsUrl(LoadedData.prevUrl);

        // Turn relative urls into absolutes
        var baseUri = new Uri(LoadedData.currentUrl!);
        LoadedData.prevUrl = new Uri(baseUri, LoadedData.prevUrl).ToString();
        LoadedData.nextUrl = new Uri(baseUri, LoadedData.nextUrl).ToString();


        await ScrollToLastReadPositionIfApplicable();
        IsLoading = false;
    }

    /// <summary>
    /// If the book has a previous position, scroll to it
    /// </summary>
    private async Task ScrollToLastReadPositionIfApplicable()
    {
        if (Book.Position <= 0)
        {
            return;
        }

        var shouldAnimate = (bool)ReaderService!.Config.ExtraConfigs.GetValueOrDefault("autoscrollanimation", true);
        await Task.Run(async () =>
        {
            while (ReadingLayout.ContentSize.Height < 1000)
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, Book.Position, shouldAnimate);
                Book.Position = 0;
            });
        });
    }

    public async void OnShellNavigated(object? _,
                           ShellNavigatingEventArgs e)
    {
        if (e.Target.Location.OriginalString == "..")
        {
            Book.Position = ReadingLayout.ScrollY;
            Book.HasNextChapter = NextVisible;
            await Book.UpdateViewTimeAndSave();
        }
        // This will safely cancel the nextChapter task
        nextChapterTaskToken.Cancel();

        Shell.Current.Navigating -= OnShellNavigated;
    }

    /// <summary>
    /// Assert that <paramref name="condition"/> is true or fail with <see cref="ErrorPage"/>
    /// </summary>
    /// <param name="condition">The condition to assert if true</param>
    /// <param name="message">A message for the <see cref="ErrorPage"/> to display</param>
    /// <returns>The result of the condition</returns>
    public async Task<bool> SafeAssert(bool condition, string message)
    {
        if (condition)
            return true;

#pragma warning disable CS8604 // Possible null reference argument.
        Dictionary<string, object> parameters = new()
        {
            { "errormessage", message },
            { "config", ReaderService?.Config }
        };
#pragma warning restore CS8604 

        await Shell.Current.GoToAsync($"/{nameof(ErrorPage)}", parameters);
        return false;
    }

    /// <summary>
    /// Assert that <paramref name="value"/> is not null, or gracefully fail
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="message">The error to pass to the user</param>
    /// <returns>True if the value is not null, or false if it is </returns>
    public async Task<bool> SafeAssertNotNull(object? value, string message)
    {
        return await SafeAssert(value is not null, message);
    }
}