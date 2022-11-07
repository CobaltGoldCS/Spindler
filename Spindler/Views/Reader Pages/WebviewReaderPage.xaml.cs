using Spindler.Models;

namespace Spindler.Views;

[QueryProperty(nameof(Book), "book")]
public partial class WebviewReaderPage : ContentPage
{
    #region Class Attributes
    Book? book;
    #endregion
    #region QueryProperty Handler
    public Book Book
    {
        set
        {
            book = value;
            LoadBook();
        }
        get => book!;
    }
    public void LoadBook()
    {
        BindingContext = Book;
        ReaderBrowser.Source = Book.Url;
        Book.LastViewed = DateTime.UtcNow;
    }
    #endregion

    public WebviewReaderPage()
    {
        InitializeComponent();

        Shell.Current.Navigating += OnShellNavigated;
        ReaderBrowser.Navigated += WebViewOnNavigated;
    }

    private void WebViewOnNavigated(object? sender, WebNavigatedEventArgs @event)
    {
        if (@event.Result != WebNavigationResult.Success) return;
        Book!.Url = @event.Url;
        Book.Position = 0;

        backButton.IsEnabled = ReaderBrowser.CanGoBack;
        forwardButton.IsEnabled = ReaderBrowser.CanGoForward;
    }

    // FIXME: This does not handle android back buttons
    public async void OnShellNavigated(object? sender,
                           ShellNavigatingEventArgs e)
    {
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/WebviewReaderPage")
        {
            await App.Database.SaveItemAsync(Book!);
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
        if (!ReaderBrowser.CanGoForward) return;
        ReaderBrowser.GoForward();
    }
}