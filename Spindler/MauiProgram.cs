using CommunityToolkit.Maui;
using Spindler.Services;
using Spindler.ViewModels;
using Spindler.Views;
using Spindler.Views.Book_Pages;

#if ANDROID
using Microsoft.Maui.Handlers;
using Spindler.Platforms.Android;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
#endif

namespace Spindler;
public static class MauiProgram
{
    public static IMauiContext? mauiContext;
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
                fonts.AddFont("spindler_icons.ttf", "SPIcon");
                fonts.AddFont("GreatVibes-Regular.ttf", "Great Vibes");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                // THIS IS A TEMPORARY WORKAROUND TO FIX 'Collectionview not defined' ERRORS
                handlers.AddHandler<CollectionView, CustomCollectionViewHandler>();
                handlers.AddHandler<Shell, ShellHandler>();
#endif
            })
        ;


        builder.Services
            .AddPages()
            .AddSingleton<IDataService, DataService>()
            .AddHttpClient();

        var mauiapp = builder.Build();

        mauiContext = new MauiContext(mauiapp.Services);
        return mauiapp;
    }


    private static IServiceCollection AddPages(this IServiceCollection service)
    {
        // These cannot be defined with a Shell Route because they are already 
        // Present within the AppShell As Tab Pages
        service.AddSingleton<HomePage>();
        service.AddSingleton<HomeViewModel>();

        service.AddTransient<BookListDetailPage>();
        service.AddTransient<ConfigPage>();
        service.AddTransient<ConfigViewModel>();

        service.AddTransient<ConfigDetailPage>();
        service.AddTransient<ConfigDetailViewModel>();
        service.AddTransient<BookPage>();
        service.AddTransient<BookDetailPage>();
        service.AddTransient<BookSearcherPage>();
        service.AddTransient<WebviewReaderPage>();
        service.AddTransient<ErrorPage>();
        service.AddTransient<ReaderPage>();

        service.AddTransientWithShellRoute<BookListPage, BookListViewModel>($"{nameof(HomePage)}/{nameof(BookListPage)}");

        return service;
    }

    private static void AddHttpClient(this IServiceCollection service)
    {
        service.AddSingleton(new HttpClient());
    }
}
