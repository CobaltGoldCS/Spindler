using Spindler.Models;
namespace Spindler;

public partial class ConfigPage : ContentPage
{
    public ConfigPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        list.ItemsSource = await App.Database.GetAllItemsAsync<Config>();
        list.Unfocus();
    }

    #region Click Handlers
    private async void addToolbarItem_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}?id=-1");
    }

    private async void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Config config = e.CurrentSelection.FirstOrDefault() as Config;
        await Shell.Current.GoToAsync($"/{nameof(ConfigDetailPage)}?id={config.Id}");
    }
    #endregion
}