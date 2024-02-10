using Spindler.Services;
using Spindler.ViewModels;

namespace Spindler;

public partial class ConfigPage : ContentPage
{
    ConfigViewModel ViewModel;
    public ConfigPage(IDataService service)
    {
        InitializeComponent();
        ViewModel = new(service);
        Shell.Current.Navigated += Navigated;
    }

    private void Navigated(object? sender, ShellNavigatedEventArgs e)
    {
        ViewModel.IsGeneralized = e.Current.Location.OriginalString == "//GeneralConfig";
     
        BindingContext = ViewModel;
        _ = ViewModel.ReloadItems();
        Shell.Current.Navigated -= Navigated;
    }
}