using Spindler.Models;
using Spindler.Services;

namespace Spindler.Views;

[QueryProperty(nameof(BookId), "id")]
public partial class WebviewReaderPage : ContentPage
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

    public WebviewReaderPage()
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
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/WebviewReaderPage")
        {
            await App.Database.SaveItemAsync(currentBook);
        }
        Shell.Current.Navigating -= OnShellNavigated;
        ReaderBrowser.Navigated -= WebViewOnNavigated;
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
}