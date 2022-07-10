using Spindler.Models;

namespace Spindler;

[QueryProperty(nameof(BookId), "id")]
public partial class BookDetailPage : ContentPage
{
	#region QueryProperty Handler
	private int _bookId;
    private int BookListId;
    public string BookId
	{
		set
		{
			// The value of "id" in gotoasync allows us to set two variables for the price of one using |04923102-afb|
			// the first index is the book id, and the second index is the list id
			var ids = value.Split("|04923102-afb|");
			LoadBook(ids[0]);
			_bookId = Convert.ToInt32(ids[0]);
			BookListId = Convert.ToInt32(ids[1]);
        }
	}

	public async void LoadBook(string id)
    {
        int bookId = Convert.ToInt32(id);
        // Handle if a new book needs to be created
        if (bookId < 0)
        {
			// This book is never referenced again, so the values technically do not matter.
			BindingContext = new Book { Id = -1, Url = "", LastViewed = DateTime.UtcNow, Title = "", BookListId = this.BookListId };
			okButton.Text = "Add a new Book";
			Title = "Add a new Book";
			return;
		}
		Book book = await App.Database.GetBookByIdAsync(bookId);
		okButton.Text = $"Modify {book.Title}";
		Title = $"Modify {book.Title}";
		BindingContext = book;

	}
	#endregion

    public BookDetailPage()
    {
        InitializeComponent();
    }

    private async Task Close()
    {
        Unfocus();
        await Shell.Current.GoToAsync("..");
    }

    #region ClickHandlers
    private async void okButton_Clicked(object sender, EventArgs e)
	{
		bool valid = !string.IsNullOrWhiteSpace(nameEntry.Text) && !string.IsNullOrWhiteSpace(urlEntry.Text);

        if (valid)
        {
			Book book = new Book { Id = _bookId, Url = urlEntry.Text, Title = nameEntry.Text, LastViewed = DateTime.UtcNow, BookListId = this.BookListId };
			await App.Database.SaveItemAsync(book);
		}
		await Close();
	}

	private async void DeleteButton_clicked(object sender, EventArgs e)
	{
		if (_bookId > 0)
		{
			await App.Database.DeleteItemAsync(await App.Database.GetBookByIdAsync(_bookId));
		}
		await Close();
	}

	private async void Cancel_Clicked(object sender, EventArgs e)
	{
		await Close();
	}
	#endregion
}