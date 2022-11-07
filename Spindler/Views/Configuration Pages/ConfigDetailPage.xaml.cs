using Spindler.Behaviors;
using Spindler.Models;
using Spindler.Services;
using Spindler.Utils;
using System.Text.RegularExpressions;

namespace Spindler;

[QueryProperty(nameof(config), "config")]
public partial class ConfigDetailPage : ContentPage
{
    private Config Configuration = new() { Id = -1 };

    #region Constants
    private readonly Regex domainValidationRegex = new(@"^(?!www\.)(((?!\-))(xn\-\-)?[a-z0-9\-_]{0,61}[a-z0-9]{1,1}\.)*(xn\-\-)?([a-z0-9\-]{1,61}|[a-z0-9\-]{1,30})\.[a-z]{2,}$");
    #endregion

    #region QueryProperty handler
    public Config config
    {
        get => Configuration;
        set
        {
            Configuration = value;
            InitializePage(value);
        }
    }

    private async void InitializePage(Config config)
    {
        if (config.Id < 0)
        {
            /*config = new Config
            {
                Id = -1,
                DomainName = "",
                ContentPath = "",
                NextUrlPath = "",
                PrevUrlPath = "",
                TitlePath = "",
            };*/
            okButton.Text = "Add";
            Title = "Add a new Config";
        }
        else
        {
            okButton.Text = "Modify";
            Title = $"Modify {config.DomainName}";
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



    public ConfigDetailPage()
    {
        InitializeComponent();

        domainEntry.Behaviors.Add(new TextValidationBehavior((string value) =>
        {
            return domainValidationRegex.IsMatch(value);
        }));

        TextValidationBehavior validSelectorBehavior = new((string value) => ConfigService.IsValidSelector(value));
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
        if (!domainValidationRegex.IsMatch(domainEntry.Text) ||
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

        config.DomainName = domainEntry.Text;
        config.ContentPath = contentEntry.Text;
        config.NextUrlPath = nextEntry.Text;
        config.PrevUrlPath = prevEntry.Text;
        config.TitlePath = titleEntry.Text;

        await App.Database.SaveItemAsync(config);
        await Shell.Current.GoToAsync("..");
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    #endregion
}