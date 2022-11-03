using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;

namespace Spindler.ViewModels
{
    partial class BookViewModel : ObservableObject
    {
        [ObservableProperty]
        public string title;
        [ObservableProperty]
        public int id;
        
        public BookViewModel(int id, string title)
        {
            this.id = id;
            this.title = title;
        }

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

        [RelayCommand]
        private async void ConfigButton(int bookid)
        {
            await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}?id={bookid}|04923102-afb|{id}");
        }

        [RelayCommand]
        private async void AddToolBarItem()
        {
            await Shell.Current.GoToAsync($"{nameof(BookDetailPage)}?id=-1|04923102-afb|{id}");
        }


        bool loading = false;
        [RelayCommand]
        private async void Selection()
        {
            if (loading || currentSelection == null)
                return;
            await Shell.Current.GoToAsync($"{nameof(ReaderPage)}?id={currentSelection.Id}");
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
