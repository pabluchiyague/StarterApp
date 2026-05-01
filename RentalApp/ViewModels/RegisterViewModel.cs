/// @file RegisterViewModel.cs
/// @brief User registration view model
/// @author RentalApp Development Team
/// @date 2025

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Services;
using System.Text.RegularExpressions;

namespace RentalApp.ViewModels;

/// @brief View model for the user registration page
/// @details Manages user registration form data, validation, and registration process
/// @extends BaseViewModel
public partial class RegisterViewModel : BaseViewModel
{
    /// @brief Authentication service for managing user registration
    private readonly IAuthenticationService _authService;
    
    /// @brief Navigation service for managing page navigation
    private readonly INavigationService _navigationService;

    /// @brief The user's first name
    /// @details Observable property bound to the first name input field
    [ObservableProperty]
    private string firstName = string.Empty;

    /// @brief The user's last name
    /// @details Observable property bound to the last name input field
    [ObservableProperty]
    private string lastName = string.Empty;

    /// @brief The user's email address
    /// @details Observable property bound to the email input field
    [ObservableProperty]
    private string email = string.Empty;

    /// @brief The user's password
    /// @details Observable property bound to the password input field
    [ObservableProperty]
    private string password = string.Empty;

    /// @brief Confirmation of the user's password
    /// @details Observable property bound to the confirm password input field
    [ObservableProperty]
    private string confirmPassword = string.Empty;

    /// @brief Whether the user accepts the terms and conditions
    /// @details Observable property bound to the terms acceptance checkbox
    [ObservableProperty]
    private bool acceptTerms;

    /// @brief Default constructor for design-time support
    /// @details Sets the title to "Register"
    public RegisterViewModel()
    {
        // Default constructor for design time support
        Title = "Register";
    }

    /// <summary>
    /// This stores the authentication and navigation services used by the
    /// registration commands and sets the page title.
    /// </summary>
    public RegisterViewModel(IAuthenticationService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Register";
    }

    /// <summary>
    /// This validates the form, sends the registration request to the active
    /// authentication service, and returns to login when the API accepts it.
    /// </summary>
    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy)
            return;

        if (!ValidateForm())
            return;

        try
        {
            IsBusy = true;
            ClearError();

            var result = await _authService.RegisterAsync(FirstName, LastName, Email, Password);

            if (result.IsSuccess)
            {
                await Application.Current.MainPage.DisplayAlert("Success", "Registration successful! Please login.", "OK");
                await _navigationService.NavigateBackAsync();
            }
            else
            {
                SetError(result.Message);
            }
        }
        catch (Exception ex)
        {
            SetError($"Registration failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// This navigates back to the login page without creating an account.
    /// </summary>
    [RelayCommand]
    private async Task NavigateBackToLoginAsync()
    {
        await _navigationService.NavigateBackAsync();
    }

    /// <summary>
    /// This checks every registration field before the API call and stores the
    /// first validation message that the user needs to fix.
    /// </summary>
    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            SetError("First name is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            SetError("Last name is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            SetError("Email is required");
            return false;
        }

        if (!IsValidEmail(Email))
        {
            SetError("Please enter a valid email address");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            SetError("Password is required");
            return false;
        }

        if (Password.Length < 8)
        {
            SetError("Password must be at least 8 characters long");
            return false;
        }

        if (!Password.Any(char.IsUpper))
        {
            SetError("Password must contain an uppercase letter");
            return false;
        }

        if (!Password.Any(char.IsLower))
        {
            SetError("Password must contain a lowercase letter");
            return false;
        }

        if (!Password.Any(char.IsDigit))
        {
            SetError("Password must contain a number");
            return false;
        }

        if (Password != ConfirmPassword)
        {
            SetError("Passwords do not match");
            return false;
        }

        if (!AcceptTerms)
        {
            SetError("Please accept the terms and conditions");
            return false;
        }

        return true;
    }

    /// <summary>
    /// This checks whether an email address has a basic address-like format.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
    }
}
