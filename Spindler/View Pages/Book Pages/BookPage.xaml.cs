using Spindler.Models;
using Spindler.Services;
using Spindler.ViewModels;


namespace Spindler.Views.Book_Pages;

public partial class BookPage : ContentPage, IQueryAttributable
{
    public BookViewModel ViewModel { get; set; }

    public BookPage(IDataService database, HttpClient client)
    {
        InitializeComponent();
        ViewModel = new BookViewModel(database, client);
        BindingContext = ViewModel;
    }


    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book? book = query["book"] as Book;
        await ViewModel.Load(book!);
    }
}