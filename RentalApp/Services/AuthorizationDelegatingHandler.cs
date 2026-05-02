using System.Net;
using System.Net.Http.Headers;

namespace RentalApp.Services;

public class AuthorizationDelegatingHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;

    /// <summary>
    /// This stores the token store used to find and clear JWT credentials.
    /// </summary>
    public AuthorizationDelegatingHandler(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    /// <summary>
    /// This adds the saved bearer token to outgoing API calls when the token is
    /// present and not expired, then clears it if the API returns 401.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.GetTokenAsync();
        var expiresAt = await _tokenStore.GetExpiresAtAsync();

        if (expiresAt != null && expiresAt <= DateTime.UtcNow)
        {
            await _tokenStore.ClearAsync();
            token = null;
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _tokenStore.ClearAsync();
        }

        return response;
    }
}
