using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HtmlAgilityPack;
using Spindler.CustomControls;
using Spindler.Models;
using Spindler.Services;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;

namespace Spindler.ViewModels;

public partial class SettingsViewmodel : ObservableObject
{
    private string font = Preferences.Default.Get("font", "OpenSansRegular");
    public string Font
    {
        get => font;
        set
        {
            SetProperty(ref font, value);
            Preferences.Set("font", value);
        }
    }

    private int fontSize = Preferences.Default.Get("font_size", 12);
    public int FontSize
    {
        get => fontSize;
        set
        {
            SetProperty(ref fontSize, value);
            Preferences.Set("font_size", value);
        }
    }

    private float fontSpacing = Preferences.Default.Get("line_spacing", 1.5f);
    public float FontSpacing
    {
        get => fontSpacing;
        set 
        { 
            SetProperty(ref fontSpacing, value);
            Preferences.Set("line_spacing", value);
        }
    }

    [ObservableProperty]
    private bool isBusy = false;

    [RelayCommand]
    public async void CheckNextChapter() {
        if (IsBusy) return;
        IsBusy = true;

        NextChapterService service = new();
        await service.Run();
        IsBusy = false;
    }

    public SettingsViewmodel() 
    {
        Shell.Current.Navigating += Navigating;
    }

    private void Navigating(object? sender, ShellNavigatingEventArgs e) 
    {
        if (IsBusy) 
        {
            e.Cancel();
            return;
        }
        Shell.Current.Navigating -= Navigating;
    }
}
