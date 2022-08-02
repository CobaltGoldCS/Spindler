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
        Config config = await App.Database.GetConfigByDomainNameAsync(new UriBuilder(currentBook.Url).Host);
        BindingContext = new ReaderViewModel();
        ((ReaderViewModel)BindingContext).CurrentBook = currentBook;
        ((ReaderViewModel)BindingContext).Config = config;
        ((ReaderViewModel)BindingContext).AttachReferencesToUI(ReadingLayout);
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
            ((ReaderViewModel)BindingContext).CurrentBook.Position = ReadingLayout.ScrollY;
            await App.Database.SaveItemAsync(((ReaderViewModel)BindingContext).CurrentBook);
        }
        Shell.Current.Navigating -= OnShellNavigated;
    }

}