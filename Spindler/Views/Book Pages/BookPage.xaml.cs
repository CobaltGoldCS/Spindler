namespace Spindler;

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
        list.LastAccessed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(list);
        var binding = new BookViewModel(list.Id, list.Name);
        binding.AddUiReferences(AddToolBarItem);
        BindingContext = binding;
        await binding.Load();
    }

    #endregion

    public BookPage()
    {
        InitializeComponent();
        BooksList.Unfocus();
    }
}