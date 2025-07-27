using Spindler.Behaviors;
using Spindler.Models;
using Spindler.Services.Web;
using Spindler.ViewModels;
using System.Text.RegularExpressions;

namespace Spindler;

public partial class ConfigDetailPage : ContentPage, IQueryAttributable
{
    ConfigDetailViewModel ViewModel { get; init; }

    public virtual void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var config = (query["config"] as Config)!;
        if (config.Id < 0)
        {
            AddButtonGroup.OkText = "Add";
            importButton.IsEnabled = true;
            Title = "Add a new Config";
        }
        else
        {
            AddButtonGroup.OkText = "Modify";
            exportButton.IsEnabled = true;
            Title = $"Modify {config.DomainName}";
        }

        ViewModel.SetConfig(config);
        BindingContext = ViewModel;

        if (config is GeneralizedConfig)
        {
            GeneralizedConfigInfo.IsVisible = true;
        }
        else
        {
            SpecializedConfigInfo.IsVisible = true;
        }
    }



    public ConfigDetailPage(ConfigDetailViewModel viewModel)
    {
        InitializeComponent();
        domainEntry.Behaviors.Add(new TextValidationBehavior(DomainValidation().IsMatch));

        ViewModel = viewModel;

        TextValidationBehavior validSelectorBehavior = new(ConfigService.IsValidSelector);
        contentEntry.Behaviors.Add(validSelectorBehavior);
        nextEntry.Behaviors.Add(validSelectorBehavior);
        prevEntry.Behaviors.Add(validSelectorBehavior);
    }

    [GeneratedRegex("^(?!www\\.)(((?!\\-))(xn\\-\\-)?[a-z0-9\\-_]{0,61}[a-z0-9]{1,1}\\.)*(xn\\-\\-)?([a-z0-9\\-]{1,61}|[a-z0-9\\-]{1,30})\\.[a-z]{2,}$")]
    private static partial Regex DomainValidation();
}