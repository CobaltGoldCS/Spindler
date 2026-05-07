using Spindler.Models;
using System.Windows.Input;

namespace Spindler.Views.CustomControls.Items;

public partial class PinnedBookItem : Border
{
    public static readonly BindableProperty DoubleTappedCommandProperty =
                    BindableProperty.Create(nameof(DoubleTappedCommand), typeof(ICommand), typeof(PinnedBookItem));

    public static readonly BindableProperty SelectionCommandProperty =
                BindableProperty.Create(nameof(SelectionCommand), typeof(ICommand), typeof(PinnedBookItem));

    public static readonly BindableProperty BookParameterProperty =
                BindableProperty.Create(nameof(BookParameter), typeof(Book), typeof(PinnedBookItem));

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

    private void Tapped(object sender, TappedEventArgs e)
    {
        Stroke = Application.Current!.RequestedTheme == AppTheme.Light ? Colors.White : Colors.Black;
        BackgroundColor = Colors.Transparent;
    }
    public PinnedBookItem()
    {
        InitializeComponent();
    }
}