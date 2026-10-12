using SuiviEntretien.Services;
using SuiviEntretien.Views;

namespace SuiviEntretien;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddSingleton<ApplicationStore>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}