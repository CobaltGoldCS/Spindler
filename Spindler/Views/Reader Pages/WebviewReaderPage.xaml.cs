using Spindler.Models;
using Spindler.Views.Reader_Pages;

namespace Spindler.Views;

public partial class WebviewReaderPage : ContentPage, IQueryAttributable, IReader
{
    public Book? Book = new Book { Id = -1};

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book = (query["book"] as Book)!;
        BindingContext = Book;
        ReaderBrowser.Source = Book.Url;
        await Book.UpdateLastViewedToNow();
    }

    public WebviewReaderPage()
    {
        InitializeComponent();

        Shell.Current.Navigating += OnShellNavigated;
    }

    #region Event Handlers
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
        if (e.Target.Location.OriginalString == "..")
        {
            await App.Database.SaveItemAsync(Book!);
        }
        Shell.Current.Navigating -= OnShellNavigated;
        ReaderBrowser.Navigated -= WebViewOnNavigated;
    }

    #endregion

    // This is safe to do because SafeAssert is never called in WebviewReaderPage
    Task<bool> IReader.SafeAssert(bool condition, string message)
    {
        throw new NotImplementedException();
    }
}