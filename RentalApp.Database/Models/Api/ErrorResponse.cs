namespace RentalApp.Models.Api;

/// <summary>
/// Common error envelope returned by every non-2xx API response.
/// Example: <c>{ "error": "Validation failed", "message": "Email already exists" }</c>.
/// </summary>
public class ErrorResponse
{
    public string Error   { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ApiValidationError> Errors { get; set; } = new();
}

/// <summary>
/// This represents one validation error from API responses that return an
/// errors array instead of the simpler error/message envelope.
/// </summary>
public class ApiValidationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Path { get; set; } = new();
}
