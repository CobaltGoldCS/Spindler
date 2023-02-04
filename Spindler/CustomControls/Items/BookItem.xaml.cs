using Spindler.Models;
using System.Windows.Input;

namespace Spindler.CustomControls.Items;

public partial class BookItem : Border
{
    public static readonly BindableProperty DoubleTappedCommandProperty =
                BindableProperty.Create(nameof(DoubleTappedCommand), typeof(ICommand), typeof(BookItem));

    public static readonly BindableProperty SelectionCommandProperty =
                BindableProperty.Create(nameof(SelectionCommand), typeof(ICommand), typeof(BookItem));

    public static readonly BindableProperty BookParameterProperty =
                BindableProperty.Create(nameof(BookParameter), typeof(Book), typeof(BookItem));

    public ICommand DoubleTappedCommand
    {
        get => (ICommand)GetValue(DoubleTappedCommandProperty);
        set => SetValue(DoubleTappedCommandProperty, value);
    }

    public ICommand SelectionCommand
    {
        get => (ICommand)GetValue(SelectionCommandProperty);
        set => SetValue(SelectionCommandProperty, value);
    }

    public Book BookParameter
    {
        get => (Book)GetValue(BookParameterProperty);
        set => SetValue(BookParameterProperty, value);
    }

    public BookItem()
	{
		InitializeComponent();
	}

    private void Tapped(object sender, TappedEventArgs e)
    {
        Stroke = Application.Current!.RequestedTheme == AppTheme.Light ? Colors.White : Colors.Black;
        BackgroundColor = Colors.Transparent;
    }
}