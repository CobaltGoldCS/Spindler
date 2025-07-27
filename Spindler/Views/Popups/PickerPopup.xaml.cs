using CommunityToolkit.Maui.Views;
using Spindler.Models;
using Spindler.Views;


namespace Spindler.CustomControls;

public partial class PickerPopup : Popup<IIndexedModel>
{
    public PickerPopup(PickerPopupViewmodel viewmodel)
    {
        InitializeComponent();
        BindingContext = viewmodel;
        WidthRequest = 0.9 * (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density);
        HeightRequest = 0.8 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density);
    }
}