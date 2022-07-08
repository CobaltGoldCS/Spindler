using Spindler.Services;
using SQLitePCL;

namespace Spindler;

public partial class App : Application
{

	static DataService database;
	public static DataService Database
    {
		get
        {
			if (database == null)
            {
				database = new DataService(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spindler.db"));
            }
			return database;
        }
    }
	public App(AppShell mainpage)
	{
		InitializeComponent();
		Batteries.Init();
		MainPage = mainpage;
		Routing.RegisterRoute(nameof(BookPage), typeof(BookPage));
		Routing.RegisterRoute($"{nameof(BookPage)}/{nameof(BookDetailPage)}", typeof(BookDetailPage));
		Routing.RegisterRoute($"{nameof(BookPage)}/{nameof(ReaderPage)}", typeof(ReaderPage));
        Routing.RegisterRoute("BookLists/" + nameof(BookListDetailPage), typeof(BookListDetailPage));
		Routing.RegisterRoute("Config/" + nameof(ConfigDetailPage), typeof(ConfigDetailPage));
	}
}
