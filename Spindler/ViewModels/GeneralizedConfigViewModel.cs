using Spindler.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Spindler.ViewModels
{
    public class GeneralizedConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public ICommand AddCommand { get; private set; }

        public GeneralizedConfigViewModel()
        {
            AddCommand = new Command(async () =>
            {
                await Shell.Current.GoToAsync($"/{nameof(GeneralizedConfigDetailPage)}?id=-1");
            });
        }
    }
}
