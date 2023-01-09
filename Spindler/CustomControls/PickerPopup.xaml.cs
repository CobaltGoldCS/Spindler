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
	static byte INITIAL_ITEMS = 9;
    static byte NUM_ITEMS_ADDED_TO_LIST = 5;

    public IList<object> Items { get; set; }
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

    private ObservableCollection<object>? renderedItems;
    public ObservableCollection<object> RenderedItems
    {
        get => renderedItems!;
        set
        {
            renderedItems = value;
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

    public PickerPopup(string title, IEnumerable<object> items)
    {
        InitializeComponent();
        Title = title;
        Size  = new(0.9 * (DeviceDisplay.MainDisplayInfo.Width  / DeviceDisplay.MainDisplayInfo.Density),
                    0.8 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density));
        Items = items.ToList();
        RenderedItems = new(Items.Take(INITIAL_ITEMS));
    }

    [RelayCommand]
    public void EndOfListReached()
    {
        foreach (object item in Items!.Skip(RenderedItems.Count).Take(NUM_ITEMS_ADDED_TO_LIST))
            RenderedItems!.Add(item);
    }
    [RelayCommand]
    private void SelectionChanged()
    {
        Close(selectedItem);
    }
}