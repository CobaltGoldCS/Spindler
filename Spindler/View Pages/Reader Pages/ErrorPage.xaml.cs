using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using System.Runtime.CompilerServices;

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
            // TODO: Make this reactable for Config to actually be affected
            HeadlessMode.On = config?.UsesHeadless ?? false;
        }
    }
    public bool ShouldReload = false;

    public string Message
    {
        set
        {
            ErrorLabel.Text = value;
        }
    }

    #endregion

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
        Config.UsesHeadless = HeadlessMode.On;
        if (Config.Id > -1)
            await App.Database.SaveItemAsync(Config!);
    }

    private async void ReloadClicked(object sender, EventArgs e)
    {
        ShouldReload = true;
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task BackButton()
    {
        await Shell.Current.GoToAsync("../..");
    }
}