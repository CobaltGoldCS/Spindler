namespace Spindler;
using Microsoft.Maui.Controls;

using Spindler.Models;
using Spindler.ViewModels;

public partial class HomePage : ContentPage
{

    public HomePage()
    {
        InitializeComponent();
        BindingContext = new HomeViewModel();
    }

    protected async override void OnAppearing()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.NetworkState>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.NetworkState>();
        }
        await Task.Run(((HomeViewModel)BindingContext).Load);
    }
}