
using Spindler.Models;
using Spindler.Services;
using Spindler.Utils;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Spindler.Views;

[QueryProperty(nameof(Book), "book")]
public partial class HeadlessReaderPage : ContentPage
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

    public HeadlessReaderPage()
    {
        InitializeComponent();

        ContentView.FontFamily = Preferences.Default.Get("font", "OpenSans (Regular)");
        ContentView.FontSize = Preferences.Default.Get("font_size", 15);
        ContentView.LineHeight = Preferences.Default.Get("line_spacing", 1.5f);
        TitleView.FontFamily = Preferences.Default.Get("font", "OpenSans (Regular)");


        Shell.Current.Navigating += OnShellNavigated;
    }

    public async void LoadBook(Book Book)
    {
        config = await WebService.FindValidConfig(Book.Url);
        if (await FailIfNull(config, "There is no valid configuration for this book")) return;

        webservice = new(config!);
        Book.LastViewed = DateTime.UtcNow;
        HeadlessBrowser.Source = Book.Url;
        HeadlessBrowser.Navigated += HeadlessBrowser_Navigated;
    }

    private async void HeadlessBrowser_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        var result = e.Result;
        if (result != WebNavigationResult.Success)
        {
            await FailIfNull(null, "Browser was unable to navigate");
            return;
        }
        await LoadContent();
    }

    private async void Bookmark_Clicked(object sender, EventArgs e)
    {
        double prevbuttonheight = PrevButton.IsVisible ? PrevButton.Height : 0;
        double nextbuttonheight = NextButton.IsVisible ? NextButton.Height : 0;
        await App.Database.SaveItemAsync<Book>(new()
        {
            BookListId = Book!.BookListId,
            Id = -1,
            Title = "Bookmark: " + loadedData!.title,
            Url = loadedData.currentUrl!,
            Position = ReadingLayout!.ScrollY / (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight)),
            LastViewed = DateTime.UtcNow,
        });
    }

    private async void PrevButton_Clicked(object sender, EventArgs e)
    {
        Book!.Url = loadedData!.prevUrl!;
        HeadlessBrowser.Source = loadedData!.prevUrl;
        LoadingIndicator.IsRunning = true;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        PrevButton.IsEnabled = false;
    }

    private async void NextButton_Clicked(object sender, EventArgs e)
    {
        Book!.Url = loadedData!.nextUrl!;
        HeadlessBrowser.Source = loadedData!.nextUrl;
        LoadingIndicator.IsRunning = true;
        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        NextButton.IsEnabled = false;
    }

    private async Task LoadContent()
    {
        var html = await HeadlessBrowser.EvaluateJavaScriptAsync(
            "'<html>'+document.getElementsByTagName('html')[0].innerHTML+'</html>';");

        // Decode non-ascii characters
        var builder = new StringBuilder(Regex.Replace(html, @"\\u(?<Value>[a-zA-Z0-9]{4})", m =>
        {
            return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
        }));
        builder.Replace("\\n", "\n").Replace("\\\"", "\"");
        html = builder.ToString();

        loadedData = await webservice!.LoadWebData(Book!.Url, html);
        if (string.IsNullOrEmpty(loadedData.text))
        {
            await AttemptRetry(html);
        }
        if (await FailIfNull(loadedData, "Configuration was unable to obtain values; check Configuration and Url")) return;
        DataChanged();
        retries = 3;
    }

    private int retries = 3;
    private async Task AttemptRetry(string html)
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
        await LoadContent();
        return;
    }

    public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
    {
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/HeadlessReaderPage")
        {
            if (Book != null)
            {
                Book.Position = ReadingLayout.ScrollY / ReadingLayout.ContentSize.Height;
                await App.Database.SaveItemAsync(Book);
            }
        }
        Shell.Current.Navigating -= OnShellNavigated;
    }

    private async void DataChanged()
    {
        if (await FailIfNull(loadedData, "Couldn't get data")) return;

        Title = loadedData!.title ?? "";
        TitleView.Text = loadedData.title ?? "";
        ContentView.Text = loadedData.text ?? "";

        NextButton.IsVisible = WebService.IsUrl(loadedData.nextUrl);
        PrevButton.IsVisible = WebService.IsUrl(loadedData.prevUrl);

        NextButton.IsEnabled = true;
        PrevButton.IsEnabled = true;

        // Turn relative urls into absolutes
        var baseUri = new Uri(loadedData.currentUrl!);
        loadedData.prevUrl = new Uri(baseUri, loadedData.prevUrl).ToString();
        loadedData.nextUrl = new Uri(baseUri, loadedData.nextUrl).ToString();

        Book.LastViewed = DateTime.UtcNow;
        if (Book!.Position > 0)
        {
            await Task.Run(async () =>
            {
                while (ReadingLayout.ContentSize.Height < 657)
                    await Task.Delay(50);
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX,
                     Math.Clamp(Book!.Position, 0d, 1d) * ReadingLayout.ContentSize.Height,
                     config!.ExtraConfigs.GetOrDefault("autoscrollanimation", true));
                    Book.Position = 0;
                });
            });
        }
        LoadingIndicator.IsRunning = false;
        await App.Database.SaveItemAsync(Book);

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
                { "config", await App.Database.GetConfigByDomainNameAsync(new Uri(Book.Url).Host) }
            };
            await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}", parameters);
        }
        return nullobj;
    }
}