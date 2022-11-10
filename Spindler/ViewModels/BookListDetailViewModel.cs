
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using System.Collections.Generic;
using System.Reflection;

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
            new ChooseColor { color = Colors.Black },
            new ChooseColor { color = Colors.White },
            new ChooseColor { color = Colors.Wheat },
            new ChooseColor { color = Colors.Thistle },
            new ChooseColor { color = Colors.AliceBlue },
            new ChooseColor { color = Colors.Aqua },
            new ChooseColor { color = Colors.Aquamarine },
            new ChooseColor { color = Colors.Blue },
            new ChooseColor { color = Colors.Green },
            new ChooseColor { color = Colors.ForestGreen },
            new ChooseColor { color = Colors.LawnGreen },
            new ChooseColor { color = Colors.LightSeaGreen },
            new ChooseColor { color = Colors.Orange },
            new ChooseColor { color = Colors.Yellow },
            new ChooseColor { color = Colors.Turquoise },
            new ChooseColor { color = Colors.Tomato },
            new ChooseColor { color = Colors.Red },
            new ChooseColor { color = Colors.Purple },
            new ChooseColor { color = Colors.Pink },
         };
        // COLORS ARE NOT UPDATING AGAIN
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

            chosenColor1 = new ChooseColor { color = Booklist.Color1 };
            chosenColor2 = new ChooseColor { color = Booklist.Color2 };
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
            if (Booklist.Id > 0)
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
