namespace Spindler.CustomControls;

public partial class DetailTextCell : ViewCell
{
    public static readonly BindableProperty DetailTextProperty =
                BindableProperty.Create(nameof(DetailText), typeof(string), typeof(DetailTextCell), defaultValue: "Add");

    public string DetailText
    {
        get => (string)GetValue(DetailTextProperty);
        set => SetValue(DetailTextProperty, value);
    }

    public static readonly BindableProperty TextProperty =
                BindableProperty.Create(nameof(Text), typeof(string), typeof(DetailTextCell), defaultValue: "Add");

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public DetailTextCell()
	{
		InitializeComponent();
	}
}