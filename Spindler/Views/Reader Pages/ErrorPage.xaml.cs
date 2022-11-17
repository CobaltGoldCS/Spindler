using Spindler.Models;
using Spindler.Utils;

namespace Spindler.Views;

[QueryProperty(nameof(Config), "config")]
[QueryProperty(nameof(Message), "errormessage")]
public partial class ErrorPage : ContentPage
{


    private Config? config = null;
    public Config? Config
    {
        get => config;
        set
        {
            config = value;
            OnConfigSet();
        }
    }
    public string Message
    {
        set
        {
            LoadPage(value);
        }
    }
    private void LoadPage(string message)
    {
        ErrorLabel.Text = message;
    }

    private void OnConfigSet()
    {
        HeadlessMode.On = Config?.ExtraConfigs.GetOrDefault("headless", false) ?? false;
    }
    public ErrorPage()
    {
        InitializeComponent();
        Shell.Current.Navigating += OnNavigate;
    }

    private async void OnNavigate(object? sender, ShellNavigatingEventArgs e)
    {
        if (Config is not null)
            await App.Database.SaveItemAsync(Config!);
    }

    private void HeadlessMode_OnChanged(object sender, ToggledEventArgs e)
    {
        if (Config is not null)
            Config.ExtraConfigs["headless"] = HeadlessMode.On;
    }
}