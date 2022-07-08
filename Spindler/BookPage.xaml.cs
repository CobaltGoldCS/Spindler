namespace Spindler;

using Spindler.Models;
[QueryProperty(nameof(BooklistId), "id")]
public partial class BookPage : ContentPage
{

    #region QueryProperty Handler

    private int _id = -1;
    public string BooklistId 
	{
		get => _id.ToString();
		set 
		{ 
			if (value == null) throw new ArgumentNullException("BookListId cannot be set to null");
			LoadBookList(Convert.ToInt32(value));
			_id = Convert.ToInt32(value);
		}
	}

    public async void LoadBookList(int id)
    {
        BookList list = await App.Database.GetBookListByIdAsync(id);
        list.LastAccessed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(list);
        BindingContext = list;
        BooksList.ItemsSource = await App.Database.GetBooksByBooklistIdAsync(id);

        loaded = true;
    }

	#endregion

	public BookPage()
	{
		InitializeComponent();
	}


    private bool loaded = false;
    protected override async void OnAppearing()
	{
		base.OnAppearing();
		BooksList.Unfocus();
		int id;
		if (loaded && int.TryParse(BooklistId, out id))
        {
            BooksList.ItemsSource = await App.Database.GetBooksByBooklistIdAsync(id);
        }
    }

    #region Click Handlers

    private async void ConfigButtonPressed(object sender, EventArgs e)
    {
		int id = await Task.Run(() => Convert.ToInt32((sender as ImageButton).BindingContext));
        await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}?id={id}|04923102-afb|{BooklistId}");
	}

    private async void addToolbarItem_Clicked(object sender, EventArgs e)
    {
		await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}?id=-1|04923102-afb|{BooklistId}");
	}

    private async void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
		Book book = e.CurrentSelection.FirstOrDefault() as Book;
		await Shell.Current.GoToAsync($"{nameof(ReaderPage)}?id={book.Id}");
    }

	#endregion
}