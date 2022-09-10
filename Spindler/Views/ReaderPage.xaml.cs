using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;
using Spindler.Utils;
using Spindler.Views;

namespace Spindler;

[QueryProperty(nameof(BookId), "id")]
public partial class ReaderPage : ContentPage
{
    #region Class Attributes
    public ReaderViewModel readerViewModel;
    #endregion

    #region QueryProperty Handler
    public string BookId
    {
        set
        {
            LoadBook(value);
        }
    }

    private async void LoadBook(string str_id)
    {
        int id = Convert.ToInt32(str_id);
        Book currentBook = await App.Database.GetItemByIdAsync<Book>(id);
        Config config = await WebService.FindValidConfig(currentBook.Url);

        if ((bool?) config?.ExtraConfigs.GetOrDefault("webview", false) ?? false)
        {
            await Shell.Current.GoToAsync($"../{nameof(WebviewReaderPage)}?id={currentBook.Id}");
            return;
        }
        ReaderViewModel viewmodel = new();
        viewmodel.CurrentBook = currentBook;
        viewmodel.Config = config;
        viewmodel.AttachReferencesToUI(ReadingLayout, PrevButton.Height);
        BindingContext = viewmodel;
        await viewmodel.StartLoad();
    }
    #endregion

    public ReaderPage()
    {
        InitializeComponent();

        ContentView.FontFamily = Preferences.Default.Get("font", "OpenSansRegular");
        ContentView.FontSize = Preferences.Default.Get("font_size", 15);
        TitleView.FontFamily = Preferences.Default.Get("font", "OpenSansRegular");
        Shell.Current.Navigating += OnShellNavigated;
    }

    // FIXME: This does not handle android back buttons
    public async void OnShellNavigated(object sender,
                           ShellNavigatingEventArgs e)
    {
        if (e.Current.Location.OriginalString == "//BookLists/BookPage/ReaderPage")
        {
            double prevbuttonheight = PrevButton.IsVisible ? PrevButton.Height : 0;
            double nextbuttonheight = NextButton.IsVisible ? PrevButton.Height : 0;
            var currentbook = (BindingContext as ReaderViewModel).CurrentBook;
            if (currentbook != null)
            {
                currentbook.Position = ReadingLayout.ScrollY / (ReadingLayout.ContentSize.Height - (prevbuttonheight + nextbuttonheight));
                await App.Database.SaveItemAsync(currentbook);
            }
        }
        Shell.Current.Navigating -= OnShellNavigated;
    }

}