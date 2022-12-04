
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;

namespace Spindler.ViewModels
{
    public partial class BookListDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        public ChooseColor chosenColor1;
        [ObservableProperty]
        public ChooseColor chosenColor2;
        [ObservableProperty]
        public static IList<ChooseColor> colorList = new List<ChooseColor>()
        {
            new ChooseColor(Colors.Black),
            new ChooseColor(Colors.White),
            new ChooseColor(Colors.Wheat ),
            new ChooseColor(Colors.Thistle ),
            new ChooseColor(Colors.AliceBlue ),
            new ChooseColor(Colors.Aqua ),
            new ChooseColor(Colors.Aquamarine ),
            new ChooseColor(Colors.Blue ),
            new ChooseColor(Colors.Green ),
            new ChooseColor(Colors.ForestGreen ),
            new ChooseColor(Colors.LawnGreen ),
            new ChooseColor(Colors.LightSeaGreen ),
            new ChooseColor(Colors.Orange ),
            new ChooseColor(Colors.Yellow ),
            new ChooseColor(Colors.Turquoise ),
            new ChooseColor(Colors.Tomato ),
            new ChooseColor(Colors.Red ),
            new ChooseColor(Colors.Purple ),
            new ChooseColor(Colors.Pink ),
         };
        BookList booklist;
        public BookList Booklist
        {
            get => booklist;
            set
            {
                SetProperty(ref booklist, value);
            }
        }

        public Func<Task<bool>> CancellationWarning;
        public BookListDetailViewModel(BookList Booklist, Func<Task<bool>> cancellationWarning)
        {
            this.Booklist = Booklist;
            booklist = Booklist;
            this.CancellationWarning = cancellationWarning;

            chosenColor1 = new ChooseColor(Booklist.Color1);
            chosenColor2 = new ChooseColor(Booklist.Color2);
        }

        private async Task Close()
        {
            await Shell.Current.GoToAsync("..");
        }

        #region Click Handlers

        [RelayCommand]
        private async void Ok()
        {
            if (!string.IsNullOrWhiteSpace(Booklist.Name))
            {
                Booklist.LastAccessed = DateTime.UtcNow;
                if (Booklist.ImageUrl.Length == 0)
                {
                    Booklist.ImageUrl = BookList.GetRandomPlaceholderImageUrl();
                }
                await App.Database.SaveItemAsync(Booklist);
            }
            await Close();
        }

        [RelayCommand]
        private async void Delete()
        {
            if (Booklist.Id > 0 && await CancellationWarning())
            {
                await App.Database.DeleteBookListAsync(Booklist);
            }
            await Close();
        }

        [RelayCommand]
        private async void Cancel()
        {
            await Close();
        }

        [RelayCommand]
        public void Color1Picked()
        {
            Booklist.Color1 = ChosenColor1.color;
        }

        [RelayCommand]
        public void Color2Picked()
        {
            Booklist.Color2 = ChosenColor2.color;
        }

        #endregion
    }
}
