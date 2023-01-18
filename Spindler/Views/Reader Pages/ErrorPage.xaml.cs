using Newtonsoft.Json;
using Spindler.Models;
using Spindler.Utilities;
using System.Text.Json.Nodes;

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
    }


    private async void HeadlessMode_OnChanged(object sender, ToggledEventArgs e)
    {
        if (Config is null)
        {
            return;
        }
        Config.ExtraConfigs["headless"] = HeadlessMode.On;
        Config.ExtraConfigsBlobbed = JsonConvert.SerializeObject(Config.ExtraConfigs);
        await App.Database.SaveItemAsync(Config!);
    }
}