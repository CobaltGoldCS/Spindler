using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using Spindler.Resources;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Views.Book_Pages;
using SQLitePCL;

using Microsoft.Maui.Handlers;
using Microsoft.Maui.Controls.PlatformConfiguration;

#if ANDROID
using Microsoft.Maui.Handlers;
using Spindler.Platforms.Android;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
#endif

namespace Spindler;

public partial class App : Application, IRecipient<ThemeChangedMessage>, IRecipient<StatusColorUpdateMessage>
{
    private ResourceDictionary Setters = new Setters();

    public App()
    {
        InitializeComponent();
        SetTheme();
        AddMappings();
        Batteries.Init();
        RegisterRoutes();

        WeakReferenceMessenger.Default.RegisterAll(this);
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

    public void Receive(ThemeChangedMessage message)
    {
        SetTheme(message.Value);
    }

    public void Receive(StatusColorUpdateMessage message)
    {
        ResourceDictionary themeDictionary = Current!.Resources.MergedDictionaries.ElementAt(1);


        string? statusString = message.Value;
        statusString ??= themeDictionary.TryGetValue("StatusString", out object status)
            ? (string)status
            : "CardBackground";

        var statusBarColor = ((Color)themeDictionary[statusString]);
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
            Themes.Gunmetal => new Resources.Styles.Gunmetal(),
            Themes.Galaxy => new Resources.Styles.Galaxy(),
            _ => throw new NotImplementedException(),
        };
        MainPage.Behaviors.Clear();
        Current!.Resources.MergedDictionaries.Add(resourceDictionary);
        Current!.Resources.MergedDictionaries.Add(Setters);

        // These are necessary in order to prevent crashing while allowing themes to override styles
        // Add a resource dictionary with lower priority, then remove the one with top priority
        Current!.Resources.MergedDictionaries.Add(resourceDictionary);
        Current!.Resources.MergedDictionaries.Remove(resourceDictionary);


        WeakReferenceMessenger.Default.Send(new ResourceDictionaryUpdatedMessage(resourceDictionary));

        Receive(new StatusColorUpdateMessage(null));
    }

    private void AddMappings()
    {
#if ANDROID
        ResourceDictionary resourceDictionary = Current!.Resources.MergedDictionaries.Where(dict => dict.ContainsKey("Primary")).First();

        var androidPrimary = ((Color)resourceDictionary["Primary"]).ToAndroid();
        var androidHint = ((Color)resourceDictionary["DisabledTextColor"]).ToAndroid();

        PickerHandler.Mapper.AppendToMapping("PickerUnderline", (viewHandler, virtualView) =>
        {
            viewHandler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(androidPrimary);
            viewHandler.PlatformView.SetHintTextColor(Android.Content.Res.ColorStateList.ValueOf(androidHint));
        });

        var androidTransparent = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToAndroid());

        EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
        {
            h.PlatformView.BackgroundTintList = androidTransparent;
        });

#endif
    }
}




public enum Themes
{
    Default,
    Dracula,
    Seasalt,
    TeaRose,
    Gunmetal,
    Galaxy,
}

public class Theme(string safeName, Themes theme)
{
    public string safeName = safeName;
    public Themes theme = theme;

    public override string ToString()
    {
        return safeName;
    }

    public static Theme FromThemeType(Themes theme)
    {
        return new Theme(Enum.GetName(typeof(Themes), theme)!, theme);
    }
}

