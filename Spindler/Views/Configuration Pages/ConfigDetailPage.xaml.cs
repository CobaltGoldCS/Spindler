using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json;
using Spindler.Behaviors;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views.Configuration_Pages;
using System.Text;
using System.Text.RegularExpressions;

namespace Spindler;

[QueryProperty(nameof(config), "config")]
public partial class ConfigDetailPage : BaseConfigDetailPage<Config>
{
    public Config config
    {
        get => Configuration;
        set
        {
            Configuration = value;
            InitializePage(value);
        }
    }


    protected override void InitializePage(Config config)
    {
        base.InitializePage(config);
        switch (state)
        {
            case State.NewConfig:
                AddButtonGroup.OkText = "Add";
                importButton.IsEnabled = true;
                Title = "Add a new Config";
                break;
            case State.ModifyConfig:
                AddButtonGroup.OkText = "Modify";
                exportButton.IsEnabled = true;
                Title = $"Modify {config.DomainName}";
                break;
        }
        BindingContext = Configuration;
        switchWebView.On = Configuration.ExtraConfigs.GetOrDefault("webview", false);
        animationSwitch.On = Configuration.ExtraConfigs.GetOrDefault("autoscrollanimation", true);
        separatorEntry.Text = Configuration.ExtraConfigs.GetOrDefault("separator", "\n")
            .Replace(Environment.NewLine, @"\n")
            .Replace("\t", @"\t");
        headlessSwitch.On = Configuration.ExtraConfigs.GetOrDefault("headless", false);
    }



    public ConfigDetailPage()
    {
        
        InitializeComponent();
        domainEntry.Behaviors.Add(new TextValidationBehavior(DomainValidationRegex().IsMatch));

        TextValidationBehavior validSelectorBehavior = new(ConfigService.IsValidSelector);
        contentEntry.Behaviors.Add(validSelectorBehavior);
        nextEntry.Behaviors.Add(validSelectorBehavior);
        prevEntry.Behaviors.Add(validSelectorBehavior);
    }

    #region Click Handlers

    protected override void okButton_Clicked(object sender, EventArgs e)
    {
        if (!DomainValidationRegex().IsMatch(domainEntry.Text) ||
            !ConfigService.IsValidSelector(contentEntry.Text)||
            !ConfigService.IsValidSelector(nextEntry.Text)   ||
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

    protected override void ImportCommand(object sender, EventArgs e)
    {
        base.ImportCommand(sender, e);
        switchWebView.On = Configuration.ExtraConfigs.GetOrDefault("webview", false);
        animationSwitch.On = Configuration.ExtraConfigs.GetOrDefault("autoscrollanimation", true);
        separatorEntry.Text = Configuration.ExtraConfigs.GetOrDefault("separator", "\n")
            .Replace(Environment.NewLine, @"\n")
            .Replace("\t", @"\t");
        headlessSwitch.On = Configuration.ExtraConfigs.GetOrDefault("headless", false);
    }

    [GeneratedRegex("^(?!www\\.)(((?!\\-))(xn\\-\\-)?[a-z0-9\\-_]{0,61}[a-z0-9]{1,1}\\.)*(xn\\-\\-)?([a-z0-9\\-]{1,61}|[a-z0-9\\-]{1,30})\\.[a-z]{2,}$")]
    private static partial Regex DomainValidationRegex();
}