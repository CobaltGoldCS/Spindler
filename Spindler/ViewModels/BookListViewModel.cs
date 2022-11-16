using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;

namespace Spindler.ViewModels;

public partial class BookListViewModel : ObservableObject
{
    [ObservableProperty]
    BookList? currentSelection;

    public BookListViewModel()
    {
    }

    private List<BookList>? _bookLists;
    // I don't know what's happening here, but for some reason I have to do this in order to refresh the UI
    public List<BookList>? BookLists
    {
        get => _bookLists;
        set
        {
            if (_bookLists != value)
            {
                SetProperty(ref _bookLists, value, nameof(BookLists));
            }
        }
    }

    private bool isReloading = false;
    public bool IsReloading
    {
        get => isReloading;
        set
        {
            if (isReloading != value)
            {
                SetProperty(ref isReloading, value, nameof(IsReloading));
            }
        }
    }    

    [RelayCommand]
    private async void ConfigButton(int id)
    {
        // Add A Dialog to change the values of BookList
        BookList bookList = await App.Database.GetItemByIdAsync<BookList>(id);
        Dictionary<string, object> parameters = new()
        {
            {
                "booklist", bookList
            }
        };
        await Shell.Current.GoToAsync($"{nameof(BookListDetailPage)}", parameters);
    }

    [RelayCommand]
    private async void AddToolBarItem()
    {
        Dictionary<string, object> parameters = new()
        {
            {
                "booklist", new BookList() {Id = -1}
            }
        };
        await Shell.Current.GoToAsync($"{nameof(BookListDetailPage)}", parameters);
    }

    [RelayCommand]
    private async void Selection()
    {
        var selectedItem = currentSelection;
        currentSelection = null;
        selectedItem!.LastAccessed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(selectedItem);
        Dictionary<string, object> parameters = new()
        {
            { "booklist", selectedItem! }
        };
        await Shell.Current.GoToAsync($"/{nameof(BookPage)}", parameters);
    }

    [RelayCommand]
    public async void Reload()
    {
        IsReloading = true;
        BookLists = await App.Database.GetBookListsAsync();
        IsReloading = false;
    }

    [RelayCommand]
    public void OnAppearing()
    {
        currentSelection = null;
    }
}
