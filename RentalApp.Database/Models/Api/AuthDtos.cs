namespace RentalApp.Models.Api;

/// <summary>POST /auth/register request body.</summary>
public record RegisterRequest(string FirstName, string LastName, string Email, string Password);

/// <summary>POST /auth/token request body.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>POST /auth/token success response (200 OK).</summary>
public class LoginResponse
{
    public string   Token     { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int      UserId    { get; set; }
}

/// <summary>
/// User payload returned by GET /users/me and the public
/// GET /users/{id}/profile. Optional fields are only present on /me.
/// </summary>
public class UserDto
{
    public int       Id                { get; set; }
    public string    Email             { get; set; } = string.Empty;
    public string    FirstName         { get; set; } = string.Empty;
    public string    LastName          { get; set; } = string.Empty;
    public double?   AverageRating     { get; set; }
    public int?      ItemsListed       { get; set; }
    public int?      RentalsCompleted  { get; set; }
    public DateTime? CreatedAt         { get; set; }
}
