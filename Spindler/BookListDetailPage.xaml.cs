using Spindler.Models;

namespace Spindler;

[QueryProperty(nameof(BooklistId), "id")]
public partial class BookListDetailPage : ContentPage
{
    #region QueryProperty handler
    private int _booklistId;
	public string BooklistId
	{
		set
		{
			LoadBookList(value);
			_booklistId = Convert.ToInt32(value);
		}
	}

    public async void LoadBookList(string id)
    {
        int booklistId = Convert.ToInt32(id);
        // Handle if a new book needs to be created
        if (booklistId < 0)
        {

            BindingContext = new BookList { Id = -1, ImageUrl = "", LastAccessed = DateTime.UtcNow, Name = "" };
            okButton.Text = "Add a new Book List";
            Title = "Add a new Book List";
            return;
        }
        BookList list = await App.Database.GetBooklistByIdAsync(booklistId);
        okButton.Text = $"Modify {list.Name}";
        Title = $"Modify {list.Name}";
        BindingContext = list;
    }
    #endregion

    public BookListDetailPage()
	{
		InitializeComponent();
	}

    private async Task Close()
    {
        // Dismiss keyboard if it is in use in android
        Unfocus();
        await Shell.Current.GoToAsync("..");
    }

    #region Click Handlers

    private async void okButton_Clicked(object sender, EventArgs e)
    {
		if (!string.IsNullOrWhiteSpace(nameEntry.Text))
        {
			BookList list = new BookList { 
				Id = _booklistId, 
				ImageUrl = imageUrlEntry.Text,
				Name = nameEntry.Text,
				LastAccessed = DateTime.UtcNow
			};
			if (list.ImageUrl.Length == 0)
            {
                list.ImageUrl = BookList.GetRandomPlaceholderImageUrl();
            }
			await App.Database.SaveItemAsync(list);
		}
		await Close();
    }

    private async void DeleteButton_clicked(object sender, EventArgs e)
    {
		if (_booklistId > 0 &&
			nameEntry.Text.Length > 0 &&
			imageUrlEntry.Text.Length > 0)
        {
			await App.Database.DeleteBookListAsync(await App.Database.GetBooklistByIdAsync(_booklistId));
		}
		await Close();
	}

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
		await Close();
	}

    #endregion
}