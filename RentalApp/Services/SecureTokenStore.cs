using Microsoft.Maui.Storage;

namespace RentalApp.Services;

public class SecureTokenStore : ITokenStore
{
    private const string TokenKey = "jwt.token";
    private const string ExpiresAtKey = "jwt.expiresAt";
    private const string UserIdKey = "jwt.userId";

    /// <summary>
    /// This saves the JWT, expiry time, and user id in MAUI SecureStorage.
    /// </summary>
    public async Task SaveAsync(string token, DateTime expiresAt, int userId)
    {
        await SecureStorage.Default.SetAsync(TokenKey, token);
        await SecureStorage.Default.SetAsync(ExpiresAtKey, expiresAt.ToString("O"));
        await SecureStorage.Default.SetAsync(UserIdKey, userId.ToString());
    }

    /// <summary>
    /// This reads the saved JWT from SecureStorage.
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        return await SecureStorage.Default.GetAsync(TokenKey);
    }

    /// <summary>
    /// This reads and parses the saved token expiry time from SecureStorage.
    /// </summary>
    public async Task<DateTime?> GetExpiresAtAsync()
    {
        var value = await SecureStorage.Default.GetAsync(ExpiresAtKey);
        return DateTime.TryParse(value, out var expiresAt) ? expiresAt : null;
    }

    /// <summary>
    /// This reads and parses the saved authenticated user id from SecureStorage.
    /// </summary>
    public async Task<int?> GetUserIdAsync()
    {
        var value = await SecureStorage.Default.GetAsync(UserIdKey);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    /// <summary>
    /// This removes all saved authentication values from SecureStorage.
    /// </summary>
    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(ExpiresAtKey);
        SecureStorage.Default.Remove(UserIdKey);
        return Task.CompletedTask;
    }
}
