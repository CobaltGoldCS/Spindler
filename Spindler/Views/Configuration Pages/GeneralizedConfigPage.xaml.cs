using Spindler.Models;
using Spindler.ViewModels;

namespace Spindler.Views;

public partial class GeneralizedConfigPage : ContentPage
{
	public GeneralizedConfigPage()
	{
		InitializeComponent();
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        list.ItemsSource = await App.Database.GetAllItemsAsync<GeneralizedConfig>();
        list.Unfocus();
    }

    #region Click Handlers

    private async void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        GeneralizedConfig config = (GeneralizedConfig)e.CurrentSelection.FirstOrDefault()!;
        list.Unfocus();
        await Shell.Current.GoToAsync($"/{nameof(GeneralizedConfigDetailPage)}?id={config.Id}");
    }
    #endregion

    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"/{nameof(GeneralizedConfigDetailPage)}?id=-1");
    }
}