using CommunityToolkit.Maui.Views;
using Spindler.Behaviors;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using System.Text.RegularExpressions;

namespace Spindler.Views.Book_Pages;

public partial class BookDetailPage : ContentPage, IQueryAttributable
{
    #region QueryProperty Handler
    Book? Book;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book = query["book"] as Book;
        if (Book!.Id < 0)
        {
            AddButtonGroup.OkText = "Add";
            Title = "Add a new Book";
            return;
        }
        AddButtonGroup.OkText = "Modify";
        Title = $"Modify {Book.Title}";
        BindingContext = Book;
    }
    #endregion

    IDataService DataService;
    public BookDetailPage(IDataService dataService)
    {
        InitializeComponent();
        DataService = dataService;
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
            Book!.Url = urlEntry.Text;
            // Convert the book's title to title case
            Book.Title = TitleCaseRegex().Replace(nameEntry.Text, m => m.Value.ToUpper());
            Book.ImageUrl = imageUrlEntry.Text;
            await Book.UpdateViewTimeAndSave(DataService);
        }
        await Close();
    }

    private async void DeleteButton_clicked(object sender, EventArgs e)
    {
        if (Book!.Id > 0 && await DisplayAlert("Warning!", "Are you sure you want to delete this book?", "Yes", "No"))
        {
            await App.Database.DeleteItemAsync(Book);
        }
        await Close();
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Close();
    }

    bool canRespond = true;
    private async void Search_clicked(object sender, EventArgs e)
    {
        if (!canRespond)
            return;
        canRespond = false;
        Dictionary<string, object?> parameters = new()
        {
            { "source", Book?.Url ?? "localhost" }

        };
        await Shell.Current.GoToAsync(nameof(BookSearcherPage), parameters: parameters);
        canRespond = true;
    }

    private async void SwitchBookList_Clicked(object sender, EventArgs e)
    {
        var popup = new PickerPopup("Switch Book Lists", await App.Database.GetBookListsAsync());
        object? result = await this.ShowPopupAsync(popup);
        if (result is not BookList list) return;
        Book!.BookListId = list.Id;
    }

    /// A Regular Expression for selecting letters to capitalize for title case
    [GeneratedRegex("(?<!\\S)\\p{Ll}")]
    private static partial Regex TitleCaseRegex();
    #endregion
}