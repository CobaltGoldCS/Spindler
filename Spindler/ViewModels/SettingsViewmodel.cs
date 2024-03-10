using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.Utilities;


namespace Spindler.ViewModels;

public partial class SettingsViewmodel : ObservableObject
{
    private string font = Preferences.Default.Get("font", "OpenSansRegular");
    private static string DatabaseLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spindler.db");
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
    public string[] layoutList = ["List", "Grid"];

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


    // TODO: Create An Automatic Back Up System
    [RelayCommand]
    private static async Task Import()
    {
        if (!await Shell.Current.CurrentPage.DisplayAlert("Warning!", "Importing a new file will delete previous data", "Import", "Nevermind"))
            return;
        FileResult? file = await FilePicker.Default.PickAsync(PickOptions.Default);
        if (file is null)
        {
            return;
        }
        File.Copy(file.FullPath, DatabaseLocation, true);
    }

    [RelayCommand]
    private static async Task Export()
    {

        CancellationToken cancellationToken = new();
        using FileStream stream = File.OpenRead(DatabaseLocation);

        var filePath = await FileSaver.Default.SaveAsync($"SpindlerDatabase.db", stream, cancellationToken);

        if (filePath.IsSuccessful)
        {
            filePath.EnsureSuccess();
            await Toast.Make($"File saved at {filePath.FilePath}").Show(cancellationToken);
        } else
        {
            await Toast.Make($"File not saved: {filePath.Exception}").Show(cancellationToken);
        }
    }
}
