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
                fonts.AddFont("DroidSans.ttf", "DroidSans");
                fonts.AddFont("DroidSans-Bold.ttf", "DroidSansBold");
                fonts.AddFont("Merriweather-Regular.ttf", "Merriweather");
                fonts.AddFont("Merriweather-Bold.ttf", "MerriweatherBold");
                fonts.AddFont("SignikaNegative-Bold.ttf", "SignikaBold");
                fonts.AddFont("SignikaNegative-Regular.ttf", "Signika");
            });
        builder.Services
            .AddSingleton<AppShell>()
            .AddSingleton<WebService>()
            ;
        return builder.Build();
    }
}
