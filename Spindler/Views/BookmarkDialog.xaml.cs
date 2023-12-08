using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Spindler.Models;
using Spindler.Services;
using System.Collections.ObjectModel;

namespace Spindler.Views;

public class CreateBottomSheetMessage : ValueChangedMessage<Popup>
{
    public CreateBottomSheetMessage(Popup view) : base(view) { }
}

public class BookmarkClickedMessage : ValueChangedMessage<Bookmark>
{
    public BookmarkClickedMessage(Bookmark value) : base(value) { }
}

public partial class BookmarkDialog : Popup
{
    private readonly IDataService Database;
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

    public BookmarkDialog(IDataService service, Book book, Func<Bookmark> getNewBookmark)
    {
        InitializeComponent();
        Database = service;
        Book = book;
        Bookmarks = new(Book.Bookmarks);
        GetNewBookmark = getNewBookmark;
        var width = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        var height = .5 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density);
        Size = new(width, height);
        ResultWhenUserTapsOutsideOfPopup = null;
    }

    [RelayCommand]
    private async Task Add()
    {
        Bookmark bookmark = GetNewBookmark.Invoke();
        Bookmarks.Add(bookmark);
        Book.Bookmarks = Bookmarks.ToList();
        await Book.UpdateViewTimeAndSave(Database);
    }

    [RelayCommand]
    private void ItemClicked()
    {
        Close(SelectedBookmark);
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