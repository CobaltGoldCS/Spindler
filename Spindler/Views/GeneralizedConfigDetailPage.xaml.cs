using Spindler.Models;
using Spindler.Services;
using Spindler.Behaviors;
using System.Text.RegularExpressions;

namespace Spindler;

[QueryProperty(nameof(ConfigId), "id")]
public partial class GeneralizedConfigDetailPage : ContentPage
{
    private int ConfigurationId = -1;

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
        GeneralizedConfig config;
        if (ConfigurationId < 0)
        {
            config = new GeneralizedConfig
            {
                Id = -1,
                DomainName = "",
                MatchPath = "",
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
            config = await App.Database.GetItemByIdAsync<GeneralizedConfig>(id);
            okButton.Text = $"Modify {config.DomainName}";
            Title = $"Modify {config.DomainName}";
        }
        BindingContext = config;
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
        if (ConfigurationId < 0)
            return;
        await App.Database.DeleteItemAsync(await App.Database.GetItemByIdAsync<GeneralizedConfig>(ConfigurationId));
        await Shell.Current.GoToAsync("..");
    }

    private async void okButton_Clicked(object sender, EventArgs e)
    {
        if (
            !ConfigService.IsValidSelector(matchEntry.Text) ||
            !ConfigService.IsValidSelector(contentEntry.Text) ||
            !ConfigService.IsValidSelector(nextEntry.Text)    ||
            !ConfigService.IsValidSelector(prevEntry.Text))
        {
            return;
        }


        GeneralizedConfig config = new()
        {
            Id = ConfigurationId,
            MatchPath = matchEntry.Text,
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