using Spindler.Models;

namespace Spindler.Views.CustomControls.Items;

public partial class ConfigItem : Border
{

    public static readonly BindableProperty ConfigParameterProperty =
                BindableProperty.Create(nameof(ConfigParameter), typeof(Config), typeof(ConfigItem));

    public Config ConfigParameter
    {
        get => (Config)GetValue(ConfigParameterProperty);
        set => SetValue(ConfigParameterProperty, value);
    }
    public ConfigItem()
    {
        InitializeComponent();
    }
}