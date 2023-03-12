using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices;
using Spindler.Models;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using static Microsoft.Maui.ApplicationModel.Permissions;


namespace Spindler.CustomControls;

public partial class PickerPopup : Popup
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

    private object? selectedItem;
    public object SelectedItem
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
        double width = 0.9 * (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density);
        double height = 0.8 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density);
        Size  = new(width, height);
        Items = items.ToList();
    }

    [RelayCommand]
    private void SelectionChanged()
    {
        Close(selectedItem);
    }
}