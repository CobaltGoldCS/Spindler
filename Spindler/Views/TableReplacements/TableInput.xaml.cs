namespace Spindler.Views.TableReplacements;

public partial class TableInput : ContentView
{
    public static readonly BindableProperty LabelProperty =
                BindableProperty.Create(nameof(Label), typeof(string), typeof(DetailLabel));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly BindableProperty TextProperty =
                BindableProperty.Create(nameof(Text), typeof(string), typeof(DetailLabel));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public TableInput()
    {
        InitializeComponent();
    }
}