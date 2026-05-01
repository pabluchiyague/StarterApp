using System.Net;
using System.Net.Http.Json;
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public class ApiItemRepository : IItemRepository
{
    private readonly HttpClient _http;

    /// <summary>
    /// This stores the typed HTTP client configured in DI with the coursework
    /// API base address and bearer-token handler.
    /// </summary>
    public ApiItemRepository(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// This requests a paginated item list from the live API, applies optional
    /// category/search filters as query-string values, and maps API DTOs into
    /// local domain <see cref="Item"/> objects for the view-models.
    /// </summary>
    public async Task<PagedResult<Item>> GetItemsAsync(
        string? categorySlug = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query.Add($"category={Uri.EscapeDataString(categorySlug)}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        query.Add($"page={page}");
        query.Add($"pageSize={pageSize}");

        var response = await _http.GetAsync($"items?{string.Join("&", query)}");
        await ThrowApiErrorAsync(response);

        var dto = await response.Content.ReadFromJsonAsync<PagedResponse<ItemSummaryDto>>(ApiJson.Options)
            ?? new PagedResponse<ItemSummaryDto>();

        var items = dto.Items.Select(MapSummary).ToList();
        return new PagedResult<Item>(items, dto.TotalItems, dto.Page, dto.PageSize, dto.TotalPages);
    }

    /// <summary>
    /// This loads one item by id from the live API and returns null when the
    /// server reports 404, which keeps missing records as a normal repository
    /// result instead of an exception.
    /// </summary>
    public async Task<Item?> GetItemByIdAsync(int id)
    {
        var response = await _http.GetAsync($"items/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowApiErrorAsync(response);
        var dto = await response.Content.ReadFromJsonAsync<ItemDetailDto>(ApiJson.Options);
        return dto == null ? null : MapDetail(dto);
    }

    /// <summary>
    /// This creates a new listing through POST /items and lets the API decide
    /// the owner from the signed-in user's JWT.
    /// </summary>
    public async Task<Item> CreateItemAsync(Item item)
    {
        var body = new CreateItemRequest(
            item.Title,
            item.Description,
            item.DailyRate,
            item.CategoryId,
            item.Latitude ?? 55.9533,
            item.Longitude ?? -3.1883);

        var response = await _http.PostAsJsonAsync("items", body, ApiJson.Options);
        await ThrowApiErrorAsync(response);

        var dto = await response.Content.ReadFromJsonAsync<ItemDetailDto>(ApiJson.Options);
        return MapDetail(dto!);
    }

    /// <summary>
    /// This sends a partial update to PUT /items/{id}, preserves 404 as null,
    /// and maps permission or validation failures into normal .NET exceptions.
    /// </summary>
    public async Task<Item?> UpdateItemAsync(int id, UpdateItemRequest updates)
    {
        var response = await _http.PutAsJsonAsync($"items/{id}", updates, ApiJson.Options);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ThrowApiErrorAsync(response);

        var dto = await response.Content.ReadFromJsonAsync<ItemDetailDto>(ApiJson.Options);
        return dto == null ? null : MapDetail(dto);
    }

    /// <summary>
    /// This derives a "my listings" view from the public item list because the
    /// current API reference does not provide a dedicated owner-items endpoint.
    /// </summary>
    public async Task<List<Item>> GetByOwnerAsync(int ownerId)
    {
        var firstPage = await GetItemsAsync(pageSize: 100);
        return firstPage.Items.Where(i => i.OwnerId == ownerId).ToList();
    }

    /// <summary>
    /// This loads categories from the live API and accepts either the wrapped
    /// shape or direct array shape described across the coursework notes.
    /// </summary>
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        var response = await _http.GetAsync("categories");
        await ThrowApiErrorAsync(response);

        var json = await response.Content.ReadAsStringAsync();
        List<CategoryDto>? categoryDtos = null;

        try
        {
            categoryDtos = System.Text.Json.JsonSerializer.Deserialize<CategoryListResponse>(json, ApiJson.Options)?.Categories;
        }
        catch
        {
            categoryDtos = System.Text.Json.JsonSerializer.Deserialize<List<CategoryDto>>(json, ApiJson.Options);
        }

        return (categoryDtos ?? new List<CategoryDto>()).Select(c => new Category
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug
        }).ToList();
    }

    /// <summary>
    /// This converts the API list/detail item DTO into the local domain model,
    /// including joined display fields such as category slug and owner name.
    /// </summary>
    private static Item MapSummary(ItemSummaryDto dto)
    {
        return new Item
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            DailyRate = dto.DailyRate,
            CategoryId = dto.CategoryId,
            Category = new Category
            {
                Id = dto.CategoryId,
                Name = ToDisplayName(dto.Category),
                Slug = dto.Category
            },
            OwnerId = dto.OwnerId,
            Owner = new User
            {
                Id = dto.OwnerId,
                FirstName = dto.OwnerName,
                LastName = string.Empty
            },
            IsAvailable = dto.IsAvailable,
            ImageUrl = dto.ImageUrl,
            CreatedAt = dto.CreatedAt,
            AverageRating = dto.AverageRating,
            DistanceKm = dto.Distance,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
    }

    /// <summary>
    /// This maps the detail DTO through the same summary mapper because the
    /// current domain model stores item details and review rows separately.
    /// </summary>
    private static Item MapDetail(ItemDetailDto dto)
    {
        return MapSummary(dto);
    }

    /// <summary>
    /// This turns a category slug into a simple display label when the API has
    /// not supplied a separate category name.
    /// </summary>
    private static string ToDisplayName(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(slug[0]) + slug[1..];
    }

    /// <summary>
    /// This translates non-success HTTP responses into repository exceptions
    /// that view-models and tests can handle consistently.
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
            // Some failed responses may not include the standard envelope.
        }

        var message = error?.Message ?? response.ReasonPhrase ?? "Request failed";
        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ArgumentException(message),
            HttpStatusCode.Unauthorized => new UnauthorizedAccessException(message),
            HttpStatusCode.Forbidden => new UnauthorizedAccessException(message),
            HttpStatusCode.Conflict => new InvalidOperationException(message),
            HttpStatusCode.TooManyRequests => new HttpRequestException("Rate limited. Try again soon."),
            _ => new HttpRequestException($"{(int)response.StatusCode} {message}")
        };
    }
}
