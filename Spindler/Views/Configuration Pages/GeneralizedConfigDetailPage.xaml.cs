using Spindler.Behaviors;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views.Configuration_Pages;

namespace Spindler;

[QueryProperty(nameof(config), "config")]
public partial class GeneralizedConfigDetailPage : BaseConfigDetailPage<GeneralizedConfig>
{

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

    protected override void InitializePage(GeneralizedConfig config)
    {
        base.InitializePage(config);
        switch (state)
        {
            case State.NewConfig:
                Buttons.OkText = "Add";
                importButton.IsEnabled = true;
                Title = "Add a new Config";
                break;
            case State.ModifyConfig:
                Buttons.OkText = "Modify";
                exportButton.IsEnabled = true;
                Title = $"Modify {config.DomainName}";
                break;
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

    protected override async void okButton_Clicked(object sender, EventArgs e)
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
        base.okButton_Clicked(sender, e);
    }

    #endregion
}