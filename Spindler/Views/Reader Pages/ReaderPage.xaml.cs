using Spindler.Models;
using Spindler.Services;
using Spindler.Utils;
using Spindler.ViewModels;
using Spindler.Views;
using System.Security;

namespace Spindler;

[QueryProperty(nameof(Book), "book")]
public partial class ReaderPage : ContentPage
{

    #region QueryProperty Handler
    public Book Book
    {
        set
        {
            LoadBook(value);
        }
    }

    private async void LoadBook(Book book)
    {
        Config? config = await WebService.FindValidConfig(book.Url);

        var webview = config?.ExtraConfigs.GetOrDefault("webview", false) ?? false;
        if (webview)
        {
            Dictionary<string, object> parameters = new()
            {
                { "book", book }
            };
            await Shell.Current.GoToAsync($"../{nameof(WebviewReaderPage)}", parameters);
            return;
        }

        var headless = config?.ExtraConfigs.GetOrDefault("headless", false) ?? false;
        if (headless)
        {
            Dictionary<string, object> parameters = new()
            {
                { "book", book }
            };
            await Shell.Current.GoToAsync($"../{nameof(HeadlessReaderPage)}", parameters);
            return;
        }

        var viewmodel = new ReaderViewModel()
        {
            CurrentBook = book,
            Config = config
        };
        viewmodel.AttachReferencesToUI(ReadingLayout, BookmarkItem);
        BindingContext = viewmodel;
        await viewmodel.StartLoad();
    }
    #endregion
    public ReaderPage()
    {
        InitializeComponent();
    }
}