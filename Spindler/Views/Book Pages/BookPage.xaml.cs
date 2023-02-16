namespace Spindler.Views.Book_Pages;

using Spindler.Models;
using Spindler.ViewModels;

[QueryProperty(nameof(Booklist), "booklist")]
public partial class BookPage : ContentPage
{

    #region QueryProperty Handler

    public BookList Booklist
    {
        set
        {
            LoadBookList(value);
        }
    }

    public async void LoadBookList(BookList list)
    {
        await list.UpdateAccessTimeToNow();

        var binding = new BookViewModel(list);
        binding.AddUiReferences(AddToolBarItem);
        BindingContext = binding;
        binding.Load();
    }

    #endregion

    public BookPage()
    {
        InitializeComponent();
        BooksList.Unfocus();
    }
}