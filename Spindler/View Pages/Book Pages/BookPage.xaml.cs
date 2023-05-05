using Spindler.Models;
using Spindler.ViewModels;

#if ANDROID
using Spindler.Platforms.Android;
#elif IOS
using Spindler.Platforms.iOS;
#endif

namespace Spindler.Views.Book_Pages;

public partial class BookPage : ContentPage, IQueryAttributable
{
    public BookViewModel ViewModel { get; set; }

    public BookPage()
    {
        // TODO: Implement gaussian blur on image background
        InitializeComponent();
        ViewModel = new BookViewModel();
        BindingContext = ViewModel;

#if ANDROID || IOS
        BackgroundImage.Behaviors.Add(new PrimaryColorsBehavior());
#endif

    }


    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Book? book = query["book"] as Book;
        await ViewModel.Load(BackgroundImage, book!);
    }
}