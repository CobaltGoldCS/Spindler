using Spindler.Models;
using Spindler.Services;
using Spindler.Behaviors;
using Spindler.Utils;
using System.Text.RegularExpressions;

namespace Spindler;

[QueryProperty(nameof(ConfigId), "id")]
public partial class ConfigDetailPage : ContentPage
{
    private int ConfigurationId = -1;

    #region Constants
    private readonly Regex domainValidationRegex = new(@"^(?!www\.)(((?!\-))(xn\-\-)?[a-z0-9\-_]{0,61}[a-z0-9]{1,1}\.)*(xn\-\-)?([a-z0-9\-]{1,61}|[a-z0-9\-]{1,30})\.[a-z]{2,}$");
    #endregion

    #region QueryProperty handler
    public string ConfigId
    {
        get => ConfigurationId.ToString();
        set
        {
            int id = Convert.ToInt32(value);
            ConfigurationId = id;
            InitializePage(id);
        }
    }

    private async void InitializePage(int id)
    {
        Config config;
        if (ConfigurationId < 0)
        {
            config = new Config
            {
                Id = -1,
                DomainName = "",
                ContentPath = "",
                NextUrlPath = "",
                PrevUrlPath = "",
                TitlePath = "",
            };
            okButton.Text = "Add";
            Title = "Add a new Config";
        }
        else
        {
            config = await App.Database.GetItemByIdAsync<Config>(id);
            okButton.Text = "Modify";
            Title = $"Modify {config.DomainName}";
        }
        BindingContext = config;
        switchWebView.On = (bool)config.ExtraConfigs.GetOrDefault("webview", false);
        animationSwitch.On = (bool)config.ExtraConfigs.GetOrDefault("autoscrollanimation", true);
        cookieSwitch.On = (bool)config.ExtraConfigs.GetOrDefault("requirescookies", false);
        separatorEntry.Text = ((string)config.ExtraConfigs.GetOrDefault("separator", "\n"))
            .Replace(System.Environment.NewLine, @"\n")
            .Replace("\t", @"\t");
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
        if (ConfigurationId < 0)
            return;
        await App.Database.DeleteItemAsync(await App.Database.GetItemByIdAsync<Config>(ConfigurationId));
        await Shell.Current.GoToAsync("..");
    }

    private async void okButton_Clicked(object sender, EventArgs e)
    {
        if (!domainValidationRegex.IsMatch(domainEntry.Text) ||
            !ConfigService.IsValidSelector(contentEntry.Text) ||
            !ConfigService.IsValidSelector(nextEntry.Text)    ||
            !ConfigService.IsValidSelector(prevEntry.Text))
        {
            return;
        }

        Dictionary<string, object> extras = new()
        {
            { "webview", switchWebView.On },
            { "autoscrollanimation", animationSwitch.On },
            { "separator", separatorEntry.Text
                            .Replace(@"\n", System.Environment.NewLine)
                            .Replace(@"\t", "     ")},
            { "requirescookies", cookieSwitch.On },
        };

        Config config = new()
        {
            Id = ConfigurationId,
            DomainName = domainEntry.Text,
            ContentPath = contentEntry.Text,
            NextUrlPath = nextEntry.Text,
            PrevUrlPath = prevEntry.Text,
            TitlePath = titleEntry.Text,
            ExtraConfigs = extras
        };
        await App.Database.SaveItemAsync(config);
        await Shell.Current.GoToAsync("..");
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    #endregion
}