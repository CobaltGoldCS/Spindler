using Spindler.Models;
using Spindler.Services;

namespace Spindler;

[QueryProperty(nameof(ConfigId), "id")]
public partial class ConfigDetailPage : ContentPage
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
        if (ConfigurationId < 0)
        {
            BindingContext = new Config
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
            return;
        }
        Config config = await App.Database.GetConfigByIdAsync(id);

        okButton.Text = $"Modify {config.DomainName}";
        Title = $"Modify {config.DomainName}";
        BindingContext = config;
    }
    #endregion

    public ConfigDetailPage()
    {
        InitializeComponent();
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
        if (domainEntry.Text.Length <= 0 ||
            !WebService.IsValidSelector(contentEntry.Text) ||
            !WebService.IsValidSelector(nextEntry.Text)    ||
            !WebService.IsValidSelector(prevEntry.Text))
        {
            return;
        }


        Config config = new Config
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