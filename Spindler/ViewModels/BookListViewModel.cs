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
    public partial class BookListViewModel(IDataService database, HttpClient client) : SpindlerViewModel(database)
    {
        private readonly HttpClient Client = client;

        #region Bindings
        [ObservableProperty]
        public string title = "Book List";
        [ObservableProperty]
        public int id;

        [ObservableProperty]
        List<Book> currentList = [];

        [ObservableProperty]
        ObservableCollection<Book> displayedBooks = [];

        [ObservableProperty]
        ObservableCollection<Book> pinnedBooks = [];

        [ObservableProperty]
        bool isLoading = true;

        [ObservableProperty]
        bool isExpanding = false;

        [ObservableProperty]
        bool expanded = false;

        [ObservableProperty]
        bool pinnedBooksAreVisible = false;

        [ObservableProperty]
        int loaderHeightRequest = 0;

        string filterText = string.Empty;
        public string FilterText
        {
            get => filterText;
            set
            {
                filterText = value;
                var lowercase = value.ToLower();

                DisplayedBooks.PopulateAndNotify(CurrentList
                                        .Where(book => book.Name.Contains(lowercase, StringComparison.CurrentCultureIgnoreCase))
                                        .Take(NUM_ITEMS_ADDED_TO_LIST),
                                        shouldClear: true);
                SetProperty(ref filterText, value);
            }
        }

        #endregion

        public void SetBookListAndProperties(BookList list)
        {
            Id = list.Id;
            Title = list.Name;
        }

        /// <summary>
        /// Method called when one of the config buttons in the book list is selected
        /// </summary>
        /// <param name="book">The book that the config button is attached to</param>
        [RelayCommand]
        private async Task ConfigButton(Book book)
        {
            Dictionary<string, object> parameters = new()
            {
                { "book", book }
            };
            await NavigateTo(nameof(BookDetailPage), parameters);
        }

        /// <summary>
        /// Method called when the plus icon in the tool bar is selected
        /// </summary>
        [RelayCommand]
        private async Task AddToolBarItem()
        {
            Dictionary<string, object> parameters = new()
            {
                {
                    "book", new Book()
                    {
                        BookListId = Id,
                        LastViewed = DateTime.UtcNow,
                    }
                }
            };
            await NavigateTo(nameof(BookDetailPage), parameters);
        }

        object locker = new();
        bool executing = false;

        bool Executing
        {
            get
            {
                lock (locker) { return executing; }
            }

            set
            {
                lock (locker) { executing = value; }
            }
        }
        /// <summary>
        /// Method Called when a standard book is selected from the booklist
        /// </summary>
        /// <param name="selection">The book that is selected</param>
        [RelayCommand]
        private async Task Selection(Book selection)
        {
            if (Executing)
                return;
            Executing = true;
            var parameters = new Dictionary<string, object>()
            {
                { "book", selection}
            };
            await NavigateTo(nameof(BookPage), parameters);

            Executing = false;
        }

        /// <summary>
        /// Method Called when a book is double tapped
        /// </summary>
        /// <param name="selection">The book that is tapped</param>
        [RelayCommand]
        private async Task DoubleTapped(Book selection)
        {
            if (Executing)
                return;
            Executing = true;
            var config = await Config.FindValidConfig(Database, Client, selection.Url);

            var parameters = new Dictionary<string, object>()
            {
                { "book", selection},
                { "type", config?.UsesHeadless switch
                    {
                        true => ReaderPage.ReaderType.Headless,
                        false => ReaderPage.ReaderType.Standard,
                        null => ReaderPage.ReaderType.Standard,
                    }
                }

            };

            string pageName = nameof(ReaderPage);
            if (config?.UsesWebview ?? false)
            {
                pageName = nameof(WebviewReaderPage);
            }

            await NavigateTo(pageName, parameters);

            Executing = false;
        }

        /// <summary>
        /// Populate the BookPage with relevant Data
        /// </summary>
        /// <returns>Nothing</returns>
        [RelayCommand]
        public async Task Load()
        {
            IsLoading = true;
            CurrentList = await Database.GetBooksByBooklistIdAsync(Id);

            PinnedBooks.PopulateAndNotify(CurrentList.Where(book => book.Pinned), shouldClear: true);
            DisplayedBooks.PopulateAndNotify(CurrentList.Take(NUM_ITEMS_ADDED_TO_LIST), shouldClear: true);

            PinnedBooksAreVisible = PinnedBooks.Count > 0;
            IsLoading = false;
        }

        const int NUM_ITEMS_ADDED_TO_LIST = 7;
        /// <summary>
        /// Method called when the user reaches the end of the displayed books
        /// </summary>
        [RelayCommand]
        public async Task EndOfListReached()
        {
            bool firstTimeLoaded = DisplayedBooks.Count == 0;
            if (firstTimeLoaded && !PinnedBooksAreVisible)
            {
                await Load();
                return;
            }

            LoaderHeightRequest = 20;
            IsExpanding = true;
            DisplayedBooks.PopulateAndNotify(CurrentList!
                                    .Where(book => book.Name.Contains(FilterText))
                                    .Skip(DisplayedBooks.Count)
                                    .Take(NUM_ITEMS_ADDED_TO_LIST),
                                    shouldClear: false);

            IsExpanding = false;
            LoaderHeightRequest = 0;
        }
    }
}
