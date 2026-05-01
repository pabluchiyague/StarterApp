using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public class ApiRentalRepository : IRentalRepository
{
    private readonly HttpClient _http;

    /// <summary>
    /// This stores the typed HTTP client that talks to the live rental API.
    /// </summary>
    public ApiRentalRepository(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// This creates a rental request through POST /rentals and maps the API's
    /// returned rental detail into the local domain model.
    /// </summary>
    public async Task<Rental> CreateAsync(Rental rental)
    {
        var response = await _http.PostAsJsonAsync(
            "rentals",
            new CreateRentalRequest(
                rental.ItemId,
                rental.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                rental.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ApiJson.Options);

        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<RentalDetailDto>(ApiJson.Options);
        return Map(dto!);
    }

    /// <summary>
    /// This loads one rental by id and treats API 404 responses as a null
    /// repository result.
    /// </summary>
    public async Task<Rental?> GetByIdAsync(int id)
    {
        var response = await _http.GetAsync($"rentals/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<RentalDetailDto>(ApiJson.Options);
        return dto == null ? null : Map(dto);
    }

    /// <summary>
    /// This loads rental requests for items owned by the signed-in user, with
    /// an optional status filter serialized in the API's string format.
    /// </summary>
    public async Task<List<Rental>> GetIncomingForOwnerAsync(int ownerId, RentalStatus? statusFilter = null)
    {
        var suffix = statusFilter == null ? string.Empty : $"?status={Uri.EscapeDataString(StatusText(statusFilter.Value))}";
        var response = await _http.GetAsync($"rentals/incoming{suffix}");
        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<RentalListResponse>(ApiJson.Options)
            ?? new RentalListResponse();
        return dto.Rentals.Select(Map).ToList();
    }

    /// <summary>
    /// This loads rental requests made by the signed-in user, with an optional
    /// status filter serialized in the API's string format.
    /// </summary>
    public async Task<List<Rental>> GetOutgoingForBorrowerAsync(int borrowerId, RentalStatus? statusFilter = null)
    {
        var suffix = statusFilter == null ? string.Empty : $"?status={Uri.EscapeDataString(StatusText(statusFilter.Value))}";
        var response = await _http.GetAsync($"rentals/outgoing{suffix}");
        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<RentalListResponse>(ApiJson.Options)
            ?? new RentalListResponse();
        return dto.Rentals.Select(Map).ToList();
    }

    /// <summary>
    /// This updates a rental status through PATCH /rentals/{id}/status and
    /// relies on the status JSON converter for values like "Out for Rent".
    /// </summary>
    public async Task<Rental?> UpdateStatusAsync(int rentalId, RentalStatus newStatus)
    {
        var response = await _http.PatchAsJsonAsync(
            $"rentals/{rentalId}/status",
            new UpdateRentalStatusRequest(newStatus),
            ApiJson.Options);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<RentalDetailDto>(ApiJson.Options);
        return dto == null ? null : Map(dto);
    }

    /// <summary>
    /// This returns false because the live API is authoritative for overlap
    /// checks and reports conflicts with HTTP 409 during create.
    /// </summary>
    public Task<bool> HasActiveOverlapAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        // The API is authoritative and returns 409 on overlapping create.
        return Task.FromResult(false);
    }

    /// <summary>
    /// This converts API rental list/detail DTOs into local domain rental
    /// objects with enough joined item/user data for display.
    /// </summary>
    private static Rental Map(RentalSummaryDto dto)
    {
        return new Rental
        {
            Id = dto.Id,
            ItemId = dto.ItemId,
            BorrowerId = dto.BorrowerId ?? 0,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            TotalPrice = dto.TotalPrice,
            CreatedAt = dto.RequestedAt,
            ApprovedAt = dto.ApprovedAt,
            Item = new Item
            {
                Id = dto.ItemId,
                Title = dto.ItemTitle,
                OwnerId = dto.OwnerId ?? 0,
                Owner = dto.OwnerId == null ? null : new User
                {
                    Id = dto.OwnerId.Value,
                    FirstName = dto.OwnerName ?? string.Empty,
                    LastName = string.Empty
                }
            },
            Borrower = dto.BorrowerId == null ? null : new User
            {
                Id = dto.BorrowerId.Value,
                FirstName = dto.BorrowerName ?? string.Empty,
                LastName = string.Empty
            }
        };
    }

    /// <summary>
    /// This converts local enum values into the API's status query string text.
    /// </summary>
    private static string StatusText(RentalStatus status)
    {
        return status == RentalStatus.OutForRent ? "Out for Rent" : status.ToString();
    }

    /// <summary>
    /// This converts failed API responses into the exception type that best
    /// matches validation, authorization, conflict, or server errors.
    /// </summary>
    private static async Task ThrowApiErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ErrorResponse? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiJson.Options);
        }
        catch
        {
        }

        var message = error?.Message ?? response.ReasonPhrase ?? "Request failed";
        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ArgumentException(message),
            HttpStatusCode.Unauthorized => new UnauthorizedAccessException(message),
            HttpStatusCode.Forbidden => new UnauthorizedAccessException(message),
            HttpStatusCode.Conflict => new InvalidOperationException(message),
            _ => new HttpRequestException($"{(int)response.StatusCode} {message}")
        };
    }
}
