namespace Spindler.Views.TableReplacements;

public partial class LabelSwitch : ContentView
{
    public static readonly BindableProperty TextProperty =
                BindableProperty.Create(nameof(Text), typeof(string), typeof(LabelSwitch));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty OnProperty =
                BindableProperty.Create(nameof(On), typeof(bool), typeof(LabelSwitch), defaultBindingMode: BindingMode.TwoWay);

    public bool On
    {
        get => (bool)GetValue(OnProperty);
        set => SetValue(OnProperty, value);
    }

    public LabelSwitch()
    {
        InitializeComponent();
    }
}