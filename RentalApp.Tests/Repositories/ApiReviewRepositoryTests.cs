using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Models.Api;

namespace RentalApp.Tests;

public class ApiReviewRepositoryTests
{
    [Fact]
    public async Task CreateAsync_PostsReviewBodyToApi()
    {
        var handler = new FakeHttpMessageHandler(_ => Created(new ReviewDto
        {
            Id = 12,
            RentalId = 4,
            ReviewerId = 8,
            ReviewerName = "Review Borrower",
            Rating = 5,
            Comment = "Excellent",
            CreatedAt = DateTime.UtcNow
        }));
        var repository = CreateRepository(handler);

        var review = await repository.CreateAsync(new Review
        {
            RentalId = 4,
            ReviewerId = 8,
            Rating = 5,
            Comment = "Excellent"
        });

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://test/reviews", request.RequestUri!.ToString());

        var json = await request.Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(4, document.RootElement.GetProperty("rentalId").GetInt32());
        Assert.Equal(5, document.RootElement.GetProperty("rating").GetInt32());
        Assert.Equal("Excellent", document.RootElement.GetProperty("comment").GetString());
        Assert.Equal(12, review.Id);
    }

    [Fact]
    public async Task GetForItemAsync_MapsPagedReviews()
    {
        var handler = new FakeHttpMessageHandler(_ => Ok(new ReviewListResponse
        {
            Reviews =
            [
                new ReviewDto
                {
                    Id = 1,
                    RentalId = 2,
                    ReviewerId = 3,
                    ReviewerName = "Ada Reviewer",
                    Rating = 4,
                    Comment = "Good kit",
                    CreatedAt = DateTime.UtcNow
                }
            ],
            TotalReviews = 1,
            Page = 1,
            PageSize = 10,
            TotalPages = 1
        }));
        var repository = CreateRepository(handler);

        var result = await repository.GetForItemAsync(99);

        var request = handler.Requests.Single();
        Assert.Equal("https://test/items/99/reviews?page=1&pageSize=10", request.RequestUri!.ToString());
        var review = Assert.Single(result.Items);
        Assert.Equal("Ada Reviewer", review.Reviewer!.FullName);
        Assert.Equal(4, review.Rating);
        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public async Task CreateAsync_On409_ThrowsInvalidOperationException()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(
                new ErrorResponse { Error = "Conflict", Message = "You have already reviewed this rental" },
                options: ApiJson.Options)
        });
        var repository = CreateRepository(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateAsync(new Review
        {
            RentalId = 4,
            Rating = 5
        }));

        Assert.Contains("already reviewed", ex.Message);
    }

    private static ApiReviewRepository CreateRepository(FakeHttpMessageHandler handler)
    {
        return new ApiReviewRepository(new HttpClient(handler)
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

    private static HttpResponseMessage Created<T>(T body)
    {
        return new HttpResponseMessage(HttpStatusCode.Created)
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
