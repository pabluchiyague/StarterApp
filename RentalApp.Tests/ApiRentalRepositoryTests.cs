using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Models.Api;

namespace RentalApp.Tests;

public class ApiRentalRepositoryTests
{
    [Fact]
    public async Task CreateAsync_PostsDateOnlyRentalRequestToApi()
    {
        var handler = new FakeHttpMessageHandler(_ => Created(new RentalDetailDto
        {
            Id = 42,
            ItemId = 7,
            ItemTitle = "Cordless drill",
            BorrowerId = 11,
            BorrowerName = "Borrower",
            OwnerId = 12,
            OwnerName = "Owner",
            StartDate = new DateTime(2026, 6, 10),
            EndDate = new DateTime(2026, 6, 12),
            Status = RentalStatus.Requested,
            TotalPrice = 15m,
            RequestedAt = DateTime.UtcNow
        }));
        var repository = CreateRepository(handler);

        var rental = await repository.CreateAsync(new Rental
        {
            ItemId = 7,
            BorrowerId = 11,
            StartDate = new DateTime(2026, 6, 10, 14, 30, 0),
            EndDate = new DateTime(2026, 6, 12, 9, 0, 0)
        });

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://test/rentals", request.RequestUri!.ToString());

        var json = await request.Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(7, document.RootElement.GetProperty("itemId").GetInt32());
        Assert.Equal("2026-06-10", document.RootElement.GetProperty("startDate").GetString());
        Assert.Equal("2026-06-12", document.RootElement.GetProperty("endDate").GetString());
        Assert.Equal(RentalStatus.Requested, rental.Status);
        Assert.Equal(15m, rental.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_On409_ThrowsInvalidOperationException()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(
                new ErrorResponse { Error = "Conflict", Message = "This item already has an approved rental for these dates" },
                options: ApiJson.Options)
        });
        var repository = CreateRepository(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateAsync(new Rental
        {
            ItemId = 7,
            StartDate = new DateTime(2026, 6, 10),
            EndDate = new DateTime(2026, 6, 12)
        }));

        Assert.Contains("approved rental", ex.Message);
    }

    private static ApiRentalRepository CreateRepository(FakeHttpMessageHandler handler)
    {
        return new ApiRentalRepository(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test/")
        });
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
