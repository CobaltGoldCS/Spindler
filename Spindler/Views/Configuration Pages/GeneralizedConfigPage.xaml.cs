using Spindler.Models;
using Spindler.ViewModels;

namespace Spindler.Views;

public partial class GeneralizedConfigPage : ContentPage
{
	public GeneralizedConfigPage()
	{
		InitializeComponent();
        BindingContext = new GeneralizedConfigViewModel();
    }
}