
using Spindler.Models;
using Spindler.Services;
using Spindler.Utils;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Spindler.Views;

[QueryProperty(nameof(Book), "book")]
public partial class HeadlessReaderPage : ContentPage, INotifyPropertyChanged
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
        config = await WebService.FindValidConfig(Book.Url);
        if (await FailIfNull(config, "There is no valid configuration for this book")) return;

        webservice = new(config!);
        HeadlessBrowser.Source = Book.Url;
        HeadlessBrowser.Navigated += PageLoaded;
    }

    private async void PageLoaded(object? sender, WebNavigatedEventArgs e)
    {
        var result = e.Result;
        if (result != WebNavigationResult.Success)
        {
            await FailIfNull(null, $"Browser was unable to navigate.");
            return;
        }
        await GetContentAsLoadedData();
    }

    private async void Bookmark_Clicked(object sender, EventArgs e)
    {
        await App.Database.SaveItemAsync<Book>(new()
        {
            BookListId = Book!.BookListId,
            Id = -1,
            Title = "Bookmark: " + loadedData!.title,
            Url = loadedData.currentUrl!,
            Position = ReadingLayout!.ScrollY / ReadingLayout.ContentSize.Height,
            LastViewed = DateTime.UtcNow,
        });
    }

    private async void PrevButton_Clicked(object sender, EventArgs e)
    {
        IsLoading = true;
        Book!.Url = loadedData!.prevUrl!;
        HeadlessBrowser.Source = loadedData!.prevUrl;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        PrevVisible = false;
    }

    private async void NextButton_Clicked(object sender, EventArgs e)
    {
        IsLoading = true;
        Book!.Url = loadedData!.nextUrl!;
        HeadlessBrowser.Source = loadedData!.nextUrl;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        NextVisible = false;
    }

    private async Task GetContentAsLoadedData()
    {
        var html = await HeadlessBrowser.EvaluateJavaScriptAsync(
            "'<html>'+document.getElementsByTagName('html')[0].innerHTML+'</html>';");
        html = DecodeRawHtml(html);

        loadedData = await webservice!.LoadWebData(Book!.Url, html);
        if (string.IsNullOrEmpty(loadedData.text))
        {
            await RetryLoadingTextContent(html);
        }
        if (await FailIfNull(loadedData, "Configuration was unable to obtain values; check Configuration and Url")) return;
        UpdateUiUsingLoadedData();
        retries = 3;
    }
    
    /// <summary>
    /// Decode raw html from webview
    /// </summary>
    /// <param name="html">Raw output html</param>
    /// <returns></returns>
    private static string DecodeRawHtml(string html)
    {
        var builder = new StringBuilder(Regex.Replace(html, @"\\u(?<Value>[a-zA-Z0-9]{4})", m =>
        {
            return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
        }));
        builder.Replace("\\n", "\n").Replace("\\\"", "\"");
        html = builder.ToString();
        return html;
    }

    private int retries = 3;
    private async Task RetryLoadingTextContent(string html)
    {
        if (retries == 0)
        {
            await FailIfNull(null, "Unable to obtain text content");
            return;
        }
        Task.Delay(500).Wait();
        retries--;
        // Check for cloudflare and increase retry count if it is found
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);
        if (ConfigService.CssElementHandler(doc, "h2.challenge-running", ConfigService.SelectorType.Text) is not null) retries = 3;
        await GetContentAsLoadedData();
        return;
    }

    private async void UpdateUiUsingLoadedData()
    {
        if (await FailIfNull(loadedData, "Couldn't get data")) return;

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

    /// <summary>
    /// Display <paramref name="message"/> if <paramref name="value"/> is <c>null</c>
    /// </summary>
    /// <param name="value">The value to check for nullability</param>
    /// <param name="message">The message to display</param>
    /// <returns>If the object is null or not</returns>
    private async Task<bool> FailIfNull(object? value, string message)
    {
        bool nullobj = value == null;
        if (nullobj)
        {
            Dictionary<string, object> parameters = new()
            {
                { "errormessage", message },
                { "config", config! }
            };
            await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}", parameters);
        }
        return nullobj;
    }

    public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
    {
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/HeadlessReaderPage")
        {
            if (Book != null)
            {
                Book.Position = ReadingLayout.ScrollY / ReadingLayout.ContentSize.Height;
                Book.LastViewed = DateTime.UtcNow;
                await App.Database.SaveItemAsync(Book);
            }
        }
        Shell.Current.Navigating -= OnShellNavigated;
    }
}