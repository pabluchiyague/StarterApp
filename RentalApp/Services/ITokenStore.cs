namespace RentalApp.Services;

public interface ITokenStore
{
    Task SaveAsync(string token, DateTime expiresAt, int userId);
    Task<string?> GetTokenAsync();
    Task<DateTime?> GetExpiresAtAsync();
    Task<int?> GetUserIdAsync();
    Task ClearAsync();
}
