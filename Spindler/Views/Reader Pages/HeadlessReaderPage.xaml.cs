using CommunityToolkit.Maui.Alerts;
using HtmlAgilityPack;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views.Reader_Pages;
using System.ComponentModel;

namespace Spindler.Views;

public partial class HeadlessReaderPage : ContentPage, INotifyPropertyChanged, IReader, IQueryAttributable
{
    // Both of these are guaranteed not null after initial page loading
    WebService? webservice;
    Config? config;

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

    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book = (query["book"] as Book)!;
        HeadlessBrowser.Navigated += PageLoaded;
        HeadlessBrowser.Source = Book.Url;
        NextChapterService chapterService = new();
        await Book.UpdateLastViewedToNow();

        await chapterService.CheckChaptersInBookList(Book.BookListId, cancellationTokenSource.Token, NextChapterBrowser);
    }

    public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
    {
        if (e.Target.Location.OriginalString == "..")
        {
            if (Book != null)
            {
                Book.Position = ReadingLayout.ScrollY / ReadingLayout.ContentSize.Height;
                await Book.UpdateLastViewedToNow();
            }
        }
        cancellationTokenSource.Cancel();
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

            if (string.IsNullOrEmpty(Book.ImageUrl) || Book!.ImageUrl == "no_image.jpg")
                Book.ImageUrl = ConfigService.PrettyWrapSelector(doc, new Models.Path(config!.ImageUrlPath), ConfigService.SelectorType.Link);
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
            Title = "Bookmark: " + loadedData!.Title,
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
        bool foundMatchingContent = await HeadlessBrowser.WaitUntilValid(new Models.Path(config!.ContentPath), TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(20));
        
        if (!await SafeAssert(foundMatchingContent, "Unable to get html specified by configuration")) return;

        LoadedData = await webservice!.LoadWebData(Book!.Url, await HeadlessBrowser.GetHtml());

        if (!await SafeAssert(LoadedData.Title != "afb-4893", "Invalid Url")) return;

        if (!await SafeAssert(!string.IsNullOrEmpty(LoadedData.Text), "Unable to obtain text content")) return;

        UpdateUiUsingLoadedData();
    }

    private async void UpdateUiUsingLoadedData()
    {
        NextVisible = WebService.IsUrl(LoadedData.nextUrl);
        PrevVisible = WebService.IsUrl(LoadedData.prevUrl);

        // Turn relative urls into absolutes
        var baseUri = new Uri(LoadedData.currentUrl!);
        LoadedData.prevUrl = new Uri(baseUri, LoadedData.prevUrl).ToString();
        LoadedData.nextUrl = new Uri(baseUri, LoadedData.nextUrl).ToString();
        Book!.HasNextChapter = NextVisible;

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
                 (bool)config!.ExtraConfigs.GetValueOrDefault("autoscrollanimation", true));
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
            await Shell.Current.GoToAsync($"/{nameof(ErrorPage)}", parameters);
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