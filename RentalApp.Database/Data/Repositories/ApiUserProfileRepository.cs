using System.Net;
using System.Net.Http.Json;
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public class ApiUserProfileRepository : IUserProfileRepository
{
    private readonly HttpClient _http;

    public ApiUserProfileRepository(HttpClient http)
    {
        _http = http;
    }

    public async Task<UserProfile?> GetProfileAsync(int userId)
    {
        var response = await _http.GetAsync($"users/{userId}/profile");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowApiErrorAsync(response);

        var dto = await response.Content.ReadFromJsonAsync<UserProfileDto>(ApiJson.Options);
        return dto == null ? null : Map(dto);
    }

    private static UserProfile Map(UserProfileDto dto)
    {
        return new UserProfile
        {
            Id = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            AverageRating = dto.AverageRating,
            ItemsListed = dto.ItemsListed,
            RentalsCompleted = dto.RentalsCompleted,
            Reviews = dto.Reviews.Select(MapReview).ToList()
        };
    }

    private static Review MapReview(ReviewDto dto)
    {
        return new Review
        {
            Id = dto.Id,
            RentalId = dto.RentalId ?? 0,
            ReviewerId = dto.ReviewerId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = dto.CreatedAt,
            Reviewer = new User
            {
                Id = dto.ReviewerId,
                FirstName = dto.ReviewerName,
                LastName = string.Empty
            },
            Rental = string.IsNullOrWhiteSpace(dto.ItemTitle)
                ? null
                : new Rental
                {
                    Id = dto.RentalId ?? 0,
                    ItemId = dto.ItemId ?? 0,
                    Item = new Item
                    {
                        Id = dto.ItemId ?? 0,
                        Title = dto.ItemTitle
                    }
                }
        };
    }

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
            _ => new HttpRequestException($"{(int)response.StatusCode} {message}")
        };
    }
}
