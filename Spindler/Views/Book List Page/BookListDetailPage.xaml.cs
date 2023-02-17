using Spindler.Behaviors;
using Spindler.Models;
using Spindler.ViewModels;
using System.Linq;

namespace Spindler;

[QueryProperty(nameof(Booklist), "booklist")]
public partial class BookListDetailPage : ContentPage
{
    #region QueryProperty handler
    private BookList _booklist = new();
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
            AddButtonGroup.OkText = "Add";
            Title = "Add a new Book List";
        }
        else
        {
            AddButtonGroup.OkText = $"Modify {booklist.Name}";
            Title = $"Modify {booklist.Name}";
        }
        BindingContext = new BookListDetailViewModel(booklist, () => DisplayAlert("Warning!", "Are you sure you want to delete this booklist?", "Yes", "No"));
    }
    #endregion

    public BookListDetailPage()
    {
        InitializeComponent();
        /*UrlEntry.Behaviors.Add(new TextValidationBehavior((string text) =>
        {
            bool validUrl = Uri.TryCreate(text, UriKind.Absolute, out Uri? url) && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);
            return validUrl || text.Length == 0;
        }));*/
    }
}