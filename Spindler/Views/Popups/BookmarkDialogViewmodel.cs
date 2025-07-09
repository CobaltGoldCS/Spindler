using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Views;

public partial class BookmarkDialogViewmodel : ObservableObject, IQueryAttributable
{
    private readonly IDataService Database;
    private readonly IPopupService PopupService;
    private Func<Bookmark> GetNewBookmark;

    [ObservableProperty]
    private ObservableCollection<Bookmark> bookmarks;

    [ObservableProperty]
    private Bookmark? selectedBookmark;
    
    private Book? relatedBook;

    public Book RelatedBook
    {
        get => relatedBook!;
        set
        {
            relatedBook = value;
            OnPropertyChanged();
        }
    }

    public BookmarkDialogViewmodel(IDataService dataService, IPopupService popupService)
    {
        Database = dataService;
        PopupService = popupService;
    }

    [RelayCommand]
    private async Task Add()
    {
        Bookmark bookmark = GetNewBookmark.Invoke();
        Bookmarks.Add(bookmark);
        RelatedBook.Bookmarks = [.. Bookmarks];
        await RelatedBook.SaveInfo(Database);
    }

    [RelayCommand]
    private async Task ItemClicked()
    {
        await PopupService.ClosePopupAsync(Shell.Current, SelectedBookmark);
    }

    [RelayCommand]
    private async void Delete(Bookmark bookmark)
    {
        Bookmarks.Remove(bookmark);

        RelatedBook.Bookmarks = Bookmarks;
        await RelatedBook.SaveInfo(Database);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        RelatedBook = (Book)query["book"];
        Bookmarks = [.. RelatedBook.Bookmarks];
        GetNewBookmark = (Func<Bookmark>)query["newBookmarkFunction"];
    }
}
