using Newtonsoft.Json;
using Spindler.Models;
using Spindler.Utilities;

namespace Spindler.Views;

[QueryProperty(nameof(Config), "config")]
[QueryProperty(nameof(Message), "errormessage")]
public partial class ErrorPage : ContentPage
{

    #region Attributes
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
    public bool ShouldReload = false;

    public string Message
    {
        set
        {
            LoadPage(value);
        }
    }

    #endregion

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
        Shell.Current.Navigating += Navigating;
    }

    private async void Navigating(object? sender, ShellNavigatingEventArgs e)
    {
        // Hopefully Prevent Duplicate Error Pages from being created at the same time
        if (e.Target.Location.OriginalString.Contains(nameof(ErrorPage)))
        {
            e.Cancel();
            return;
        }
        // Redirect to BookPage If Normal Back Navigation button is pressed
        if (e.Target.Location.OriginalString == ".." && !ShouldReload)
        {
            e.Cancel();
            await Shell.Current.GoToAsync("../..");
        }
        Shell.Current.Navigating -= Navigating;
    }

    private async void HeadlessMode_OnChanged(object sender, ToggledEventArgs e)
    {
        if (Config is null)
        {
            return;
        }
        Config.ExtraConfigs["headless"] = HeadlessMode.On;
        await App.Database.SaveItemAsync(Config!);
    }

    private async void ReloadClicked(object sender, EventArgs e)
    {
        ShouldReload = true;
        await Shell.Current.GoToAsync("..");
    }
}