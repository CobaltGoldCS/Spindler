namespace Spindler;
using Microsoft.Maui.Controls;

using Spindler.Models;

public partial class BookListPage : ContentPage
{

    public BookListPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Attach booklists to the main list
        list.ItemsSource = await App.Database.GetBookListsAsync();
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.NetworkState>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.NetworkState>();
        }
    }

    #region Click Handlers
    private async void ConfigButtonPressed(object sender, EventArgs e)
    {
        int id = Convert.ToInt32(((ImageButton)sender).BindingContext);
        // Add A Dialog to change the values of BookList
        await Shell.Current.GoToAsync($"{nameof(BookListDetailPage)}?id={id}");
    }

    private async void addToolbarItem_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(BookListDetailPage)}?id=-1");
    }

    private async void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BookList list = e.CurrentSelection.FirstOrDefault() as BookList;
        list.LastAccessed = DateTime.UtcNow;
        await App.Database.SaveItemAsync(list);
        await Shell.Current.GoToAsync($"/{nameof(BookPage)}?id={list.Id}");
    }

    #endregion
}