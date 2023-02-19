namespace Spindler.Views;

using Spindler.Models;
using Spindler.ViewModels;

[QueryProperty(nameof(Booklist), "booklist")]
public partial class BookListPage : ContentPage
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

        var binding = new BookListViewModel(list);
        binding.AddUiReferences(AddToolBarItem);
        BindingContext = binding;
        await binding.Load();
    }

    #endregion

    public BookListPage()
    {
        InitializeComponent();
        BooksList.Unfocus();
    }
}