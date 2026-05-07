using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using System.Collections.ObjectModel;

namespace Spindler.ViewModels;
public partial class ConfigViewModel(IDataService service) : SpindlerViewModel(service)
{
    // IsGeneralized is set by the code-behind of the Config Page, and is not set in this class
    public bool IsGeneralized = false;

    [ObservableProperty]
    public Config? selectedItem;

    [ObservableProperty]
    public ObservableCollection<Config> configItems = [];

    [ObservableProperty]
    public bool isRefreshing = false;

    [RelayCommand]
    public async Task ReloadItems()
    {
        IsRefreshing = true;
        if (IsGeneralized)
        {
            ConfigItems.PopulateAndNotify(await Database.GetAllItemsAsync<GeneralizedConfig>(), shouldClear: true);
        }
        else
        {
            ConfigItems.PopulateAndNotify(await Database.GetAllItemsAsync<Config>(), shouldClear: true);
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
        await NavigateTo($"/{nameof(ConfigDetailPage)}", parameters);
        SelectedItem = null;
    }

    [RelayCommand]
    public async Task Add()
    {
        Config newItem = IsGeneralized switch
        {
            true => new GeneralizedConfig() { Id = -1 },
            false => new Config() { Id = -1 },
        };

        Dictionary<string, object> parameters = new()
        {
            {
                "config",
                newItem
            }
        };
        await NavigateTo($"/{nameof(ConfigDetailPage)}", parameters);
    }
}
