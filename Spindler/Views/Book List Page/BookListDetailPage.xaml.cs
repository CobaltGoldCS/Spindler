using Spindler.Behaviors;
using Spindler.Models;
using Spindler.ViewModels;
using System.Linq;

namespace Spindler;

public partial class BookListDetailPage : ContentPage, IQueryAttributable
{

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        BookList? booklist = query["booklist"] as BookList;
        // Handle if a new book needs to be created
        if (booklist!.Id < 0)
        {
            AddButtonGroup.OkText = "Add";
            Title = "Add a new Book List";
        }
        else
        {
            AddButtonGroup.OkText = $"Modify {booklist.Name}";
            Title = $"Modify {booklist.Name}";
        }
        BindingContext = new BookListDetailViewModel(booklist);
    }

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