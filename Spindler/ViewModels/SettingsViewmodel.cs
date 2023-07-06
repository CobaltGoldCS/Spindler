using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.Models;
using Spindler.Utilities;
using Spindler.Views;

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
    public string[] layoutList = { "List", "Grid" };

    private string selectedLayout = Preferences.Default.Get("book list layout", "List"); // Default is LayoutType.List
    public string SelectedLayout
    {
        get => selectedLayout;
        set
        {
            SetProperty(ref selectedLayout, value);
            Preferences.Set("book list layout", value);
        }
    }

    [ObservableProperty]
    private bool isBusy = false;

    private Theme selectedTheme = Theme.FromThemeType((Themes)Preferences.Get("theme", (int)Themes.Default));

    public Theme SelectedTheme
    {
        get => selectedTheme;
        set
        {
            if (selectedTheme == value)
                return;
            SetProperty(ref selectedTheme, value);
            Preferences.Set("theme", (int)value.theme);
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(value));
        }
    }

    [ObservableProperty]
    public Theme[] possibleThemes = ((Themes[])Enum.GetValues(typeof(Themes)))
        .Select(themes => Theme.FromThemeType(themes))
        .ToArray();

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
