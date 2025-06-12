using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;


namespace Spindler.CustomControls;

public partial class PickerPopup : Popup<IIndexedModel>
{
    private IList<IIndexedModel>? items;
    public IList<IIndexedModel> Items
    {
        get => items!;
        set
        {
            items = value;
            OnPropertyChanged();
        }
    }

    private string? title;
    public string Title
    {
        get => title!;
        set
        {
            title = value;
            OnPropertyChanged();
        }
    }

    private IIndexedModel? selectedItem;
    public IIndexedModel SelectedItem
    {
        get => selectedItem!;
        set
        {
            selectedItem = value;
            OnPropertyChanged();
        }
    }
    public PickerPopup(string title, IEnumerable<IIndexedModel> items)
    {
        InitializeComponent();
        Title = title;
        WidthRequest = 0.9 * (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density);
        HeightRequest = 0.8 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density);
        Items = items.ToList();
    }

    [RelayCommand]
    private async Task SelectionChanged()
    {
        await CloseAsync(SelectedItem);
    }
}