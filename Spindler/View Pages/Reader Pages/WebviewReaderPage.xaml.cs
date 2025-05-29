using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;

namespace Spindler.Views.Reader_Pages;

public partial class WebviewReaderPage : ContentPage, IQueryAttributable, IReader
{
    public Book? Book = new Book { Id = -1 };

    private IDataService DataService;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book = (query["book"] as Book)!;
        BindingContext = Book;
        ReaderBrowser.Source = Book.Url;
        await Book.SaveInfo(DataService);
    }

    public WebviewReaderPage(IDataService dataService)
    {
        InitializeComponent();
        DataService = dataService;
        Shell.Current.Navigating += OnShellNavigating;
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

    [RelayCommand]
    private void Back()
    {
        if (!ReaderBrowser.CanGoBack) return;
        ReaderBrowser.GoBack();
    }

    [RelayCommand]
    private void Reload()
    {
        ReaderBrowser.Reload();
    }

    [RelayCommand]
    private void Forward()
    {
        if (!ReaderBrowser.CanGoForward) return;
        ReaderBrowser.GoForward();
    }

    public async void OnShellNavigating(object? sender,
                           ShellNavigatingEventArgs e)
    {
        if (e.Target.Location.OriginalString == "..")
        {
            await DataService.SaveItemAsync(Book!);
        }
        Shell.Current.Navigating -= OnShellNavigating;
        ReaderBrowser.Navigated -= WebViewOnNavigated;
    }

    #endregion

    // This is safe to do because SafeAssert is never called in WebviewReaderPage
    Task<bool> IReader.SafeAssert(bool condition, string message)
    {
        throw new NotImplementedException();
    }
}