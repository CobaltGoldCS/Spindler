
using Spindler.Models;
using Spindler.Services;
using Spindler.Utils;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Spindler.Views;

[QueryProperty(nameof(BookId), "id")]
public partial class HeadlessReaderPage : ContentPage
{
	// Both of these are guaranteed not null after initial page loading
	WebService? webservice;
	Book? currentbook;
    Config? config;
	LoadedData? loadedData;

	public string BookId
	{
		set
		{
			LoadBook(Convert.ToInt32(value));
		}
	}
	public HeadlessReaderPage()
	{
		InitializeComponent();

        ContentView.FontFamily = Preferences.Default.Get("font", "OpenSans (Regular)");
        ContentView.FontSize = Preferences.Default.Get("font_size", 15);
        TitleView.FontFamily = Preferences.Default.Get("font", "OpenSans (Regular)");
        

		Shell.Current.Navigating += OnShellNavigated;
    }

	public async void LoadBook(int id)
	{
		currentbook = await App.Database.GetItemByIdAsync<Book>(id);
	    config = await WebService.FindValidConfig(currentbook.Url);
		if (await FailIfNull(config, "There is no valid configuration for this book")) return;
		webservice = new(config!);
		currentbook.LastViewed = DateTime.UtcNow;
		HeadlessBrowser.Source = currentbook.Url;
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
            BookListId = currentbook!.BookListId,
            Id = -1,
            Title = "Bookmark: " + loadedData!.title,
            Url = loadedData.currentUrl!,
            Position = ReadingLayout!.ScrollY / (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight)),
            LastViewed = DateTime.UtcNow,
        });
    }

	private async void PrevButton_Clicked(object sender, EventArgs e)
	{
        HeadlessBrowser.Source = loadedData!.prevUrl;
        PrevButton.IsEnabled = false;
    }

	private async void NextButton_Clicked(object sender, EventArgs e)
	{
        HeadlessBrowser.Source = loadedData!.nextUrl;
        NextButton.IsEnabled = false;
    }


    private int retries = 3;
    private async Task LoadContent()
    {
        var html = await HeadlessBrowser.EvaluateJavaScriptAsync("'<html>'+document.getElementsByTagName('html')[0].innerHTML+'</html>';");
        // Decode non-ascii characters
        html = Regex.Replace(html,@"\\u(?<Value>[a-zA-Z0-9]{4})", m => {
            return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
        }).Replace("\\n", "\n").Replace("\\\"", "\"");
        loadedData = await webservice!.LoadWebData(currentbook!.Url, html);
        if (string.IsNullOrEmpty(loadedData.text))
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
        if (await FailIfNull(loadedData, "Configuration was unable to obtain values; check Configuration and Url")) return;
        if (currentbook.Position > 0)
        {
            await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX,
                Math.Clamp(currentbook!.Position, 0d, 1d) * (ReadingLayout.ContentSize.Height - (PrevButton.Height + NextButton.Height)),
                config!.ExtraConfigs.GetOrDefault("autoscrollanimation", true));
        }
        DataChanged();
        retries = 3;
    }

    public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
    {
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/ReaderPage")
        {
            double prevbuttonheight = PrevButton.IsVisible ? PrevButton.Height : 0;
            double nextbuttonheight = NextButton.IsVisible ? PrevButton.Height : 0;
            if (currentbook != null)
            {
                currentbook.Position = ReadingLayout.ScrollY / (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight));
                await App.Database.SaveItemAsync(currentbook);
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

        currentbook!.Url = loadedData.currentUrl!;
        currentbook.LastViewed = DateTime.UtcNow;
        currentbook.Position = 0;
        await App.Database.SaveItemAsync(currentbook);

        await ReadingLayout.ScrollToAsync(ReadingLayout.ScrollX, 0, false);
        
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
            await Shell.Current.GoToAsync($"../{nameof(ErrorPage)}?id={currentbook!.Id}&errormessage={message}");
        }
        return nullobj;
    }

    private async Task DelayScroll(bool autoscrollanimation)
    {
        await Task.Run(async () =>
        {
            double prevbuttonheight = PrevButton.IsVisible ? PrevButton.Height : 0;
            double nextbuttonheight = NextButton.IsVisible ? NextButton.Height : 0;
            await Task.Delay(100);
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                    await ReadingLayout!.ScrollToAsync(ReadingLayout.ScrollX,
                    Math.Clamp(currentbook!.Position, 0d, 1d) * (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight)),
                    autoscrollanimation);
            });
        });
    }
}