using Spindler.Models;
using Spindler.ViewModels;


namespace Spindler.Views.Book_Pages;

public partial class BookPage : ContentPage, IQueryAttributable
{
    public BookViewModel ViewModel { get; set; }

    public BookPage(HttpClientHandler handler)
    {
        InitializeComponent();
        ViewModel = new BookViewModel(new(handler));
        BindingContext = ViewModel;
    }


    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book? book = query["book"] as Book;
        await ViewModel.Load(BackgroundImage, book!);
    }
}