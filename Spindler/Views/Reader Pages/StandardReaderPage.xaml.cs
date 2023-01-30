using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.ViewModels;
using Spindler.Views;

namespace Spindler;

[QueryProperty(nameof(Book), "book")]
public partial class StandardReaderPage : ContentPage
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

        var viewmodel = new StandardReaderViewModel()
        {
            CurrentBook = book,
            Config = config
        };
        viewmodel.AttachReferencesToUI(ReadingLayout, BookmarkItem);
        BindingContext = viewmodel;
        await viewmodel.StartLoad();
    }
    #endregion
    public StandardReaderPage()
    {
        InitializeComponent();
    }
}