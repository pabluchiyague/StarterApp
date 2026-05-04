using RentalApp.Views;
using RentalApp.Services;

namespace RentalApp;

public partial class AppShell : Shell
{
    private readonly IAuthenticationService _authService;

    public AppShell(IAuthenticationService authService)
    {
        _authService = authService;
        InitializeComponent();

        // Routes for pages that aren't direct children of the Shell.
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
        Routing.RegisterRoute(nameof(CreateItemPage), typeof(CreateItemPage));
        Routing.RegisterRoute(nameof(EditItemPage), typeof(EditItemPage));
        Routing.RegisterRoute(nameof(LeaveReviewPage), typeof(LeaveReviewPage));
        Routing.RegisterRoute(nameof(UserProfilePage), typeof(UserProfilePage));
    }

    /// <summary>
    /// This signs the current user out, clears the saved API token, closes the
    /// flyout, and returns the app to the login route.
    /// </summary>
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await _authService.LogoutAsync();
        FlyoutIsPresented = false;
        await GoToAsync("//login");
    }
}
