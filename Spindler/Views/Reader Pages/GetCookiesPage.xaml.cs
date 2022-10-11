using Newtonsoft.Json;
using Spindler.Models;
using System.Net;

namespace Spindler.Views;

[QueryProperty(nameof(BookId), "id")]
public partial class GetCookiesPage : ContentPage
{
    #region Class Attributes
    Book? currentBook;
    #endregion
    #region QueryProperty Handler
    public string BookId
    {
        set
        {
            LoadBook(Convert.ToInt32(value));
        }
    }
    /// <summary>
    /// Loads the Page based on book <paramref name="id"/>
    /// </summary>
    /// <param name="id">The book id to load</param>
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

    private void WebViewOnNavigated(object? sender, WebNavigatedEventArgs @event)
    {
        if (@event.Result != WebNavigationResult.Success) return;
        currentBook!.Url = @event.Url;
        currentBook.Position = 0;

        backButton.IsEnabled = ReaderBrowser.CanGoBack;
        forwardButton.IsEnabled = ReaderBrowser.CanGoForward;
    }

    // FIXME: This does not handle android back buttons
    public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
    {
        DetachEventHandlers();
        // This should only be called when the back buttons are clicked
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/ReaderPage/GetCookiesPage")
        {
            await App.Database.SaveItemAsync(currentBook!);
            // Properly redirect down two pages (so that we don't run into an inactive ReaderPage)
            e.Cancel();
            await Shell.Current.GoToAsync("../..");
            return;
        }
    }
    /// Event that fires on the back button
    private void Back_Clicked(object sender, EventArgs e)
    {
        if (!ReaderBrowser.CanGoBack) return;
        ReaderBrowser.GoBack();
    }
    /// Event that fires on the reload button
    private void Reload_Clicked(object sender, EventArgs e)
    {
        ReaderBrowser.Reload();
    }
    /// Event that fires on the forward button
    private void Forward_Clicked(object sender, EventArgs e)
    {
        if(!ReaderBrowser.CanGoForward) return;
        ReaderBrowser.GoForward();
    }
    /// <summary>
    /// Event that fires when the 'Get Cookies' button is clicked
    /// </summary>
    /// <param name="sender">The sender of the event (should be a Button)</param>
    /// <param name="e">The arguments of the event (goes unused)</param>
    private async void GetCookies_Clicked(object sender, EventArgs e)
    {
        // Detaching these first allows us to avoid triggering OnShellNavigated
        DetachEventHandlers();
        await App.Database.SaveItemAsync(currentBook!);
        App.SharedValues.cookies = ReaderBrowser.Cookies;

        await Shell.Current.GoToAsync("..?&id="+currentBook!.Id);
    }

    /// <summary>
    /// Detach event handlers so that we don't accidentally cause a memory leak when navigating between pages
    /// </summary>
    private void DetachEventHandlers()
    {
        Shell.Current.Navigating -= OnShellNavigated;
        ReaderBrowser.Navigated -= WebViewOnNavigated;
    }
}