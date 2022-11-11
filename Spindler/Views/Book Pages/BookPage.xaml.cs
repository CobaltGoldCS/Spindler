namespace Spindler;

using Spindler.Models;
using Spindler.ViewModels;

[QueryProperty(nameof(Booklist), "booklist")]
public partial class BookPage : ContentPage
{

    #region QueryProperty Handler

    private BookList _booklist;
    public BookList Booklist
    {
        get => _booklist;
        set
        {
            if (value == null) throw new ArgumentNullException("BookListId cannot be set to null");
            _booklist = value;
            LoadBookList(value);
        }
    }

    public async void LoadBookList(BookList list)
    {
        list.LastAccessed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(list);
        BindingContext = new BookViewModel(list.Id, list.Name);
        ((BookViewModel)BindingContext).Reload();
    }

    #endregion

    public BookPage()
    {
        InitializeComponent();
        BooksList.Unfocus();
        _booklist = new BookList();
    }
}