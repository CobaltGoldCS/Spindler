using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Spindler.ViewModels;
public partial class ConfigViewModel : ObservableObject
{
    [ObservableProperty]
    public Config? selectedItem;

    private List<Config>? _configItems;
    // I don't know what's happening here, but for some reason I have to do this in order to refresh the UI
    public List<Config>? ConfigItems
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
        ConfigItems = await App.Database.GetAllItemsAsync<Config>();
        IsRefreshing = false;
    }

    [RelayCommand]
    public async Task ItemClicked()
    {
        await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}?id={selectedItem!.Id}");
    }

    [RelayCommand]
    public async Task Add()
    {
        await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}?id=-1");
    }
}
