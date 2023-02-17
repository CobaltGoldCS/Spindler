using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Book_Pages;
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
        bool pinnedBooksAreVisible = false;

        [ObservableProperty]
        int loaderHeightRequest = 0;

        #endregion

        public BookViewModel(BookList list)
        {
            id = list.Id;
            title = list.Name;
        }

        ImageButton? AddButton;
        /// <summary>
        /// Add references to BookPage Elements 
        /// </summary>
        /// <param name="addToolBarItem">The ImageButton Plus in the toolbar </param>
        public void AddUiReferences(ImageButton addToolBarItem)
        {
            AddButton = addToolBarItem;
        }

        /// <summary>
        /// Method called when one of the config buttons in the book list is selected
        /// </summary>
        /// <param name="book">The book that the config button is attached to</param>
        [RelayCommand]
        private async void ConfigButton(Book book)
        {
            Dictionary<string, object> parameters = new()
            {
                { "book", book }
            };
            await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}", parameters);
        }

        /// <summary>
        /// Method called when the plus icon in the tool bar is selected
        /// </summary>
        [RelayCommand]
        private async void AddToolBarItem()
        {
            await AddButton!.RelRotateTo(360, 250, Easing.CubicIn);
            Dictionary<string, object> parameters = new()
            {
                { 
                    "book", new Book()
                    {
                        BookListId = id,
                        LastViewed = DateTime.UtcNow,
                    }
                }
            };
            await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}", parameters);
        }


        bool executing = false;
        /// <summary>
        /// Method Called when a standard book is selected from the booklist
        /// </summary>
        /// <param name="selection">The book that is selected</param>
        [RelayCommand]
        private async void Selection(Book selection)
        {
            if (executing)
                return;
            executing = true;
            var parameters = new Dictionary<string, object>()
            {
                { "book", selection}
            };
            await Shell.Current.GoToAsync($"{nameof(BookInfoPage)}", parameters);
            executing = false;
        }

        /// <summary>
        /// Method Called when a book is double tapped
        /// </summary>
        /// <param name="selection">The book that is tapped</param>
        [RelayCommand]
        private async void DoubleTapped(Book selection)
        {
            if (executing)
                return;
            executing = true;
            var parameters = new Dictionary<string, object>()
            {
                { "book", selection}
            };

            var config = await WebService.FindValidConfig(selection.Url);
            
            string pageName = nameof(StandardReaderPage);
            if (config?.ExtraConfigs.GetOrDefault("webview", false) ?? false)
            {
                pageName = nameof(WebviewReaderPage);
            }
            if (config?.ExtraConfigs.GetOrDefault("headless", false) ?? false)
            {
                pageName = nameof(HeadlessReaderPage);
            }

            await Shell.Current.GoToAsync($"{pageName}", parameters);
            executing = false;
        }

        /// <summary>
        /// Method called when a Pinned Book is selected
        /// </summary>
        [RelayCommand]
        private async void PinnedBookSelection()
        {
            if (executing || CurrentPinnedBookSelection == null)
                return;
            var parameters = new Dictionary<string, object>()
            {
                { "book", CurrentPinnedBookSelection}
            };
            await Shell.Current.GoToAsync($"{nameof(BookInfoPage)}", parameters);
            executing = false;
        }

        /// <summary>
        /// Populate the BookPage with relevant Data
        /// </summary>
        /// <returns>Nothing</returns>
        public async Task Load()
        {
            BookList = await App.Database.GetBooksByBooklistIdAsync(id);
            PinnedBooks = new ObservableCollection<Book>(BookList.FindAll((book) => book.Pinned));
            PinnedBooksAreVisible = PinnedBooks.Count > 0;
        }

        const int NUM_ITEMS_ADDED_TO_LIST = 100;
        /// <summary>
        /// Method called when the user reaches the end of the displayed books
        /// </summary>
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
