using Spindler.Models;
using Spindler.Utils;

namespace Spindler.Views;

[QueryProperty(nameof(BookId), "id")]
[QueryProperty(nameof(Message), "errormessage")]
public partial class ErrorPage : ContentPage
{

    private string? bookid = null;
    public string? BookId
    {
        get => bookid;
        set
        {
            bookid = value!;
            if (int.TryParse(bookid, out int id))
                OnBookIdSet(id);
        }
    }

    public Config? Config = null;
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

    private async void OnBookIdSet(int id)
    {
        Config = await App.Database.GetItemByIdAsync<Config>(id);
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

    private async void WebviewButton_Clicked(object sender, EventArgs e)
    {
        if (!ValidId()) return;
        await Shell.Current.GoToAsync($"../{nameof(WebviewReaderPage)}?id={BookId}");
    }

    private bool ValidId() => BookId != null;

    private void HeadlessMode_OnChanged(object sender, ToggledEventArgs e)
    {
        if (Config is not null)
            Config.ExtraConfigs["headless"] = HeadlessMode.On;
    }
}