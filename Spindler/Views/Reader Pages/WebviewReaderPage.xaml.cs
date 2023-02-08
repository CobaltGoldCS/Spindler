using Spindler.Models;
using Spindler.Views.Reader_Pages;

namespace Spindler.Views;

[QueryProperty(nameof(Book), "book")]
public partial class WebviewReaderPage : ContentPage, IReader
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

    public async void LoadBook()
    {
        BindingContext = Book;
        ReaderBrowser.Source = Book.Url;
        await Book.UpdateLastViewedToNow();
    }
    #endregion

    public WebviewReaderPage()
    {
        InitializeComponent();

        Shell.Current.Navigating += OnShellNavigated;
    }

    private void WebViewOnNavigated(object? sender, WebNavigatedEventArgs @event)
    {
        if (@event.Result != WebNavigationResult.Success) return;
        Book!.Url = @event.Url;
        Book.Position = 0;

        backButton.IsEnabled = ReaderBrowser.CanGoBack;
        forwardButton.IsEnabled = ReaderBrowser.CanGoForward;
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


    Task<bool> IReader.SafeAssert(bool condition, string message)
    {
        throw new NotImplementedException();
    }
}