using CommunityToolkit.Maui;
using Spindler.Services;
using Spindler.ViewModels;
using Spindler.Views;
using Spindler.Utilities;

#if ANDROID
using Spindler.Platforms.Android;
#endif

namespace Spindler;
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
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSans (Regular)");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSans (Semibold)");
                fonts.AddFont("DroidSans.ttf", "DroidSans");
                fonts.AddFont("DroidSans-Bold.ttf", "DroidSans (Bold)");
                fonts.AddFont("Merriweather-Regular.ttf", "Merriweather");
                fonts.AddFont("Merriweather-Bold.ttf", "Merriweather (Bold)");
                fonts.AddFont("SignikaNegative-Bold.ttf", "Signika (Bold)");
                fonts.AddFont("SignikaNegative-Regular.ttf", "Signika");
            })
        // Fix CollectionView Lag in Android
#if ANDROID
            .ConfigureMauiHandlers(h =>
            {
                h.AddHandler(typeof(CollectionView), typeof(CollectionViewHandlerEx));
            })
#endif
        ;
        builder.Services
            .AddShellRoutes()
            .AddSingleton<IDataService, DataService>()
            .AddSingleton<AppShell>();
        
        return builder.Build();
    }


    private static IServiceCollection AddShellRoutes(this IServiceCollection service)
    {
        // These cannot be defined with a Shell Route because they are already 
        // Present within the AppShell As Tab Pages
        service.AddSingleton<HomePage>();
        service.AddSingleton<HomeViewModel>();

        service.AddTransientWithShellRoute<BookListPage, BookListViewModel>($"{nameof(HomePage)}/{nameof(BookListPage)}");

        return service;
    }
}
