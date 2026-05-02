using System.Net;
using System.Net.Http.Json;
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Services;

public class ApiAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _http;
    private readonly ITokenStore _tokenStore;
    private User? _currentUser;

    /// <summary>
    /// This stores the live API client and token store used for login,
    /// registration, logout, and current-user lookup.
    /// </summary>
    public ApiAuthenticationService(HttpClient http, ITokenStore tokenStore)
    {
        _http = http;
        _tokenStore = tokenStore;
    }

    public event EventHandler<bool>? AuthenticationStateChanged;

    public bool IsAuthenticated => _currentUser != null;

    public User? CurrentUser => _currentUser;

    public List<string> CurrentUserRoles { get; } = new();

    /// <summary>
    /// This posts credentials to /auth/token, stores the returned JWT, loads
    /// /users/me, and marks the app as authenticated.
    /// </summary>
    public async Task<AuthenticationResult> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("auth/token", new LoginRequest(email, password), ApiJson.Options);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new AuthenticationResult(false, "Invalid email or password");
        }

        if (!response.IsSuccessStatusCode)
        {
            return new AuthenticationResult(false, await ErrorMessageAsync(response));
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(ApiJson.Options);
        if (login == null || string.IsNullOrWhiteSpace(login.Token))
        {
            return new AuthenticationResult(false, "Login response did not include a token.");
        }

        await _tokenStore.SaveAsync(login.Token, login.ExpiresAt, login.UserId);
        await LoadCurrentUserAsync(login.UserId);
        AuthenticationStateChanged?.Invoke(this, true);
        return new AuthenticationResult(true, "Login successful");
    }

    /// <summary>
    /// This posts a new account request to /auth/register and returns the API's
    /// success or validation message to the registration view-model.
    /// </summary>
    public async Task<AuthenticationResult> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        var response = await _http.PostAsJsonAsync(
            "auth/register",
            new RegisterRequest(firstName, lastName, email, password),
            ApiJson.Options);

        if (!response.IsSuccessStatusCode)
        {
            return new AuthenticationResult(false, await ErrorMessageAsync(response));
        }

        return new AuthenticationResult(true, "Registration successful");
    }

    /// <summary>
    /// This clears the current user and saved JWT so future API calls are made
    /// as an anonymous user until login happens again.
    /// </summary>
    public async Task LogoutAsync()
    {
        _currentUser = null;
        CurrentUserRoles.Clear();
        await _tokenStore.ClearAsync();
        AuthenticationStateChanged?.Invoke(this, false);
    }

    /// <summary>
    /// This checks whether the current API-authenticated user has one named
    /// role in the local in-memory role list.
    /// </summary>
    public bool HasRole(string roleName) =>
        CurrentUserRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// This checks whether the current user has at least one of the supplied
    /// role names.
    /// </summary>
    public bool HasAnyRole(params string[] roleNames) => roleNames.Any(HasRole);

    /// <summary>
    /// This checks whether the current user has every supplied role name.
    /// </summary>
    public bool HasAllRoles(params string[] roleNames) => roleNames.All(HasRole);

    /// <summary>
    /// This returns false because the live coursework API reference does not
    /// expose a change-password endpoint.
    /// </summary>
    public Task<bool> ChangePasswordAsync(string currentPassword, string newPassword) =>
        Task.FromResult(false);

    /// <summary>
    /// This loads the authenticated user profile from /users/me and falls back
    /// to a minimal user object if the profile request fails.
    /// </summary>
    private async Task LoadCurrentUserAsync(int fallbackUserId)
    {
        var response = await _http.GetAsync("users/me");
        if (!response.IsSuccessStatusCode)
        {
            _currentUser = new User { Id = fallbackUserId };
            return;
        }

        var dto = await response.Content.ReadFromJsonAsync<UserDto>(ApiJson.Options);
        _currentUser = dto == null
            ? new User { Id = fallbackUserId }
            : new User
            {
                Id = dto.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                AverageRating = dto.AverageRating,
                ItemsListed = dto.ItemsListed ?? 0,
                RentalsCompleted = dto.RentalsCompleted ?? 0,
                IsActive = true,
                CreatedAt = dto.CreatedAt
            };
    }

    /// <summary>
    /// This extracts the standard API error message body and falls back to the
    /// HTTP reason phrase when the response body is empty or unexpected.
    /// </summary>
    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiJson.Options);
            if (error?.Errors.Count > 0)
            {
                return string.Join(Environment.NewLine, error.Errors.Select(e =>
                {
                    var field = e.Path.LastOrDefault();
                    return string.IsNullOrWhiteSpace(field)
                        ? e.Message
                        : $"{field}: {e.Message}";
                }));
            }

            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }

            return response.ReasonPhrase ?? "Request failed";
        }
        catch
        {
            return response.ReasonPhrase ?? "Request failed";
        }
    }
}
