using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Book_Pages;
using System.Collections.ObjectModel;

namespace Spindler.ViewModels
{
    public partial class BookListViewModel : ObservableObject
    {
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
        bool isLoading = false;

        [ObservableProperty]
        bool expanded = false;

        [ObservableProperty]
        bool pinnedBooksAreVisible = false;

        [ObservableProperty]
        int loaderHeightRequest = 0;

        #endregion

        public BookListViewModel() 
        { 
        }

        public void SetBookList(BookList list)
        {
            Id = list.Id;
            Title = list.Name;
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
                        BookListId = Id,
                        LastViewed = DateTime.UtcNow,
                    }
                }
            };
            await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}", parameters);
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
        private async void Selection(Book selection)
        {
            if (Executing)
                return;
            Executing = true;
            var parameters = new Dictionary<string, object>()
            {
                { "book", selection}
            };
            await Shell.Current.GoToAsync($"{nameof(BookPage)}", parameters);

            Executing = false;
        }

        /// <summary>
        /// Method Called when a book is double tapped
        /// </summary>
        /// <param name="selection">The book that is tapped</param>
        [RelayCommand]
        private async void DoubleTapped(Book selection)
        {
            if (Executing)
                return;
            Executing = true;
            var parameters = new Dictionary<string, object>()
            {
                { "book", selection}
            };

            var config = await Config.FindValidConfig(selection.Url);

            string pageName = nameof(StandardReaderPage);
            if ((bool?)config?.ExtraConfigs.GetValueOrDefault("webview", false) ?? false)
            {
                pageName = nameof(WebviewReaderPage);
            }
            if ((bool?)config?.ExtraConfigs.GetValueOrDefault("headless", false) ?? false)
            {
                pageName = nameof(HeadlessReaderPage);
            }

            await Shell.Current.GoToAsync($"{pageName}", parameters);

            Executing = false;
        }

        /// <summary>
        /// Populate the BookPage with relevant Data
        /// </summary>
        /// <returns>Nothing</returns>
        public async Task Load()
        {
            await Task.Run(async () =>
            {
                CurrentList = new(await App.Database.GetBooksByBooklistIdAsync(Id));

                
                if (DisplayedBooks.Count > 0)
                {
                    DisplayedBooks.ExecuteAddAndDeleteTransactions(CurrentList);
                }
                else
                {
                    foreach(var book in CurrentList.Take(NUM_ITEMS_ADDED_TO_LIST))
                    {
                        DisplayedBooks.Add(book);
                    }
                }

                if (PinnedBooks.Count > 0)
                {
                    PinnedBooks.ExecuteAddAndDeleteTransactions(CurrentList);
                }
                else
                {
                    foreach (var book in CurrentList.Take(NUM_ITEMS_ADDED_TO_LIST))
                    {
                        PinnedBooks.Add(book);
                    }
                }
                PinnedBooksAreVisible = PinnedBooks.Count > 0;
            });
        }

        const int NUM_ITEMS_ADDED_TO_LIST = 20;
        /// <summary>
        /// Method called when the user reaches the end of the displayed books
        /// </summary>
        [RelayCommand]
        public void EndOfListReached()
        {
            LoaderHeightRequest = 20;
            IsLoading = true;
            foreach (Book book in CurrentList!.Skip(DisplayedBooks.Count).Take(NUM_ITEMS_ADDED_TO_LIST))
            {
                DisplayedBooks!.Add(book);
            }
            IsLoading = false;
            LoaderHeightRequest = 0;
        }
    }
}
