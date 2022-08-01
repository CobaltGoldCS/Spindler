using Spindler.Models;
using Spindler.Services;
using Spindler.Behaviors;
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
            okButton.Text = "Add a new Config";
            Title = "Add a new Config";
        }
        else
        {
            config = await App.Database.GetConfigByIdAsync(id);
            okButton.Text = $"Modify {config.DomainName}";
            Title = $"Modify {config.DomainName}";
        }
        BindingContext = config;
    }
    #endregion



    public ConfigDetailPage()
    {
        InitializeComponent();

        domainEntry.Behaviors.Add(new TextValidationBehavior((string value) =>
        {
            return domainValidationRegex.IsMatch(value);
        }));

        TextValidationBehavior validSelectorBehavior = new((string value) => PathService.IsValidSelector(value));
        contentEntry.Behaviors.Add(validSelectorBehavior);
        nextEntry.Behaviors.Add(validSelectorBehavior);
        prevEntry.Behaviors.Add(validSelectorBehavior);
    }

    #region Click Handlers
    private async void DeleteButton_Clicked(object sender, EventArgs e)
    {
        if (ConfigurationId < 0)
            return;
        await App.Database.DeleteItemAsync(await App.Database.GetConfigByIdAsync(ConfigurationId));
        await Shell.Current.GoToAsync("..");
    }

    private async void okButton_Clicked(object sender, EventArgs e)
    {
        if (!domainValidationRegex.IsMatch(domainEntry.Text) ||
            !PathService.IsValidSelector(contentEntry.Text) ||
            !PathService.IsValidSelector(nextEntry.Text)    ||
            !PathService.IsValidSelector(prevEntry.Text))
        {
            return;
        }


        Config config = new()
        {
            Id = ConfigurationId,
            DomainName = domainEntry.Text,
            ContentPath = contentEntry.Text,
            NextUrlPath = nextEntry.Text,
            PrevUrlPath = prevEntry.Text,
            TitlePath = titleEntry.Text,
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