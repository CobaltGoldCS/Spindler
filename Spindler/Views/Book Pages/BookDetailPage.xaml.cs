using CommunityToolkit.Maui.Views;
using Spindler.Behaviors;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Views.Book_Pages;

namespace Spindler;

[QueryProperty(nameof(Book), "book")]
public partial class BookDetailPage : ContentPage
{
    #region QueryProperty Handler
    private Book _book = new();
    public Book Book
    {
        set
        {
            _book = value;
            LoadBook(value);
        }
        get => _book;
    }

    public void LoadBook(Book book)
    {
        if (book.Id < 0)
        {
            AddButtonGroup.OkText = "Add";
            Title = "Add a new Book";
            return;
        }
        AddButtonGroup.OkText = "Modify";
        Title = $"Modify {book.Title}";
        BindingContext = book;

    }
    #endregion

    public BookDetailPage()
    {
        InitializeComponent();
        urlEntry.Behaviors.Add(new TextValidationBehavior((string text) =>
        {
            return Uri.TryCreate(text, UriKind.Absolute, out Uri? uriresult) && (uriresult.Scheme == Uri.UriSchemeHttp || uriresult.Scheme == Uri.UriSchemeHttps);
        }));
    }

    private async Task Close()
    {
        Unfocus();
        await Shell.Current.GoToAsync("..");
    }

    #region ClickHandlers
    private async void okButton_Clicked(object sender, EventArgs e)
    {
        bool valid = !string.IsNullOrWhiteSpace(nameEntry.Text) && !string.IsNullOrWhiteSpace(urlEntry.Text);

        if (valid)
        {
            Book.Url = urlEntry.Text;
            Book.Title = nameEntry.Text;
            await App.Database.SaveItemAsync(Book);
        }
        await Close();
    }

    private async void DeleteButton_clicked(object sender, EventArgs e)
    {
        if (!await DisplayAlert("Warning!", "Are you sure you want to delete this book?", "Yes", "No"))
        {
            return;
        }
        if (Book.Id > 0)
        {
            await App.Database.DeleteItemAsync(Book);
        }
        await Close();
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Close();
    }

    private async void Search_clicked(object sender, EventArgs e)
    {
        Dictionary<string, object> parameters = new()
        {
            { "bookListId", Book.BookListId }

        };
        await Shell.Current.GoToAsync(nameof(BookSearcherPage), parameters: parameters);
    }

    private async void SwitchBookList_Clicked(object sender, EventArgs e)
    {
        var popup = new PickerPopup("Switch Book Lists", await App.Database.GetBookListsAsync());
        object? result = await this.ShowPopupAsync(popup);
        if (result is not BookList list) return;
        Book.BookListId = list.Id;
    }    
    #endregion
}