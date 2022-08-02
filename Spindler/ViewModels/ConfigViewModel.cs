using Spindler.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Spindler.ViewModels
{
    public class ConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private List<Config> _configs;
        public List<Config> configs
        {
            get => _configs;
            set
            {
                if (_configs == value) return;
                _configs = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddConfigCommand;

        public ConfigViewModel()
        {
            var configtask = InitListAsync;

            AddConfigCommand = new Command(async () =>
            {
                await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}?id=-1");
            });
        }

        private async Task InitListAsync()
        {
            configs = new();
            configs = await App.Database.GetAllItemsAsync<Config>();
        }
    }
}
