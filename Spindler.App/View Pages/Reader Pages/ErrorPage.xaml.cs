using CommunityToolkit.Mvvm.Input;
using Spindler.Models;
using Spindler.Services;

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

    IDataService Database { get; set; }

    #endregion

    public ErrorPage(IDataService dataService)
    {
        InitializeComponent();
        Database = dataService;
    }

    private async void HeadlessMode_OnChanged(object sender, ToggledEventArgs e)
    {
        if (Config is null)
        {
            return;
        }
        Config.UsesHeadless = HeadlessMode.On;
        if (Config.Id > -1)
            await Database.SaveItemAsync(Config!);
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