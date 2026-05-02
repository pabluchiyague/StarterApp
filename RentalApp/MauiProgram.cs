using Microsoft.Extensions.Logging;
using RentalApp.ViewModels;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Views;
using RentalApp.Services;

namespace RentalApp;

public static class FeatureFlags
{
    /// <summary>
    /// This selects the live SET09102 API implementations so this app shares
    /// users, items, rentals, and reviews with everyone else using the
    /// coursework service. Set this to false only when deliberately testing
    /// the local PostgreSQL repositories.
    /// </summary>
    public static bool UseApi { get; set; } = true;
}

public static class MauiProgram
{
    /// <summary>
    /// This builds the MAUI application, registers all pages, services, and
    /// repositories, and chooses between live API repositories and local
    /// database repositories through <see cref="FeatureFlags.UseApi"/>.
    /// </summary>
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

        // ---- Database ----
        builder.Services.AddDbContext<AppDbContext>();

        // ---- Navigation ----
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddTransient<AuthorizationDelegatingHandler>();

        // ---- Shell and App ----
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        // ---- Auth pages and view-models ----
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();

        // ---- About ----
        builder.Services.AddTransient<AboutViewModel>();
        builder.Services.AddTransient<AboutPage>();

        // ---- Domain repositories ----
        if (FeatureFlags.UseApi)
        {
            var apiBase = new Uri("https://set09102-api.b-davison.workers.dev/");

            builder.Services.AddHttpClient("RentalApiAuth", client =>
                client.BaseAddress = apiBase)
                .AddHttpMessageHandler<AuthorizationDelegatingHandler>();

            builder.Services.AddSingleton<IAuthenticationService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var tokenStore = provider.GetRequiredService<ITokenStore>();
                return new ApiAuthenticationService(
                    httpClientFactory.CreateClient("RentalApiAuth"),
                    tokenStore);
            });

            builder.Services.AddHttpClient<IItemRepository, ApiItemRepository>(client =>
                client.BaseAddress = apiBase)
                .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
            builder.Services.AddHttpClient<IRentalRepository, ApiRentalRepository>(client =>
                client.BaseAddress = apiBase)
                .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
            builder.Services.AddHttpClient<IReviewRepository, ApiReviewRepository>(client =>
                client.BaseAddress = apiBase)
                .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
        }
        else
        {
            // AuthenticationService lives in RentalApp.Database/Services but
            // keeps the RentalApp.Services namespace so the MAUI app and tests
            // share the same interface.
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IItemRepository, LocalItemRepository>();
            builder.Services.AddScoped<IRentalRepository, LocalRentalRepository>();
            builder.Services.AddScoped<IReviewRepository, LocalReviewRepository>();
        }
        builder.Services.AddScoped<RentalApp.Database.Services.RentalService>();
        builder.Services.AddScoped<RentalApp.Database.Services.ReviewService>();

        // ---- Items ----
        builder.Services.AddTransient<ItemsListViewModel>();
        builder.Services.AddTransient<ItemsListPage>();
        builder.Services.AddTransient<NearbyItemsViewModel>();
        builder.Services.AddTransient<NearbyItemsPage>();
        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<ItemDetailPage>();
        builder.Services.AddTransient<CreateItemViewModel>();
        builder.Services.AddTransient<CreateItemPage>();
        builder.Services.AddTransient<IncomingRequestsViewModel>();
        builder.Services.AddTransient<IncomingRequestsPage>();
        builder.Services.AddTransient<OutgoingRentalsViewModel>();
        builder.Services.AddTransient<OutgoingRentalsPage>();
        builder.Services.AddTransient<LeaveReviewViewModel>();
        builder.Services.AddTransient<LeaveReviewPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
