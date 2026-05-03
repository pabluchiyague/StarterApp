using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Models.Api;

namespace RentalApp.Tests;

public class ApiItemRepositoryTests
{
    public class GetItemsAsyncTests
    {
    [Fact]
    public async Task GetItemsAsync_AllFilters_BuildsEscapedQueryString()
    {
        var handler = new FakeHttpMessageHandler(_ => Ok(new PagedResponse<ItemSummaryDto>()));
        var repository = CreateRepository(handler);

        await repository.GetItemsAsync("power tools", "cordless drill", 2, 25);

        var uri = handler.Requests.Single().RequestUri!.AbsoluteUri;
        Assert.Contains("category=power%20tools", uri);
        Assert.Contains("search=cordless%20drill", uri);
        Assert.Contains("page=2", uri);
        Assert.Contains("pageSize=25", uri);
    }

    [Fact]
    public async Task GetItemsAsync_OnSuccess_MapsDtosToDomainObjects()
    {
        var response = new PagedResponse<ItemSummaryDto>
        {
            Items =
            [
                new ItemSummaryDto
                {
                    Id = 10,
                    Title = "Projector",
                    Description = "HD projector",
                    DailyRate = 9.99m,
                    CategoryId = 4,
                    Category = "electronics",
                    OwnerId = 99,
                    OwnerName = "Ada Lovelace",
                    IsAvailable = true,
                    AverageRating = 4.5,
                    CreatedAt = DateTime.UtcNow
                }
            ],
            TotalItems = 1,
            Page = 1,
            PageSize = 20,
            TotalPages = 1
        };

        var repository = CreateRepository(new FakeHttpMessageHandler(_ => Ok(response)));

        var result = await repository.GetItemsAsync();

        var item = Assert.Single(result.Items);
        Assert.Equal("Projector", item.Title);
        Assert.Equal("electronics", item.Category!.Slug);
        Assert.Equal(99, item.OwnerId);
        Assert.Equal(4.5, item.AverageRating);
    }
    }

    public class GetItemByIdAsyncTests
    {
    [Fact]
    public async Task GetItemByIdAsync_NotFoundResponse_ReturnsNull()
    {
        var repository = CreateRepository(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await repository.GetItemByIdAsync(123);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetItemByIdAsync_On500_ThrowsHttpRequestException()
    {
        var repository = CreateRepository(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        await Assert.ThrowsAsync<HttpRequestException>(() => repository.GetItemByIdAsync(123));
    }
    }

    public class CreateItemAsyncTests
    {
    [Fact]
    public async Task CreateItemAsync_ValidItem_PostsRightBodyToItemsEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ => Created(new ItemDetailDto
        {
            Id = 4,
            Title = "Camera",
            DailyRate = 8m,
            CategoryId = 4,
            Category = "electronics",
            OwnerId = 2,
            OwnerName = "Owner",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        }));
        var repository = CreateRepository(handler);

        await repository.CreateItemAsync(new Item
        {
            Title = "Camera",
            Description = "DSLR",
            DailyRate = 8m,
            CategoryId = 4,
            Latitude = 55.9533,
            Longitude = -3.1883
        });

        var request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://test/items", request.RequestUri!.ToString());

        var json = await request.Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal("Camera", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(4, document.RootElement.GetProperty("categoryId").GetInt32());
        Assert.Equal(55.9533, document.RootElement.GetProperty("latitude").GetDouble(), 4);
        Assert.Equal(-3.1883, document.RootElement.GetProperty("longitude").GetDouble(), 4);
    }

    [Fact]
    public async Task CreateItemAsync_BadRequest_ThrowsArgumentExceptionWithApiMessage()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new ErrorResponse { Error = "Validation", Message = "Title is required" }, options: ApiJson.Options)
        });
        var repository = CreateRepository(handler);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => repository.CreateItemAsync(new Item()));
        Assert.Contains("Title is required", ex.Message);
    }
    }

    public class UpdateItemAsyncTests
    {
    [Fact]
    public async Task UpdateItemAsync_ForbiddenResponse_ThrowsUnauthorizedAccessException()
    {
        var repository = CreateRepository(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            repository.UpdateItemAsync(1, new UpdateItemRequest { Title = "New" }));
    }
    }

    public class GetNearbyAsyncTests
    {
    [Fact]
    public async Task GetNearbyAsync_ValidSearch_BuildsNearbyQueryAndMapsDistance()
    {
        var handler = new FakeHttpMessageHandler(_ => Ok(new NearbyResponse
        {
            Items =
            [
                new ItemSummaryDto
                {
                    Id = 42,
                    Title = "Nearby drill",
                    DailyRate = 4m,
                    CategoryId = 1,
                    Category = "tools",
                    OwnerId = 7,
                    OwnerName = "Owner",
                    IsAvailable = true,
                    Latitude = 55.954,
                    Longitude = -3.19,
                    Distance = 1.2,
                    CreatedAt = DateTime.UtcNow
                }
            ],
            SearchLocation = new NearbyOrigin { Latitude = 55.9533, Longitude = -3.1883 },
            Radius = 5,
            TotalResults = 1
        }));
        var repository = CreateRepository(handler);

        var result = await repository.GetNearbyAsync(55.9533, -3.1883, 5, "tools");

        var requestUri = handler.Requests.Single().RequestUri!.ToString();
        Assert.Contains("items/nearby", requestUri);
        Assert.Contains("lat=55.9533", requestUri);
        Assert.Contains("lon=-3.1883", requestUri);
        Assert.Contains("radius=5", requestUri);
        Assert.Contains("category=tools", requestUri);

        var item = Assert.Single(result.Items);
        Assert.Equal(1.2, item.DistanceKm);
        Assert.Equal(1, result.TotalResults);
    }
    }

    public class RentalStatusJsonConverterTests
    {
    [Fact]
    public void RentalStatusJsonConverter_OutForRent_RoundTripsDisplayName()
    {
        var json = JsonSerializer.Serialize(RentalStatus.OutForRent, ApiJson.Options);
        var status = JsonSerializer.Deserialize<RentalStatus>(json, ApiJson.Options);

        Assert.Equal("\"Out for Rent\"", json);
        Assert.Equal(RentalStatus.OutForRent, status);
    }
    }

    private static ApiItemRepository CreateRepository(FakeHttpMessageHandler handler)
    {
        return new ApiItemRepository(new HttpClient(handler)
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
