using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Spindler.ViewModels
{
    public partial class SettingsViewmodel : ObservableObject
    {
        private string font = Preferences.Default.Get("font", "OpenSansRegular");

        public string Font
        {
            get => font;
            set
            {
                SetProperty(ref font, value);
                Preferences.Set("font", value);
            }
        }

        private int fontSize = Preferences.Default.Get("font_size", 12);

        public int FontSize
        {
            get => fontSize;
            set
            {
                SetProperty(ref fontSize, value);
                Preferences.Set("font_size", value);
            }
        }

        private float fontSpacing = Preferences.Default.Get("line_spacing", 1.5f);

        public float FontSpacing
        {
            get => fontSpacing;
            set 
            { 
                SetProperty(ref fontSpacing, value);
                Preferences.Set("line_spacing", value);
            }
        }

        public SettingsViewmodel() { }
    }
}
