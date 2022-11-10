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
            okButton.Text = "Add";
            Title = "Add a new Book List";
        }
        else
        {
            okButton.Text = $"Modify {booklist.Name}";
            Title = $"Modify {booklist.Name}";
        }
        if (!BookListDetailViewModel.colorList.Contains(new ChooseColor { color = booklist.Color1 }))
            Booklist.Color1 = Colors.Black;
        if (!BookListDetailViewModel.colorList.Contains(new ChooseColor { color = booklist.Color2 }))
            Booklist.Color2 = Colors.Black;
        BindingContext = new BookListDetailViewModel(booklist);
    }
    #endregion

    public BookListDetailPage()
    {
        InitializeComponent();
        /*UrlEntry.Behaviors.Add(new TextValidationBehavior((string text) =>
        {
            bool validUrl = Uri.TryCreate(text, UriKind.Absolute, out Uri? uriresult) && (uriresult.Scheme == Uri.UriSchemeHttp || uriresult.Scheme == Uri.UriSchemeHttps);
            return validUrl || text.Length == 0;
        }));*/
    }

    private async Task Close()
    {
        // Dismiss keyboard if it is in use in android
        Unfocus();
        await Shell.Current.GoToAsync("..");
    }
}