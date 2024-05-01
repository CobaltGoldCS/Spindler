using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Views;
using Spindler.Views.Book_Pages;
using System.Collections.ObjectModel;

namespace Spindler.ViewModels
{
    public partial class BookListViewModel : SpindlerViewModel
    {
        private readonly HttpClient Client;

        #region Bindings
        [ObservableProperty]
        public string title = "Book List";
        [ObservableProperty]
        public int id;

        [ObservableProperty]
        List<Book> currentList = new();

        [ObservableProperty]
        ObservableCollection<Book> displayedBooks = new();

        [ObservableProperty]
        ObservableCollection<Book> pinnedBooks = new();

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
                DisplayedBooks.Clear();
                var lowercase = value.ToLower();
                foreach (var book in CurrentList
                                        .Where(book => book.Name.ToLower()
                                        .Contains(lowercase))
                                        .Take(NUM_ITEMS_ADDED_TO_LIST))
                {
                    DisplayedBooks.Add(book);
                }
                SetProperty(ref filterText, value);
            }
        }

        #endregion

        public BookListViewModel(IDataService database, HttpClient client) : base(database)
        {
            Client = client;
        }

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
            await NavigateTo($"{nameof(BookDetailPage)}", parameters);
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
            await NavigateTo($"{nameof(BookDetailPage)}", parameters);
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
                { "type", config?.UsesHeadless ?? false ? ReaderPage.ReaderType.Headless : ReaderPage.ReaderType.Standard }
            };

            string pageName = nameof(ReaderPage);
            if (config?.UsesWebview ?? false)
            {
                pageName = nameof(WebviewReaderPage);
            }

            await NavigateTo($"{pageName}", parameters);

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
            CurrentList = new(await Database.GetBooksByBooklistIdAsync(Id));
            DisplayedBooks.Clear();
            PinnedBooks.Clear();
            foreach (var book in CurrentList.Take(NUM_ITEMS_ADDED_TO_LIST))
            {
                DisplayedBooks.Add(book);
            }

            foreach (var book in CurrentList.Where(book => book.Pinned))
            {
                PinnedBooks.Add(book);
            }
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
                // Set up pinned books
                CurrentList = await Database.GetBooksByBooklistIdAsync(Id);
                PinnedBooks = new(CurrentList.Where(b => b.Pinned));
                PinnedBooksAreVisible = PinnedBooks.Count > 0;

                LoaderHeightRequest = 0;
                IsLoading = false;
            }

            LoaderHeightRequest = 20;
            IsExpanding = true;
            foreach (Book book in CurrentList!
                                    .Where(book => book.Name.Contains(FilterText))
                                    .Skip(DisplayedBooks.Count)
                                    .Take(NUM_ITEMS_ADDED_TO_LIST))
            {
                DisplayedBooks!.Add(book);
            }
            IsExpanding = false;
            LoaderHeightRequest = 0;
        }
    }
}
