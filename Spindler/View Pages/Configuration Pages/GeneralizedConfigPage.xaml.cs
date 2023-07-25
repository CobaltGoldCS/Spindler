using Spindler.ViewModels;

namespace Spindler.Views;

public partial class GeneralizedConfigPage : ContentPage
{
    public GeneralizedConfigPage()
    {
        InitializeComponent();
        var viewModel = new GeneralizedConfigViewModel();
        BindingContext = viewModel;
        viewModel.ReloadItems();
    }
}