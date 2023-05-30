using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
using Spindler.Resources;
using Spindler.Views.Book_Pages;
using SQLitePCL;

namespace Spindler;

public partial class App : Application
{
    private static DataService? database;
    public static DataService Database
    {
        get
        {
            database ??= new DataService(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spindler.db"));
            return database;
        }
    }

    enum Themes
    {
        Default,
        Dracula
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
    }


    public void SetTheme()
    {
        Current?.Resources.MergedDictionaries.Clear();
        Themes? defaultTheme = (Themes)Preferences.Get("theme", (int)Themes.Default);
        ResourceDictionary theme = defaultTheme switch
        {
            Themes.Default => new Resources.Styles.Default(),
            Themes.Dracula => new Resources.Styles.Dracula(),
            _ => throw new NotImplementedException("HOW")
        };
        Current?.Resources.MergedDictionaries.Add(theme);
        Current?.Resources.MergedDictionaries.Add(new Resources.Setters());
    }
}
