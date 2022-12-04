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

        private List<Book>? _bookList = null;
        // I don't know what's happening here, but for some reason I have to do this in order to refresh the UI
        public List<Book>? BookList
        {
            get => _bookList;
            set
            {
                if (_bookList != value)
                {
                    SetProperty(ref _bookList, value, nameof(BookList));
                }
            }
        }

        [ObservableProperty]
        ObservableCollection<Book> displayedBooks = new();

        [ObservableProperty]
        bool isLoading = false;

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


        bool loading = false;
        [RelayCommand]
        private async void Selection()
        {
            if (loading || currentSelection == null)
                return;
            var parameters = new Dictionary<string, object>()
            {
                { "book", currentSelection}
            };
            await Shell.Current.GoToAsync($"{nameof(ReaderPage)}", parameters);
            loading = false;
            currentSelection = null;
        }

        public async Task Load()
        {
            BookList = await App.Database.GetBooksByBooklistIdAsync(id);
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
