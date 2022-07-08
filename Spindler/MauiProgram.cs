namespace Spindler;

using CommunityToolkit.Maui;
using Spindler.Services;
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		builder.Services
			.AddSingleton<AppShell>()
			.AddSingleton<ReaderPage>()
			.AddSingleton<BookListPage>()
			.AddTransient<WebService>();
        return builder.Build();
	}
}
