using Spindler.Models;
using Spindler.ViewModels;

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
        var viewmodel = new ReaderViewModel()
        {
            CurrentBook = book,
            Config = book.Config
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