using Microsoft.Extensions.Logging;
using RentalApp.ViewModels;
using RentalApp.Database.Data;
using RentalApp.Views;
using System.Diagnostics;
using RentalApp.Services;
using RentalApp.Database.Repositories;

namespace RentalApp;

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
        
        //Authentication
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        
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

        // ViewModels and Views for Authentication
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();

        // About page
        builder.Services.AddTransient<AboutViewModel>();
        builder.Services.AddTransient<AboutPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
