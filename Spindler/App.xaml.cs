using Spindler.Services;
using Spindler.Utilities;
using Spindler.Views;
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
    private static Shared? shared;
    public static Shared SharedValues
    {
        get
        {
            shared ??= new Shared();
            return shared;
        }
    }
    public App(AppShell shell)
    {
        InitializeComponent();
        Batteries.Init();
        MainPage = shell;
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookDetailPage)}", typeof(BookDetailPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookDetailPage)}/{nameof(BookSearcherPage)}", typeof(BookSearcherPage));

        //Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookListPage)}", typeof(BookListPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookPage)}", typeof(BookPage));

        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(StandardReaderPage)}", typeof(StandardReaderPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(WebviewReaderPage)}", typeof(WebviewReaderPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(HeadlessReaderPage)}", typeof(HeadlessReaderPage));
        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(ErrorPage)}", typeof(ErrorPage));

        Routing.RegisterRoute($"{nameof(HomePage)}/{nameof(BookListDetailPage)}", typeof(BookListDetailPage));
        Routing.RegisterRoute("Config/" + nameof(ConfigDetailPage), typeof(ConfigDetailPage));
        Routing.RegisterRoute("GeneralConfig/" + nameof(GeneralizedConfigDetailPage), typeof(GeneralizedConfigDetailPage));
    }
}
