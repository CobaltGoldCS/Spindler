namespace Spindler;

using Spindler.Models;
using Spindler.ViewModels;

[QueryProperty(nameof(BooklistId), "id")]
public partial class BookPage : ContentPage
{

    #region QueryProperty Handler

    private int _id = -1;
    public string BooklistId
    {
        get => _id.ToString();
        set
        {
            if (value == null) throw new ArgumentNullException("BookListId cannot be set to null");
            LoadBookList(Convert.ToInt32(value));
            _id = Convert.ToInt32(value);
        }
    }

    public async void LoadBookList(int id)
    {
        BookList list = await App.Database.GetItemByIdAsync<BookList>(id);
        list.LastAccessed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(list);
        BindingContext = new BookViewModel(list.Id, list.Name);
    }

    #endregion

    public BookPage()
    {
        InitializeComponent();
        BooksList.Unfocus();
    }

    #region Click Handlers

    private async void ConfigButtonPressed(object sender, EventArgs e)
    {
        int id = await Task.Run(() => Convert.ToInt32(((ImageButton)sender).BindingContext));
        await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}?id={id}|04923102-afb|{BooklistId}");
    }

    private async void addToolbarItem_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}?id=-1|04923102-afb|{BooklistId}");
    }

    bool executing = false;
    private async void ItemClicked(object sender, EventArgs e)
    {
        if (executing) return;
        executing = true;
        Book book = (Book)((Frame)sender).BindingContext;
        await Shell.Current.GoToAsync($"{nameof(ReaderPage)}?id={book.Id}");
        executing = false;
    }

    #endregion
}