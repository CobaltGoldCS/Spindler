
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
        /// <summary>
        /// A list of valid colors to pick from in the color picker 
        /// </summary>
        [ObservableProperty]
        public static IList<ChooseColor> colorList = new List<ChooseColor>()
        {
            new (Colors.Black),
            new (Colors.White),
            new (Colors.Wheat ),
            new (Colors.Thistle ),
            new (Colors.AliceBlue ),
            new (Colors.Aqua ),
            new (Colors.Aquamarine ),
            new (Colors.Blue ),
            new (Colors.Green ),
            new (Colors.ForestGreen ),
            new (Colors.LawnGreen ),
            new (Colors.LightSeaGreen ),
            new (Colors.Orange ),
            new (Colors.Yellow ),
            new (Colors.Turquoise ),
            new (Colors.Tomato ),
            new (Colors.Red ),
            new (Colors.Purple ),
            new (Colors.Pink ),
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

        public BookListDetailViewModel(BookList Booklist)
        {
            this.Booklist = Booklist;
            booklist = Booklist;

            chosenColor1 = new(Booklist.Color1);
            chosenColor2 = new(Booklist.Color2);
        }

        private static async Task Close()
        {
            await Shell.Current.GoToAsync("..");
        }

        #region Click Handlers

        /// <summary>
        /// A handler for when the Ok button is clicked
        /// </summary>
        [RelayCommand]
        private async Task Ok()
        {
            if (!string.IsNullOrWhiteSpace(Booklist.Name))
            {
                await Booklist.UpdateAccessTimeToNow();
            }
            await Close();
        }

        /// <summary>
        /// A handler for when the Delete button is clicked
        /// </summary>
        [RelayCommand]
        private async Task Delete()
        {
            if (Booklist.Id > 0 && await Application.Current!.MainPage!.DisplayAlert("Warning!", "Are you sure you want to delete this booklist?", "Yes", "No"))
            {
                await App.Database.DeleteBookListAsync(Booklist);
            }
            await Close();
        }

        /// <summary>
        /// A handler for when the Cancel button is clicked
        /// </summary>
        [RelayCommand]
        private async Task Cancel()
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
