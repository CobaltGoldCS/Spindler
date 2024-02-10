using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;

namespace Spindler.ViewModels;
public partial class ConfigViewModel : ObservableObject
{
    private readonly IDataService Database;

    [ObservableProperty]
    public Config? selectedItem;

    public bool IsGeneralized = false;
    private List<Config> _configItems = [];
    // I don't know what's happening here, but for some reason I have to do this in order to refresh the UI
    public List<Config> ConfigItems
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

    public ConfigViewModel(IDataService service)
    {
        Database = service;
    }

    [RelayCommand]
    public async Task ReloadItems() 
    {
        IsRefreshing = true;
        if (IsGeneralized)
        {
            List<Config> newConfigs = [];
            foreach (GeneralizedConfig gConfig in await Database.GetAllItemsAsync<GeneralizedConfig>())
            {
                newConfigs.Add(gConfig);
            }

            ConfigItems = newConfigs;
        } else
        {
            ConfigItems = await Database.GetAllItemsAsync<Config>();
        }
        
        IsRefreshing = false;
    }

    [RelayCommand]
    public async Task ItemClicked()
    {
        Dictionary<string, object> parameters = new()
        {
            { "config", SelectedItem! }
        };
        await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}", parameters);
        SelectedItem = null;
    }

    [RelayCommand]
    public async Task Add()
    {
        Config newItem = IsGeneralized ? new GeneralizedConfig() { Id = -1 } : new Config() { Id = -1 };
        Dictionary<string, object> parameters = new()
        {
            {
                "config",
                newItem
            }
        };
        await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}", parameters);
    }
}
