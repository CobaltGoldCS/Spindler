using System.Windows.Input;

namespace Spindler.CustomControls;

public partial class AddButtonGroup : HorizontalStackLayout
{
    public event EventHandler? DeleteClicked;
    public event EventHandler? OkClicked;
    public event EventHandler? CancelClicked;

    public AddButtonGroup()
	{
		InitializeComponent();
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
        DeleteClicked?.Invoke(sender, e);
    }

    private void okButton_Clicked(object sender, EventArgs e)
    {
        OkClicked?.Invoke(sender, e);
    }

    private void Cancel_Clicked(object sender, EventArgs e)
    {
        CancelClicked?.Invoke(sender, e);
    }

    public static readonly BindableProperty DeleteCommandProperty =
                BindableProperty.Create(nameof(DeleteCommand), typeof(ICommand), typeof(AddButtonGroup));

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public static readonly BindableProperty OkCommandProperty =
                BindableProperty.Create(nameof(OkCommand), typeof(ICommand), typeof(AddButtonGroup));

    public ICommand OkCommand
    {
        get => (ICommand)GetValue(OkCommandProperty);
        set => SetValue(OkCommandProperty, value);
    }

    public static readonly BindableProperty CancelCommandProperty =
                BindableProperty.Create(nameof(CancelCommand), typeof(ICommand), typeof(AddButtonGroup));

    public ICommand CancelCommand
    {
        get => (ICommand)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public static readonly BindableProperty OkTextProperty =
                BindableProperty.Create(nameof(OkText), typeof(string), typeof(AddButtonGroup), defaultValue: "Add");

    public string OkText
    {
        get => (string)GetValue(OkTextProperty);
        set => SetValue(OkTextProperty, value);
    }
}