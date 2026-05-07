using CommunityToolkit.Maui;
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

    private bool HasLoaded { get; set; } = false;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // TODO: For some reason, it seems like the MCT popup will specifically try and reopen one of these
        // after using a popup on the BookSearcherPage
        if (HasLoaded)
        {
            return;
        }
        HasLoaded = true;
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
    IPopupService PopupService;
    public BookDetailPage(IDataService dataService, IPopupService popupService)
    {
        InitializeComponent();
        DataService = dataService;
        PopupService = popupService;
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
            await Book.SaveInfo(DataService);
        }
        await Close();
    }

    private async void DeleteButton_clicked(object sender, EventArgs e)
    {
        if (Book!.Id > 0 && await DisplayAlertAsync("Warning!", "Are you sure you want to delete this book?", "Yes", "No"))
        {
            await DataService.DeleteItemAsync(Book);
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
            { "source", Book!.Url ?? "about:blank" }

        };
        // Workaround for a problem with BookSearcherPage being initialized twice for some reason
        // This is why we don't have just one GoToAsync call.
        await Shell.Current.GoToAsync("..");
        await Shell.Current.GoToAsync(nameof(BookSearcherPage), parameters: parameters);

        canRespond = true;
    }

    private async void SwitchBookList_Clicked(object sender, EventArgs e)
    {
        var result = await PopupService.ShowPopupAsync<PickerPopupViewmodel, IIndexedModel>(
            Shell.Current,
            new PopupOptions
            {
                Shape = null,
                Shadow = null
            }, new Dictionary<string, object>
            {
                ["title"] = "Switch Book Lists",
                ["items"] = await DataService.GetAllItemsAsync<BookList>(),
            }
        );
        if (result.WasDismissedByTappingOutsideOfPopup) return;
        Book!.BookListId = ((BookList)result.Result!).Id;
    }

    /// A Regular Expression for selecting letters to capitalize for title case
    [GeneratedRegex("(?<!\\S)\\p{Ll}")]
    private static partial Regex TitleCaseRegex();
    #endregion
}