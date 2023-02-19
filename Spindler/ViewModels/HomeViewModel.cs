using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Views;

namespace Spindler.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    BookList? currentSelection;

    public HomeViewModel()
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
                "booklist", new BookList()
            }
        };
        await Shell.Current.GoToAsync($"{nameof(BookListDetailPage)}", parameters);
    }

    [RelayCommand]
    private async void Selection()
    {
        if (CurrentSelection == null) return;
        var selectedItem = currentSelection;
        await selectedItem!.UpdateAccessTimeToNow();
        Dictionary<string, object> parameters = new()
        {
            { "booklist", selectedItem! }
        };
        await Shell.Current.GoToAsync($"{nameof(BookListPage)}", parameters);
        CurrentSelection = null;
    }

    public async void Load()
    {
        BookLists = await App.Database.GetBookListsAsync();
    }

}
