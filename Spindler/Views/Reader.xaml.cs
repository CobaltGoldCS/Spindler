using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Spindler.CustomControls;

public partial class Reader : Grid
{
    private string fontFamily = Preferences.Default.Get("font", "OpenSans (Regular)");
    public string FontFamily
    {
        get => fontFamily;
        set
        {
            fontFamily = value;
            OnPropertyChanged(nameof(FontFamily));
        }
    }

    private int fontSize = Preferences.Default.Get("font_size", 15);
    public int FontSize
    {
        get => fontSize;
        set
        {
            fontSize = value;
            OnPropertyChanged(nameof(FontSize));
        }
    }

    private float lineHeight = Preferences.Default.Get("line_spacing", 1.5f);
    public float LineHeight
    {
        get => lineHeight;
        set
        {
            lineHeight = value;
            OnPropertyChanged(nameof(LineHeight));
        }
    }

    private float itemSpacing = Preferences.Default.Get("item_spacing", 3f);
    public float ItemSpacing
    {
        get => itemSpacing;
        set
        {
            itemSpacing = value;
            OnPropertyChanged(nameof(ItemSpacing));
        }
    }


    public Reader()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TextProperty =
                BindableProperty.Create(nameof(Text), typeof(ObservableCollection<string>), typeof(Grid));

    public static readonly BindableProperty TextTypeProperty =
                BindableProperty.Create(nameof(TextType), typeof(TextType), typeof(Grid));

    public static readonly BindableProperty TitleProperty =
                BindableProperty.Create(nameof(Title), typeof(string), typeof(Grid));

    public static readonly BindableProperty LoadingProperty =
            BindableProperty.Create(nameof(Loading), typeof(bool), typeof(Grid));

    public static readonly BindableProperty PrevVisibleProperty =
            BindableProperty.Create(nameof(PrevVisible), typeof(bool), typeof(Grid));

    public static readonly BindableProperty NextVisibleProperty =
            BindableProperty.Create(nameof(NextVisible), typeof(bool), typeof(Grid));

    public static readonly BindableProperty PreviousCommandProperty =
            BindableProperty.Create(nameof(PreviousCommand), typeof(ICommand), typeof(Grid));

    public static readonly BindableProperty PreviousCommandParameterProperty =
            BindableProperty.Create(nameof(PreviousCommandParameter), typeof(object), typeof(Grid));

    public static readonly BindableProperty NextCommandProperty =
            BindableProperty.Create(nameof(NextCommand), typeof(ICommand), typeof(Grid));

    public static readonly BindableProperty NextCommandParameterProperty =
            BindableProperty.Create(nameof(NextCommandParameter), typeof(object), typeof(Grid));

    public event EventHandler? PrevClicked;
    public event EventHandler? NextClicked;

    public ObservableCollection<string> Text
    {
        get
        {
            return (ObservableCollection<string>)GetValue(TextProperty);
        }
        set { SetValue(TextProperty, value); }
    }


    public TextType TextType
    {
        get => (TextType)GetValue(TextTypeProperty);
        set { SetValue(TextTypeProperty, value); }
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set { SetValue(TitleProperty, value); }
    }

    public bool Loading
    {
        get => (bool)GetValue(LoadingProperty);
        set { SetValue(LoadingProperty, value); }
    }


    public bool PrevVisible
    {
        get => (bool)GetValue(PrevVisibleProperty);
        set { SetValue(PrevVisibleProperty, value); }
    }


    public bool NextVisible
    {
        get => (bool)GetValue(NextVisibleProperty);
        set { SetValue(NextVisibleProperty, value); }
    }

    public ICommand PreviousCommand
    {
        get => (ICommand)GetValue(PreviousCommandProperty);
        set { SetValue(PreviousCommandProperty, value); }
    }

    public object PreviousCommandParameter
    {
        get => GetValue(PreviousCommandParameterProperty);
        set { SetValue(PreviousCommandParameterProperty, value); }
    }

    public ICommand NextCommand
    {
        get => (ICommand)GetValue(NextCommandProperty);
        set { SetValue(NextCommandProperty, value); }
    }

    public object NextCommandParameter
    {
        get => GetValue(NextCommandParameterProperty);
        set { SetValue(NextCommandParameterProperty, value); }
    }

    private void Prev_Clicked(object sender, EventArgs e)
    {
        PrevClicked?.Invoke(sender, e);
    }

    private void Next_Clicked(object sender, EventArgs e)
    {
        NextClicked?.Invoke(sender, e);
    }
}