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
        await Shell.Current.GoToAsync($"{nameof(BookListDetailPage)}?id={id}");
    }

    [RelayCommand]
    private async void AddToolBarItem()
    {
        await Shell.Current.GoToAsync($"{nameof(BookListDetailPage)}?id=-1");
    }

    [RelayCommand]
    private async void Selection()
    {
        var selectedItem = currentSelection;
        currentSelection = null;
        selectedItem!.LastAccessed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(selectedItem);
        await Shell.Current.GoToAsync($"/{nameof(BookPage)}?id={selectedItem.Id}");
    }

    [RelayCommand]
    public async void Reload()
    {
        IsReloading = true;
        BookLists = await App.Database.GetBookListsAsync();
        IsReloading = false;
    }
}
