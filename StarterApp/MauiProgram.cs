using Microsoft.Extensions.Logging;
using StarterApp.ViewModels;
using StarterApp.Database.Data;
using StarterApp.Views;
using System.Diagnostics;
using StarterApp.Services;
using StarterApp.Database.Repositories;

namespace StarterApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Database
        builder.Services.AddDbContext<AppDbContext>();
        
        // Repository 
        builder.Services.AddScoped<INoteRepository, NoteRepository>();

        // Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // Shell and App
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        // ViewModels and Views for Notes
        builder.Services.AddTransient<NotesViewModel>();
        builder.Services.AddTransient<NotesPage>();
        builder.Services.AddTransient<NoteViewModel>();
        builder.Services.AddTransient<NotePage>();

        // About page
        builder.Services.AddTransient<AboutPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
