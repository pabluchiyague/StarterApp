using System.Net;
using System.Net.Http.Json;
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public class ApiReviewRepository : IReviewRepository
{
    private readonly HttpClient _http;

    /// <summary>
    /// This stores the typed HTTP client used to call the live review API.
    /// </summary>
    public ApiReviewRepository(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// This posts a new review to the live API and maps the returned DTO back
    /// into the local domain review model.
    /// </summary>
    public async Task<Review> CreateAsync(Review review)
    {
        var response = await _http.PostAsJsonAsync(
            "reviews",
            new CreateReviewRequest(review.RentalId, review.Rating, review.Comment),
            ApiJson.Options);

        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<ReviewDto>(ApiJson.Options);
        return Map(dto!);
    }

    /// <summary>
    /// This loads paginated reviews for one item from the live API.
    /// </summary>
    public async Task<PagedResult<Review>> GetForItemAsync(int itemId, int page = 1, int pageSize = 10)
    {
        var response = await _http.GetAsync($"items/{itemId}/reviews?page={page}&pageSize={pageSize}");
        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<ReviewListResponse>(ApiJson.Options)
            ?? new ReviewListResponse();

        return new PagedResult<Review>(
            dto.Reviews.Select(Map).ToList(),
            dto.TotalReviews,
            dto.Page,
            dto.PageSize,
            dto.TotalPages);
    }

    /// <summary>
    /// This loads paginated reviews written by or associated with one user
    /// from the live API.
    /// </summary>
    public async Task<PagedResult<Review>> GetForUserAsync(int userId, int page = 1, int pageSize = 10)
    {
        var response = await _http.GetAsync($"users/{userId}/reviews?page={page}&pageSize={pageSize}");
        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<ReviewListResponse>(ApiJson.Options)
            ?? new ReviewListResponse();

        return new PagedResult<Review>(
            dto.Reviews.Select(Map).ToList(),
            dto.TotalReviews,
            dto.Page,
            dto.PageSize,
            dto.TotalPages);
    }

    /// <summary>
    /// This converts an API review DTO into the local Review entity and keeps
    /// reviewer display information available for UI binding.
    /// </summary>
    private static Review Map(ReviewDto dto)
    {
        return new Review
        {
            Id = dto.Id,
            RentalId = dto.RentalId ?? 0,
            ReviewerId = dto.ReviewerId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = dto.CreatedAt,
            Rental = new Rental
            {
                Id = dto.RentalId ?? 0,
                ItemId = dto.ItemId ?? 0,
                Item = string.IsNullOrWhiteSpace(dto.ItemTitle)
                    ? null
                    : new Item
                    {
                        Id = dto.ItemId ?? 0,
                        Title = dto.ItemTitle
                    }
            },
            Reviewer = new User
            {
                Id = dto.ReviewerId,
                FirstName = dto.ReviewerName,
                LastName = string.Empty
            }
        };
    }

    /// <summary>
    /// This converts failed review API responses into validation,
    /// authorization, conflict, or HTTP exceptions.
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
