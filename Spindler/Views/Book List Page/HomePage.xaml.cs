namespace Spindler;
using Microsoft.Maui.Controls;
using Spindler.Services;
using Spindler.ViewModels;

public partial class HomePage : ContentPage
{

    public HomePage(DataService database)
    {
        InitializeComponent();
        BindingContext = new HomeViewModel(database);
    }

    protected async override void OnAppearing()
    {
        RequestPermissionIfNotGranted();
        await Task.Run(((HomeViewModel)BindingContext).Load);
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

        status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.StorageWrite>();
        }
    }
}