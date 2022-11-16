using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;

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

        private List<Book>? _bookList;
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

        private bool isReloading = false;
        public bool IsReloading
        {
            get => isReloading;
            set
            {
                if (isReloading != value)
                {
                    SetProperty(ref isReloading, value, nameof(IsReloading));
                }
            }
        }

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

        [RelayCommand]
        public async void Reload()
        {
            IsReloading = true;
            BookList = await App.Database.GetBooksByBooklistIdAsync(id);
            IsReloading = false;
        }
    }
}
