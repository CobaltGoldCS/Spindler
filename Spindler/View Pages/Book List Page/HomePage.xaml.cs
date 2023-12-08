namespace Spindler;

using Microsoft.Maui.Controls;
using Spindler.Services;
using Spindler.ViewModels;

public partial class HomePage : ContentPage
{

    public HomePage(HomeViewModel viewModel, IDataService dataService)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _ = viewModel.Load();
        RequestPermissionIfNotGranted();
    }


    private static async void RequestPermissionIfNotGranted()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.NetworkState>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.NetworkState>();
        }

        status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.StorageRead>();
        }

        status = await Permissions.CheckStatusAsync<Permissions.Media>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.Media>();
        }

        status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.StorageWrite>();
        }
    }
}