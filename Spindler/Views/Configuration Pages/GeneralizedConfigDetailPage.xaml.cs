using Spindler.Behaviors;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;

namespace Spindler;

[QueryProperty(nameof(config), "config")]
public partial class GeneralizedConfigDetailPage : ContentPage
{
    private GeneralizedConfig Configuration = new() { Id = -1 };

    #region QueryProperty handler
    public GeneralizedConfig config
    {
        get => Configuration;
        set
        {
            Configuration = value;
            InitializePage(value);
        }
    }

    private void InitializePage(GeneralizedConfig config)
    {
        if (config.Id < 0)
        {
            Title = "Add a new Config";
            Buttons.OkText = "Add";
        }
        else
        {
            Title = $"Modify {config.DomainName}";
            Buttons.OkText = "Modify";
        }
        BindingContext = config;
        switchWebView.On = config.ExtraConfigs.GetOrDefault("webview", false);
        animationSwitch.On = config.ExtraConfigs.GetOrDefault("autoscrollanimation", true);
        separatorEntry.Text = config.ExtraConfigs.GetOrDefault("separator", "\n")
            .Replace(Environment.NewLine, @"\n")
            .Replace("\t", @"\t");
        headlessSwitch.On = config.ExtraConfigs.GetOrDefault("headless", false);
    }
    #endregion



    public GeneralizedConfigDetailPage()
    {
        InitializeComponent();


        TextValidationBehavior validSelectorBehavior = new((string value) => ConfigService.IsValidSelector(value));
        matchEntry.Behaviors.Add(validSelectorBehavior);
        contentEntry.Behaviors.Add(validSelectorBehavior);
        nextEntry.Behaviors.Add(validSelectorBehavior);
        prevEntry.Behaviors.Add(validSelectorBehavior);
    }



    #region Click Handlers
    private async void DeleteButton_Clicked(object sender, EventArgs e)
    {
        if (config.Id < 0)
            return;
        await App.Database.DeleteItemAsync(config);
        await Shell.Current.GoToAsync("..");
    }

    private async void okButton_Clicked(object sender, EventArgs e)
    {
        if (
            !ConfigService.IsValidSelector(matchEntry.Text) ||
            !ConfigService.IsValidSelector(contentEntry.Text) ||
            !ConfigService.IsValidSelector(nextEntry.Text) ||
            !ConfigService.IsValidSelector(prevEntry.Text))
        {
            return;
        }

        config.ExtraConfigs = new()
        {
            { "webview", switchWebView.On },
            { "autoscrollanimation", animationSwitch.On },
            { "separator", separatorEntry.Text
                            .Replace(@"\n", Environment.NewLine)
                            .Replace(@"\t", "     ")},
            { "headless", headlessSwitch.On },
        };
        if (!ConfigService.IsValidSelector(config.ImageUrlPath))
            config.ImageUrlPath = "";

        await App.Database.SaveItemAsync(config);
        await Shell.Current.GoToAsync("..");
    }

    private async void Cancel_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    #endregion
}