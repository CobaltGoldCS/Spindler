using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Views;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Collections;

namespace Spindler.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    BookList? currentSelection;

    DataService Database;

    public HomeViewModel(DataService database)
    {
        Database = database;
    }

    private ObservableCollection<BookList>? _bookLists;
    // I don't know what's happening here, but for some reason I have to do this in order to refresh the UI
    public ObservableCollection<BookList>? BookLists
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
        if (CurrentSelection is null) return;
        await CurrentSelection!.UpdateAccessTimeToNow();
        Dictionary<string, object> parameters = new()
        {
            { "booklist", CurrentSelection! }
        };
        BookLists!.RemoveAt(BookLists!.IndexOf(CurrentSelection!));
        BookLists.Insert(0, CurrentSelection!);
        OnPropertyChanged(nameof(BookLists));
        await Shell.Current.GoToAsync($"{nameof(BookListPage)}", parameters);

        CurrentSelection = null;
    }

    public async void Load()
    {
        await Task.Run(async () =>
        {
            var updatedBooklist = await Database.GetBookListsAsync();
            if (BookLists is null)
            {
                BookLists = new(updatedBooklist);
            }
            var set = new HashSet<BookList>(BookLists.ToList());
            if (!set.Equals(updatedBooklist))
            {
                BookLists = new(updatedBooklist);
            }
        });
    }

}
