using Spindler.Models;
using Spindler.ViewModels;
namespace Spindler;

public partial class ConfigPage : ContentPage
{
    public ConfigPage()
    {
        InitializeComponent();
        BindingContext = new ConfigViewModel();
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        list.Unfocus();
        list.ItemsSource = await App.Database.GetAllItemsAsync<Config>();
    }

    #region Click Handlers

    private async void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Config config = (Config)e.CurrentSelection.FirstOrDefault()!;
        await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}?id={config.Id}");
    }
    #endregion
}