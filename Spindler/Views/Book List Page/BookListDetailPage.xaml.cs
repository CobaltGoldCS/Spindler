using Spindler.Behaviors;
using Spindler.Models;

namespace Spindler;

[QueryProperty(nameof(Booklist), "booklist")]
public partial class BookListDetailPage : ContentPage
{
    #region QueryProperty handler
    private BookList _booklist = new BookList();
    public BookList Booklist
    {
        set
        {
            _booklist = value;
            LoadBookList(value);
        }
        get { return _booklist; }
    }

    public void LoadBookList(BookList booklist)
    {
        // Handle if a new book needs to be created
        if (booklist.Id < 0)
        {
            okButton.Text = "Add";
            Title = "Add a new Book List";
            return;
        }
        okButton.Text = $"Modify {booklist.Name}";
        Title = $"Modify {booklist.Name}";
        BindingContext = booklist;
    }
    #endregion

    public BookListDetailPage()
    {
        InitializeComponent();
        imageUrlEntry.Behaviors.Add(new TextValidationBehavior((string text) =>
        {
            bool validUrl = Uri.TryCreate(text, UriKind.Absolute, out Uri? uriresult) && (uriresult.Scheme == Uri.UriSchemeHttp || uriresult.Scheme == Uri.UriSchemeHttps);
            return validUrl || text.Length == 0;
        }));
    }

    private async Task Close()
    {
        // Dismiss keyboard if it is in use in android
        Unfocus();
        await Shell.Current.GoToAsync("..");
    }

    #region Click Handlers

    private async void okButton_Clicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(nameEntry.Text))
        {
            Booklist.ImageUrl = imageUrlEntry.Text;
            Booklist.Name = nameEntry.Text;
            Booklist.LastAccessed = DateTime.UtcNow;
            if (Booklist.ImageUrl.Length == 0)
            {
                Booklist.ImageUrl = BookList.GetRandomPlaceholderImageUrl();
            }
            await App.Database.SaveItemAsync(Booklist);
        }
        await Close();
    }

    private async void DeleteButton_clicked(object sender, EventArgs e)
    {
        if (Booklist.Id > 0)
        {
            await App.Database.DeleteBookListAsync(await App.Database.GetItemByIdAsync<BookList>(Booklist.Id));
        }
        await Close();
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Close();
    }

    #endregion
}