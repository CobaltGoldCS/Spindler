using Newtonsoft.Json;
using Spindler.Models;
using System.Net;

namespace Spindler.Views;

[QueryProperty(nameof(BookId), "id")]
public partial class GetCookiesPage : ContentPage
{
    #region Class Attributes
    Book currentBook;
    #endregion
    #region QueryProperty Handler
    public string BookId
    {
        set
        {
            LoadBook(Convert.ToInt32(value));
        }
    }
    public async void LoadBook(int id)
    {
        currentBook = await App.Database.GetItemByIdAsync<Book>(id);
        BindingContext = currentBook;
        ReaderBrowser.Source = currentBook.Url;
        currentBook.LastViewed = DateTime.UtcNow;
    }
    #endregion

    public GetCookiesPage()
	{
		InitializeComponent();
        Shell.Current.Navigating += OnShellNavigated;
        ReaderBrowser.Navigated += WebViewOnNavigated;
    }

    private void WebViewOnNavigated(object sender, WebNavigatedEventArgs @event)
    {
        if (@event.Result != WebNavigationResult.Success) return;
        currentBook.Url = @event.Url;
        currentBook.Position = 0;

        backButton.IsEnabled = ReaderBrowser.CanGoBack;
        forwardButton.IsEnabled = ReaderBrowser.CanGoForward;
    }

    // FIXME: This does not handle android back buttons
    public async void OnShellNavigated(object sender,
                           ShellNavigatingEventArgs e)
    {
        Shell.Current.Navigating -= OnShellNavigated;
        ReaderBrowser.Navigated -= WebViewOnNavigated;
        // This should only be called when the back buttons are clicked
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/ReaderPage/GetCookiesPage")
        {
            await App.Database.SaveItemAsync(currentBook);
            // Properly redirect down two pages (so that we don't run into an inactive ReaderPage)
            e.Cancel();
            await Shell.Current.GoToAsync("../..");
            return;
        }
    }

    private void Back_Clicked(object sender, EventArgs e)
    {
        if (!ReaderBrowser.CanGoBack) return;
        ReaderBrowser.GoBack();
    }

    private void Reload_Clicked(object sender, EventArgs e)
    {
        ReaderBrowser.Reload();
    }

    private void Forward_Clicked(object sender, EventArgs e)
    {
        if(!ReaderBrowser.CanGoForward) return;
        ReaderBrowser.GoForward();
    }

    private async void getCookies_Clicked(object sender, EventArgs e)
    {
        // Get readerbrowser cookies
        var cookies = ReaderBrowser
            .Cookies?
            .GetCookies(new Uri(currentBook.Url, UriKind.Absolute))
            .OfType<Cookie>()
            .ToList();
        Shell.Current.Navigating -= OnShellNavigated;
        if (cookies != null)
        {
            string stringifiedcookies = JsonConvert.SerializeObject(cookies, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            await Shell.Current.GoToAsync($"..?cookies={stringifiedcookies}&id={currentBook.Id}");
            return;
        }
        await Shell.Current.GoToAsync("..?cookies=[]&id="+currentBook.Id);
    }
}