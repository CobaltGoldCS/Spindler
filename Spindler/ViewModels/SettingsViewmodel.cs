using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Spindler.ViewModels
{
    public partial class SettingsViewmodel : ObservableObject
    {
        [ObservableProperty]
        private string font = Preferences.Default.Get("font", "OpenSansRegular");
        [ObservableProperty]
        private int fontSize = Preferences.Default.Get("font_size", 12);

        [ObservableProperty]
        private float fontSpacing = Preferences.Default.Get("line_spacing", 1.5f);

        public SettingsViewmodel() { }
        [RelayCommand]
        public void SaveSettings()
        {
            Preferences.Set("font", font);
            Preferences.Set("font_size", fontSize);
            Preferences.Set("line_spacing", fontSpacing);
        }
    }
}
