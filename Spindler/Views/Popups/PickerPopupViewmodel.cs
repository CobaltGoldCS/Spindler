using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Views;

public partial class PickerPopupViewmodel : ObservableObject, IQueryAttributable
{
    private IPopupService popupService;
    private IEnumerable<IIndexedModel> items = [];
    public IEnumerable<IIndexedModel> Items
    {
        get => items;
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

    [RelayCommand]
    private async Task SelectionChanged()
    {
        await popupService.ClosePopupAsync(Shell.Current, SelectedItem);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Title = (string)query["title"];
        Items = (IEnumerable<IIndexedModel>)query["items"];
    }

    public PickerPopupViewmodel(IPopupService service)
    {
        popupService = service;
    }
}
