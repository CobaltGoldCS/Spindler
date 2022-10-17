using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace Spindler.ViewModels;
public partial class ConfigViewModel : ObservableObject
{

    public ICommand AddCommand { get; private set; }

    public ConfigViewModel()
    {

        AddCommand = new Command(async () =>
        {
            await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}?id=-1");
        });
    }
}
