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
        ReaderBrowser.Source = currentBook.Url;
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
}