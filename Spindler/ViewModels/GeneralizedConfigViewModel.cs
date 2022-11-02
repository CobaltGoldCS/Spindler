using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;

namespace Spindler.ViewModels
{
    public partial class GeneralizedConfigViewModel : ObservableObject
    {
        [ObservableProperty]
        public GeneralizedConfig? selectedItem;

        private List<GeneralizedConfig>? _configItems;
        // I don't know what's happening here, but for some reason I have to do this in order to refresh the UI
        public List<GeneralizedConfig>? ConfigItems
        {
            get => _configItems;
            set
            {
                if (_configItems != value)
                {
                    SetProperty(ref _configItems, value, nameof(ConfigItems));
                }
            }
        }

        [ObservableProperty]
        public bool isRefreshing = false;

        [RelayCommand]
        public async Task ReloadItems()
        {
            IsRefreshing = true;
            ConfigItems = await App.Database.GetAllItemsAsync<GeneralizedConfig>();
            IsRefreshing = false;
        }

        [RelayCommand]
        public async Task ItemClicked()
        {
            await Shell.Current.GoToAsync($"/{nameof(GeneralizedConfigDetailPage)}?id={selectedItem!.Id}");
        }

        [RelayCommand]
        public async Task Add()
        {
            await Shell.Current.GoToAsync($"/{nameof(GeneralizedConfigDetailPage)}?id=-1");
        }
    }
}
