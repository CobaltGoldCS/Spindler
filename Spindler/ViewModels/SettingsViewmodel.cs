using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Spindler.ViewModels
{
    public partial class SettingsViewmodel : ObservableObject
    {
        [ObservableProperty]
        private string font = Preferences.Default.Get("font", "OpenSansRegular");
        [ObservableProperty]
        private int fontSize = Preferences.Default.Get("font_size", 12);

        public ICommand SaveSettingsCommand { get; private set; }

        public SettingsViewmodel()
        {
            SaveSettingsCommand = new Command(() =>
            {
                Preferences.Set("font", font);
                Preferences.Set("font_size", fontSize);
            });
        }
    }
}
