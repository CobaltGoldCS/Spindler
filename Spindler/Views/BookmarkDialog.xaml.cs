using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Spindler.Views;

public partial class BookmarkDialog : Popup
{
	private readonly IDataService Database;
    private readonly Action<Bookmark> ItemClickedHandler;
    private readonly Func<Bookmark> GetNewBookmark;

	public double Width = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - 20;

    private ObservableCollection<Bookmark> bookmarks = new();
	public ObservableCollection<Bookmark> Bookmarks
	{
		get => bookmarks;
		set
		{
			bookmarks = value;
			OnPropertyChanged();
		}
	}

	private Bookmark? selectedBookmark;
	public Bookmark SelectedBookmark
	{
		get => selectedBookmark!;
		set
		{
			selectedBookmark = value;
			OnPropertyChanged();
		}
	}
	

	public static readonly BindableProperty BookProperty = BindableProperty.Create(
		nameof(Book),
		typeof(Book),
		typeof(Popup),
		defaultBindingMode: BindingMode.TwoWay);

	public Book Book
	{
		get => (Book)GetValue(BookProperty);
		set => SetValue(BookProperty, value);
	}

	public BookmarkDialog(IDataService service, Book book, Func<Bookmark> getBookmark, Action<Bookmark> itemClickedHandler)
	{
		InitializeComponent();
		Database = service;
		Book = book;
		Bookmarks = new (Book.Bookmarks);
		ItemClickedHandler = itemClickedHandler;
		GetNewBookmark = getBookmark;
		var width = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        var height = .5 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density);
		Size = new(width, height);
    }

	[RelayCommand]
	private async void Add()
	{
		Bookmark bookmark = GetNewBookmark.Invoke();
		Bookmarks.Add(bookmark);
		Book.Bookmarks = Bookmarks.ToList();
		await Book.UpdateViewTimeAndSave(Database);
	}

	[RelayCommand]
	private void ItemClicked()
	{
		Close();
        ItemClickedHandler?.Invoke(SelectedBookmark);
	}

    private async void DeleteItem_Clicked(object sender, EventArgs e)
    {
		var item = (Button)sender;
		var bookmark = (Bookmark)item.BindingContext;
		Bookmarks.Remove(bookmark);

		Book.Bookmarks = Bookmarks;
		await Book.UpdateViewTimeAndSave(Database);
    }
}