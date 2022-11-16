using Spindler.ViewModels;
namespace Spindler;

public partial class ConfigPage : ContentPage
{
    public ConfigPage()
    {
        InitializeComponent();
        var viewModel = new ConfigViewModel();
        BindingContext = viewModel;
    }
}