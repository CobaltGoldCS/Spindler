using Spindler.Behaviors;
using Spindler.Models;
using Spindler.Services.Web;
using Spindler.Views.Configuration_Pages;

namespace Spindler;

public partial class GeneralizedConfigDetailPage : BaseConfigDetailPage<GeneralizedConfig>
{

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        base.ApplyQueryAttributes(query);
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
                Title = $"Modify {Configuration.DomainName}";
                break;
        }
    }



    public GeneralizedConfigDetailPage()
    {
        InitializeComponent();


        TextValidationBehavior validSelectorBehavior = new(ConfigService.IsValidSelector);
        matchEntry.Behaviors.Add(validSelectorBehavior);
        contentEntry.Behaviors.Add(validSelectorBehavior);
        nextEntry.Behaviors.Add(validSelectorBehavior);
        prevEntry.Behaviors.Add(validSelectorBehavior);
    }



    #region Click Handlers

    protected override void okButton_Clicked(object sender, EventArgs e)
    {
        if (
            !ConfigService.IsValidSelector(matchEntry.Text) ||
            !ConfigService.IsValidSelector(contentEntry.Text) ||
            !ConfigService.IsValidSelector(nextEntry.Text) ||
            !ConfigService.IsValidSelector(prevEntry.Text))
        {
            return;
        }

        Configuration.ExtraConfigs = new()
        {
            { "webview", switchWebView.On },
            { "autoscrollanimation", animationSwitch.On },
            { "separator", separatorEntry.Text
                            .Replace(@"\n", Environment.NewLine)
                            .Replace(@"\t", "     ")},
            { "headless", headlessSwitch.On },
            { "filteringcontentenabled", filterSwitch.On },
            { "contenttype", Convert.ToInt32(((ContentExtractorOption)ContentTypeSelector.SelectedItem).contentType) },
        };
        base.okButton_Clicked(sender, e);
    }

    #endregion

    protected override void SetSwitchesBasedOnExtraConfigs()
    {
        switchWebView.On = Configuration.UsesWebview;
        animationSwitch.On = Configuration.HasAutoscrollAnimation;
        separatorEntry.Text = Configuration.Separator
            .Replace(Environment.NewLine, @"\n")
            .Replace("\t", @"\t");
        headlessSwitch.On = Configuration.UsesHeadless;
        filterSwitch.On = Configuration.FilteringContentEnabled;

        ContentTypeSelector.ItemsSource = possibleExtractors;
        ContentTypeSelector.SelectedItem = selectedExtractor;
    }
}