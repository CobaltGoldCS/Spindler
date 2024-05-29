using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using System.Collections.ObjectModel;

namespace Spindler.ViewModels;

public partial class HomeViewModel : SpindlerViewModel
{
    #region Class Attributes
    [ObservableProperty]
    BookList? currentSelection;

    [ObservableProperty]
    public ObservableCollection<BookList> displayedBooklists = [];

    [ObservableProperty]
    public bool isLoading = true;

    public List<BookList>? BookLists;

    #endregion

    public HomeViewModel(IDataService database) : base(database)
    {
        Database = database;
    }

    [RelayCommand]
    private async Task ConfigButton(int id)
    {
        // Add A Dialog to change the values of BookList
        BookList bookList = await Database.GetItemByIdAsync<BookList>(id);
        Dictionary<string, object> parameters = new()
        {
            {
                "booklist", bookList
            }
        };
        await NavigateTo($"{nameof(BookListDetailPage)}", parameters);
    }

    [RelayCommand]
    private async Task AddToolBarItem()
    {
        Dictionary<string, object> parameters = new()
        {
            {
                "booklist", new BookList()
            }
        };
        await NavigateTo($"{nameof(BookListDetailPage)}", parameters);
    }

    [RelayCommand]
    private async Task Selection()
    {
        if (CurrentSelection is null)
            return;

        await CurrentSelection!.UpdateAccessTimeToNow(Database);
        Dictionary<string, object> parameters = new()
        {
            { "booklist", CurrentSelection! }
        };

        BookLists!.Remove(CurrentSelection!);
        BookLists.Insert(0, CurrentSelection!);

        OnPropertyChanged(nameof(BookLists));
        await NavigateTo($"{nameof(BookListPage)}", parameters, false);

        CurrentSelection = null;
    }

    [RelayCommand]
    public async Task Load()
    {
        IsLoading = true;
        BookLists = await Database.GetBookListsAsync();
        DisplayedBooklists.PopulateAndNotify(BookLists, shouldClear: true);
        IsLoading = false;
    }

}
