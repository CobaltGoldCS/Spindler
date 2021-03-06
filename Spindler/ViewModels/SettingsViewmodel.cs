using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Spindler.ViewModels
{
    public class SettingsViewmodel : INotifyPropertyChanged
    {
        private string font = Preferences.Default.Get("font", "OpenSansRegular");
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

        public string Font
        {
            get => font;
            set
            {
                if (font == value)
                    return;
                font = value;
                OnPropertyChanged();
            }
        }

        public int FontSize
        {
            get => fontSize;
            set
            {
                if (fontSize == value)
                    return;
                fontSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RequestSize));
            }
        }

        public int RequestSize
        {
            get => FontSize + 5;
            set
            {
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
