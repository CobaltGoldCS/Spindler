using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;

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
            LoadBook(Convert.ToInt32(value));
        }
    }

    private async void LoadBook(int id)
    {
        Book currentBook = await App.Database.GetItemByIdAsync<Book>(id);
        Config config = await WebService.FindValidConfig(currentBook.Url);
        BindingContext = new ReaderViewModel();
        ((ReaderViewModel)BindingContext).CurrentBook = currentBook;
        ((ReaderViewModel)BindingContext).Config = config;
        ((ReaderViewModel)BindingContext).AttachReferencesToUI(ReadingLayout, PrevButton);
        await ((ReaderViewModel)BindingContext).StartLoad();
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
            double buttonheight = PrevButton.IsVisible ? PrevButton.Height : 0;
            ((ReaderViewModel)BindingContext).CurrentBook.Position = ReadingLayout.ScrollY / (ReadingLayout.ContentSize.Height - buttonheight);

            await App.Database.SaveItemAsync(((ReaderViewModel)BindingContext).CurrentBook);
        }
        Shell.Current.Navigating -= OnShellNavigated;
    }

}