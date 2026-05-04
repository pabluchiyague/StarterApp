using System.Net;
using System.Net.Http.Json;
using RentalApp.Database.Repositories;
using RentalApp.Models.Api;

namespace RentalApp.Tests;

public class ApiUserProfileRepositoryTests
{
    [Fact]
    public async Task GetProfileAsync_OnSuccess_MapsStatsAndReceivedReviews()
    {
        var createdAt = DateTime.UtcNow;
        var handler = new FakeHttpMessageHandler(_ => Ok(new UserProfileDto
        {
            Id = 456,
            FirstName = "Sarah",
            LastName = "Smith",
            AverageRating = 4.8,
            ItemsListed = 8,
            RentalsCompleted = 24,
            Reviews =
            [
                new ReviewDto
                {
                    Id = 101,
                    RentalId = 22,
                    ItemId = 33,
                    ItemTitle = "Cordless drill",
                    ReviewerId = 77,
                    ReviewerName = "Mike Johnson",
                    Rating = 5,
                    Comment = "Great item, very helpful owner!",
                    CreatedAt = createdAt
                }
            ]
        }));
        var repository = CreateRepository(handler);

        var profile = await repository.GetProfileAsync(456);

        Assert.NotNull(profile);
        Assert.Equal("Sarah", profile!.FirstName);
        Assert.Equal("Smith", profile.LastName);
        Assert.Equal(4.8, profile.AverageRating);
        Assert.Equal(8, profile.ItemsListed);
        Assert.Equal(24, profile.RentalsCompleted);

        var review = Assert.Single(profile.Reviews);
        Assert.Equal(101, review.Id);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Mike Johnson", review.Reviewer!.FullName);
        Assert.Equal("Cordless drill", review.Rental!.Item!.Title);
        Assert.Equal(createdAt, review.CreatedAt);
        Assert.Equal("https://test/users/456/profile", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task GetProfileAsync_NotFoundResponse_ReturnsNull()
    {
        var repository = CreateRepository(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var profile = await repository.GetProfileAsync(999);

        Assert.Null(profile);
    }

    [Fact]
    public async Task GetProfileAsync_ForbiddenResponse_ThrowsUnauthorizedAccessException()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = JsonContent.Create(
                new ErrorResponse { Error = "Forbidden", Message = "Access denied" },
                options: ApiJson.Options)
        });
        var repository = CreateRepository(handler);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => repository.GetProfileAsync(456));

        Assert.Contains("Access denied", ex.Message);
    }

    private static ApiUserProfileRepository CreateRepository(FakeHttpMessageHandler handler)
    {
        return new ApiUserProfileRepository(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test/")
        });
    }

    private static HttpResponseMessage Ok<T>(T body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(body, options: ApiJson.Options)
        };
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responder(request));
        }
    }
}
