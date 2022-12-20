using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using System.Collections.ObjectModel;

namespace Spindler.ViewModels
{
    partial class BookViewModel : ObservableObject
    {
        #region Bindings
        [ObservableProperty]
        public string title;
        [ObservableProperty]
        public int id;

        [ObservableProperty]
        Book? currentSelection;

        [ObservableProperty]
        Book? currentPinnedBookSelection;

        [ObservableProperty]
        List<Book>? bookList = null;

        [ObservableProperty]
        ObservableCollection<Book> displayedBooks = new();

        [ObservableProperty]
        ObservableCollection<Book> pinnedBooks = new();

        [ObservableProperty]
        bool isLoading = false;

        [ObservableProperty]
        bool expanded = false;

        [ObservableProperty]
        int loaderHeightRequest = 0;

        #endregion

        public BookViewModel(int id, string title)
        {
            this.id = id;
            this.title = title;
        }

        ImageButton? AddButton;
        public void AddUiReferences(ImageButton addToolBarItem)
        {
            AddButton = addToolBarItem;
        }

        [RelayCommand]
        private async void ConfigButton(Book book)
        {
            Dictionary<string, object> parameters = new()
            {
                { "book", book }
            };
            await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}", parameters);
        }

        [RelayCommand]
        private async void AddToolBarItem()
        {
            await AddButton!.RelRotateTo(360, 250, Easing.CubicIn);
            Dictionary<string, object> parameters = new()
            {
                { 
                    "book", new Book()
                    {
                        Id = -1,
                        BookListId = id,
                        LastViewed = DateTime.UtcNow,
                    }
                }
            };
            await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}", parameters);
        }


        bool executing = false;
        [RelayCommand]
        private async void Selection()
        {
            if (executing || currentSelection == null)
                return;
            var parameters = new Dictionary<string, object>()
            {
                { "book", currentSelection}
            };
            await Shell.Current.GoToAsync($"{nameof(ReaderPage)}", parameters);
            executing = false;
            currentSelection = null;
        }

        [RelayCommand]
        private async void PinnedBookSelection()
        {
            if (executing || CurrentPinnedBookSelection == null)
                return;
            var parameters = new Dictionary<string, object>()
            {
                { "book", CurrentPinnedBookSelection}
            };
            await Shell.Current.GoToAsync($"{nameof(ReaderPage)}", parameters);
            executing = false;
            CurrentPinnedBookSelection = null;
        }

        public async Task Load()
        {
            BookList = await App.Database.GetBooksByBooklistIdAsync(id);
            PinnedBooks = new ObservableCollection<Book>(BookList.FindAll((book) => book.Pinned));
        }

        const int NUM_ITEMS_ADDED_TO_LIST = 9;
        [RelayCommand]
        public void EndOfListReached()
        {
            LoaderHeightRequest = 20;
            IsLoading = true;
            foreach (Book book in BookList!.Skip(DisplayedBooks.Count).Take(NUM_ITEMS_ADDED_TO_LIST))
                DisplayedBooks!.Add(book);
            IsLoading = false;
            LoaderHeightRequest = 0;
        }
    }
}
