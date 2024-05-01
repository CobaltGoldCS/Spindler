using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Views;
using System.Collections.ObjectModel;

namespace Spindler.ViewModels;

public partial class HomeViewModel : SpindlerViewModel
{
    #region Class Attributes
    [ObservableProperty]
    BookList? currentSelection;

    [ObservableProperty]
    public ObservableCollection<BookList> displayedBooklists = new();

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
        await Task.Run(async () =>
        {
            await Task.Delay(250);
            IsLoading = true;
            BookLists = await Database.GetBookListsAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DisplayedBooklists.Clear();
                foreach (BookList list in BookLists!)
                {
                    DisplayedBooklists!.Add(list);
                }
            });
            IsLoading = false;
        });
    }

}
