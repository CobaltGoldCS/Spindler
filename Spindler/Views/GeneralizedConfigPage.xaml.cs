using Spindler.Models;
namespace Spindler.Views;

public partial class GeneralizedConfigPage : ContentPage
{
	public GeneralizedConfigPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        list.ItemsSource = await App.Database.GetAllItemsAsync<GeneralizedConfig>();
        list.Unfocus();
    }

    #region Click Handlers
    private async void addToolbarItem_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"/{nameof(GeneralizedConfigDetailPage)}?id=-1");
    }

    private async void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        GeneralizedConfig config = e.CurrentSelection.FirstOrDefault() as GeneralizedConfig;
        await Shell.Current.GoToAsync($"/{nameof(GeneralizedConfigDetailPage)}?id={config.Id}");
    }
    #endregion
}