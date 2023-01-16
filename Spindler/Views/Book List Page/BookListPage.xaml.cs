namespace Spindler;
using Microsoft.Maui.Controls;

using Spindler.Models;
using Spindler.ViewModels;

public partial class BookListPage : ContentPage
{

    public BookListPage()
    {
        InitializeComponent();
        BindingContext = new BookListViewModel();
    }

    protected async override void OnAppearing()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.NetworkState>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.NetworkState>();
        }
        await Task.Run(((BookListViewModel)BindingContext).Load);
    }
}