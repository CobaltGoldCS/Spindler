using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;

namespace Spindler;

public partial class BookListDetailPage : ContentPage, IQueryAttributable
{
    IDataService DataService { get; set; }

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
        BindingContext = new BookListDetailViewModel(DataService, booklist);
    }

    public BookListDetailPage(IDataService dataService)
    {
        InitializeComponent();
        this.DataService = dataService;
        /*UrlEntry.Behaviors.Add(new TextValidationBehavior((string text) =>
        {
            bool validUrl = Uri.TryCreate(text, UriKind.Absolute, out Uri? url) && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);
            return validUrl || text.Length == 0;
        }));*/
    }
}