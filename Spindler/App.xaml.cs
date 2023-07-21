using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Resources;
using Spindler.Views.Book_Pages;
using SQLitePCL;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.Models;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Kotlin.Jvm.Internal;
using static Android.Graphics.BlurMaskFilter;
using CommunityToolkit.Maui.Core.Extensions;

namespace Spindler;

public partial class App : Application
{
    private static DataService? database;
    public static DataService Database
    {
        get
        {
            database ??= new DataService(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spindler.db"));
            return database;
        }
    }

    public App()
    {
        InitializeComponent();
        SetTheme();
        Batteries.Init();
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookDetailPage)}", typeof(BookDetailPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookDetailPage)}/{nameof(BookSearcherPage)}", typeof(BookSearcherPage));

        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookPage)}", typeof(BookPage));

        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(ReaderPage)}", typeof(ReaderPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(WebviewReaderPage)}", typeof(WebviewReaderPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(ErrorPage)}", typeof(ErrorPage));

        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookListDetailPage)}", typeof(BookListDetailPage));
        Routing.RegisterRoute("Config/" + nameof(ConfigDetailPage), typeof(ConfigDetailPage));
        Routing.RegisterRoute("GeneralConfig/" + nameof(GeneralizedConfigDetailPage), typeof(GeneralizedConfigDetailPage));

        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (theme, message) => {
            SetTheme(message.Value);
        });
    }


    public void SetTheme(Theme? theme = null)
    {
        Current?.Resources.MergedDictionaries.Clear();
        if (theme is null)
        {
            var themeType = Preferences.Get("theme", (int)Themes.Default);
            theme = Theme.FromThemeType((Themes)themeType);
        }
        ResourceDictionary resourceDictionary = theme.theme switch
        {
            Themes.Default => new Resources.Styles.Default(),
            Themes.Dracula => new Resources.Styles.Dracula(),
            Themes.Seasalt => new Resources.Styles.Seasalt(),
            Themes.TeaRose => new Resources.Styles.Tearose(),
            _ => throw new NotImplementedException()
        };
        MainPage.Behaviors.Clear();
        Current?.Resources.MergedDictionaries.Add(resourceDictionary);
        Current?.Resources.MergedDictionaries.Add(new Resources.Setters());

        var statusBarColor = (Color)resourceDictionary["CardBackground"];
        StatusBarStyle bestContrast = (statusBarColor.GetByteRed() * 0.299 + statusBarColor.GetByteGreen() * 0.587 + statusBarColor.GetByteBlue() * 0.114) > 186 ? StatusBarStyle.DarkContent : StatusBarStyle.LightContent;

#if !MACCATALYST
        MainPage.Behaviors.Add(new StatusBarBehavior
        {
            StatusBarColor = statusBarColor,
            StatusBarStyle = bestContrast
        });
#endif

        // These are necessary in order to prevent crashing while allowing themes to override styles
        // Add a resource dictionary with lower priority, then remove the one with top priority
        Current?.Resources.MergedDictionaries.Add(resourceDictionary);
        Current?.Resources.MergedDictionaries.Remove(resourceDictionary);


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

