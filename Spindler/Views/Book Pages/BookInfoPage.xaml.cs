using Spindler.Models;
using Spindler.ViewModels;

namespace Spindler.Views.Book_Pages;

[QueryProperty(nameof(Book), "book")]
public partial class BookInfoPage : ContentPage
{
	public Book Book { set => LoadBook(value); }
	public BookInfoViewModel ViewModel { get; set; }
	public BookInfoPage()
	{
		// TODO: Implement gaussian blur on image background
		InitializeComponent();
		ViewModel = new BookInfoViewModel();
		BindingContext = ViewModel;
    }

	private void LoadBook(Book book)
	{
		ViewModel.Load(book);
    }
}