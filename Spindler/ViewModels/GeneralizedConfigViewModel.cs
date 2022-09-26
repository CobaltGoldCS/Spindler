using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace Spindler.ViewModels
{
    public class GeneralizedConfigViewModel : ObservableObject
    {
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
