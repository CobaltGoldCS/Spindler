using Spindler.Models;
using Spindler.ViewModels;

namespace Spindler.Views.Book_Pages;

[QueryProperty(nameof(Book), "book")]
public partial class BookInfoPage : ContentPage
{
	public Book Book { set => LoadBook(value); }
	public BookInfoPage()
	{
		InitializeComponent();
		BindingContext = new BookInfoViewModel();
    }

	private void LoadBook(Book book)
	{
        (BindingContext as BookInfoViewModel)!.Load(book!);
    }
}