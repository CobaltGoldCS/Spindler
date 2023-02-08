using HtmlAgilityPack;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views.Reader_Pages;
using System.ComponentModel;

namespace Spindler.Views;

[QueryProperty(nameof(Book), "book")]
public partial class HeadlessReaderPage : ContentPage, INotifyPropertyChanged, IReader
{
    // Both of these are guaranteed not null after initial page loading
    WebService? webservice;
    Config? config;
    LoadedData? loadedData;
    Book book = new Book();
    public Book Book
    {
        set
        {
            book = value;
            LoadBook(value);
        }
        get => book;
    }

    #region Binding Definitions

    string? text;
    public string? Text
    {
        get => text;
        set
        {
            text = value;
            OnPropertyChanged();
        }
    }

    string? readerTitle;
    public string? ReaderTitle
    {
        get => readerTitle;
        set
        {
            readerTitle = value;
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

    public async void LoadBook(Book Book)
    {
        HeadlessBrowser.Navigated += PageLoaded;
        HeadlessBrowser.Source = Book.Url;
        await Book.UpdateLastViewedToNow();
    }

    public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
    {
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/HeadlessReaderPage" && e.Target.Location.OriginalString != "../ErrorPage")
        {
            if (Book != null)
            {
                Book.Position = ReadingLayout.ScrollY / ReadingLayout.ContentSize.Height;
                await Book.UpdateLastViewedToNow();
            }
        }
        Shell.Current.Navigating -= OnShellNavigated;
    }

    private async void PageLoaded(object? sender, WebNavigatedEventArgs e)
    {
        // Define Config before accidentally breaking something
        if (config is null)
        {
            string html = await HeadlessBrowser.GetHtml();
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            config = await WebService.FindValidConfig(Book.Url, html);
            if (!await SafeAssertNotNull(config, "There is no valid configuration for this book")) return;
            webservice = new(config!);

            if (string.IsNullOrEmpty(book.ImageUrl) || book!.ImageUrl == "no_image.jpg")
                book.ImageUrl = ConfigService.PrettyWrapSelector(doc, new Models.Path(config!.ImageUrlPath), ConfigService.SelectorType.Link) ?? "";
        }

        var result = e.Result;
        if (!await SafeAssert(result == WebNavigationResult.Success, $"Browser was unable to navigate.")) return;

        await GetContentAsLoadedData();
    }

    private async void Bookmark_Clicked(object sender, EventArgs e)
    {
        await App.Database.SaveItemAsync<Book>(new()
        {
            BookListId = Book!.BookListId,
            Title = "Bookmark: " + loadedData!.title,
            Url = loadedData.currentUrl!,
            Position = ReadingLayout!.ScrollY / ReadingLayout.ContentSize.Height,
        });
    }

    private async void PrevButton_Clicked(object sender, EventArgs e)
    {
        IsLoading = true;
        Book!.Url = loadedData!.prevUrl!;
        HeadlessBrowser.Source = loadedData!.prevUrl;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        PrevVisible = false;
        NextVisible = false;
    }

    private async void NextButton_Clicked(object sender, EventArgs e)
    {
        IsLoading = true;
        Book!.Url = loadedData!.nextUrl!;
        HeadlessBrowser.Source = loadedData!.nextUrl;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        PrevVisible = false;
        NextVisible = false;
    }

    private async Task GetContentAsLoadedData()
    {
        bool foundMatchingContent = await HeadlessBrowser.WaitUntilValid(new Models.Path(config!.ContentPath), TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(20));
        
        if (!await SafeAssert(foundMatchingContent, "Unable to get html specified by configuration")) return;

        loadedData = await webservice!.LoadWebData(Book!.Url, await HeadlessBrowser.GetHtml());

        if (!await SafeAssert(loadedData.title != "afb-4893", "Invalid Url")) return;

        if (!await SafeAssert(!string.IsNullOrEmpty(loadedData.text), "Unable to obtain text content")) return;

        if (!await SafeAssertNotNull(loadedData, "Configuration was unable to obtain values; check Configuration and Url")) return;
        UpdateUiUsingLoadedData();
    }

    private async void UpdateUiUsingLoadedData()
    {
        if (!await SafeAssertNotNull(loadedData, "Couldn't get data")) return;

        Title = loadedData!.title ?? "";
        ReaderTitle = loadedData!.title ?? "";

        Text = loadedData.text ?? "";

        NextVisible = WebService.IsUrl(loadedData.nextUrl);
        PrevVisible = WebService.IsUrl(loadedData.prevUrl);

        // Turn relative urls into absolutes
        var baseUri = new Uri(loadedData.currentUrl!);
        loadedData.prevUrl = new Uri(baseUri, loadedData.prevUrl).ToString();
        loadedData.nextUrl = new Uri(baseUri, loadedData.nextUrl).ToString();

        await ScrollToLastReadPositionIfApplicable();
        IsLoading = false;
        await App.Database.SaveItemAsync(Book);

    }

    private async Task ScrollToLastReadPositionIfApplicable()
    {
        if (Book!.Position <= 0)
        {
            return;
        }

        await Task.Run(async () =>
        {
            while (ReadingLayout.ContentSize.Height < 1000)
                await Task.Delay(50);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX,
                 Math.Clamp(Book!.Position, 0d, 1d) * ReadingLayout.ContentSize.Height,
                 config!.ExtraConfigs.GetOrDefault("autoscrollanimation", true));
                Book.Position = 0;
            });
        });
    }

    
    public async Task<bool> SafeAssert(bool condition, string message)
    {
        if (!condition)
        {
            Dictionary<string, object> parameters = new()
            {
                { "errormessage", message },
                { "config", config! }
            };
            await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}", parameters);
        }
        return condition;
    }

    /// <summary>
    /// Assert that <paramref name="value"/> is not null, or gracefully fail
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="message">The error to pass to the user</param>
    /// <returns>True if the value is not null, or false if it is </returns>
    public async Task<bool> SafeAssertNotNull(object? value, string message) => await SafeAssert(value is not null, message);
}