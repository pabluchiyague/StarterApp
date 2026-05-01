using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Services;
using RentalApp.Views;

namespace RentalApp.ViewModels;
/// @brief View model for the login page that handles user authentication
/// @details Manages login form data, validation, and authentication process
/// @extends BaseViewModel
public partial class LoginViewModel : BaseViewModel
{
    /// @brief Authentication service for managing user login
    private readonly IAuthenticationService _authService;
    
    /// @brief Navigation service for managing page navigation
    private readonly INavigationService _navigationService;

    /// @brief The user's email address
    /// @details Observable property bound to the email input field
    [ObservableProperty]
    private string email = string.Empty;

    /// @brief The user's password
    /// @details Observable property bound to the password input field
    [ObservableProperty]
    private string password = string.Empty;

    /// @brief Whether to remember the user's login credentials
    /// @details Observable property bound to the remember me checkbox
    [ObservableProperty]
    private bool rememberMe;

    /// @brief Indicates whether a login operation is in progress
    /// @details Observable property that notifies the LoginCommand when changed
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    /// @brief Default constructor for design-time support
    /// @details Sets the title to "Login"
    public LoginViewModel()
    {
        // Default constructor for design time support
        Title = "Login";
    }

    /// <summary>
    /// This stores the authentication and navigation services used by the login
    /// commands and sets the page title.
    /// </summary>
    public LoginViewModel(IAuthenticationService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Login";
    }

    /// <summary>
    /// This validates the entered credentials, signs in through the active
    /// authentication service, and navigates to the shared API item browser.
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Please enter both email and password");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var result = await _authService.LoginAsync(Email, Password);

            if (result.IsSuccess)
            {
                await Shell.Current.GoToAsync("//items");
            }
            else
            {
                SetError(result.Message);
            }
        }
        catch (Exception ex)
        {
            SetError($"Login failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// This navigates to the registration page so the user can create a live
    /// API account.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        // nameof(RegisterPage) matches the route registered in AppShell.cs
        await _navigationService.NavigateToAsync(nameof(RegisterPage));
    }

    /// <summary>
    /// This displays the current placeholder message because the coursework API
    /// does not define a forgot-password endpoint.
    /// </summary>
    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        // TODO: Implement forgot password functionality
        await Application.Current.MainPage.DisplayAlert("Info", "Forgot password functionality not implemented yet", "OK");
    }
}
