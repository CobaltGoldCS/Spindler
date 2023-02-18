using Spindler.Models;
using Spindler.ViewModels;

namespace Spindler.Views.Book_Pages;

[QueryProperty(nameof(Book), "book")]
public partial class BookPage : ContentPage
{
	public Book Book { set => LoadBook(value); }
	public BookViewModel ViewModel { get; set; }
	public BookPage()
	{
		// TODO: Implement gaussian blur on image background
		InitializeComponent();
		ViewModel = new BookViewModel();
		BindingContext = ViewModel;
    }

	private async void LoadBook(Book book)
	{
		await ViewModel.Load(book);
    }
}