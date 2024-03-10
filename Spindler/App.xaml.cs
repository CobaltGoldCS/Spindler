using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.Resources;
using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Book_Pages;
using SQLitePCL;

namespace Spindler;

public partial class App : Application
{
    private ResourceDictionary Setters = new Setters();

    public App()
    {
        InitializeComponent();
        SetTheme();
        Batteries.Init();
        RegisterRoutes();

        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (theme, message) =>
        {
            SetTheme(message.Value);
        });
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookDetailPage)}", typeof(BookDetailPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookDetailPage)}/{nameof(BookSearcherPage)}", typeof(BookSearcherPage));

        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookPage)}", typeof(BookPage));

        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(ReaderPage)}", typeof(ReaderPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(WebviewReaderPage)}", typeof(WebviewReaderPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(ErrorPage)}", typeof(ErrorPage));

        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookListDetailPage)}", typeof(BookListDetailPage));
        Routing.RegisterRoute("Config/" + nameof(ConfigDetailPage), typeof(ConfigDetailPage));
        Routing.RegisterRoute("GeneralConfig/" + nameof(ConfigDetailPage), typeof(ConfigDetailPage));
    }


    public void SetTheme()
    {
        var themeType = Preferences.Get("theme", (int)Themes.Default);
        var theme = Theme.FromThemeType((Themes)themeType);
        SetTheme(theme);
    }
    public void SetTheme(Theme theme)
    {
        Current!.Resources.MergedDictionaries.Clear();
        ResourceDictionary resourceDictionary = theme.theme switch
        {
            Themes.Default => new Resources.Styles.Default(),
            Themes.Dracula => new Resources.Styles.Dracula(),
            Themes.Seasalt => new Resources.Styles.Seasalt(),
            Themes.TeaRose => new Resources.Styles.Tearose(),
            _ => throw new NotImplementedException()
        };
        MainPage.Behaviors.Clear();
        Current!.Resources.MergedDictionaries.Add(resourceDictionary);
        Current!.Resources.MergedDictionaries.Add(Setters);

        var statusBarColor = (Color)resourceDictionary["CardBackground"];
        StatusBarStyle bestContrast = (statusBarColor.GetByteRed() * 0.299 + statusBarColor.GetByteGreen() * 0.587 + statusBarColor.GetByteBlue() * 0.114) > 186 
            ? StatusBarStyle.DarkContent 
            : StatusBarStyle.LightContent;

#if !MACCATALYST
        MainPage.Behaviors.Add(new StatusBarBehavior
        {
            StatusBarColor = statusBarColor,
            StatusBarStyle = bestContrast
        });
#endif

        // These are necessary in order to prevent crashing while allowing themes to override styles
        // Add a resource dictionary with lower priority, then remove the one with top priority
        Current!.Resources.MergedDictionaries.Add(resourceDictionary);
        Current!.Resources.MergedDictionaries.Remove(resourceDictionary);


        WeakReferenceMessenger.Default.Send(new ResourceDictionaryUpdatedMessage(resourceDictionary));
    }
}


public enum Themes
{
    Default,
    Dracula,
    Seasalt,
    TeaRose
}

public class Theme
{
    public string safeName;
    public Themes theme;

    public Theme(string safeName, Themes theme)
    {
        this.safeName = safeName;
        this.theme = theme;
    }

    public override string ToString()
    {
        return safeName;
    }

    public static Theme FromThemeType(Themes theme)
    {
        return new Theme(Enum.GetName(typeof(Themes), theme)!, theme);
    }
}

