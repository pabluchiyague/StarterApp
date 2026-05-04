using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Models.Api;
using RentalApp.Services;

namespace RentalApp.Tests;

internal sealed class FakeNavigationService : INavigationService
{
    public string? LastRoute { get; private set; }
    public Dictionary<string, object>? LastParameters { get; private set; }
    public int BackCount { get; private set; }

    public Task NavigateToAsync(string route)
    {
        LastRoute = route;
        LastParameters = null;
        return Task.CompletedTask;
    }

    public Task NavigateToAsync(string route, Dictionary<string, object> parameters)
    {
        LastRoute = route;
        LastParameters = parameters;
        return Task.CompletedTask;
    }

    public Task NavigateBackAsync()
    {
        BackCount++;
        return Task.CompletedTask;
    }

    public Task NavigateToRootAsync() => Task.CompletedTask;

    public Task PopToRootAsync() => Task.CompletedTask;
}

internal sealed class FakeAuthenticationService : IAuthenticationService
{
    public event EventHandler<bool>? AuthenticationStateChanged;

    public bool IsAuthenticated => CurrentUser != null;
    public User? CurrentUser { get; set; }
    public List<string> CurrentUserRoles { get; } = new();

    public Task<AuthenticationResult> LoginAsync(string email, string password)
    {
        CurrentUser = new User { Id = 1, Email = email };
        AuthenticationStateChanged?.Invoke(this, true);
        return Task.FromResult(new AuthenticationResult(true, "Login successful"));
    }

    public Task<AuthenticationResult> RegisterAsync(string firstName, string lastName, string email, string password) =>
        Task.FromResult(new AuthenticationResult(true, "Registration successful"));

    public Task LogoutAsync()
    {
        CurrentUser = null;
        AuthenticationStateChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }

    public bool HasRole(string roleName) => CurrentUserRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    public bool HasAnyRole(params string[] roleNames) => roleNames.Any(HasRole);
    public bool HasAllRoles(params string[] roleNames) => roleNames.All(HasRole);
    public Task<bool> ChangePasswordAsync(string currentPassword, string newPassword) => Task.FromResult(false);
}

internal sealed class FakeLocationService : ILocationService
{
    public (double Latitude, double Longitude)? CurrentLocation { get; set; }
    public int CallCount { get; private set; }

    public Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync()
    {
        CallCount++;
        return Task.FromResult(CurrentLocation);
    }
}

internal sealed class FakeItemRepository : IItemRepository
{
    public List<Category> Categories { get; } =
    [
        new Category { Id = 1, Name = "Tools", Slug = "tools" },
        new Category { Id = 2, Name = "Electronics", Slug = "electronics" }
    ];

    public List<Item> Items { get; } = new();
    public Item? ItemById { get; set; }
    public NearbySearchResult NearbyResult { get; set; } = new([], 0, 0, 5, 0);
    public Item? CreatedItem { get; private set; }
    public UpdateItemRequest? LastUpdate { get; private set; }
    public int? LastUpdateId { get; private set; }
    public (string? CategorySlug, string? Search, int Page, int PageSize)? LastGetItemsCall { get; private set; }
    public (double Latitude, double Longitude, double RadiusKm, string? CategorySlug)? LastNearbyCall { get; private set; }

    public Task<PagedResult<Item>> GetItemsAsync(
        string? categorySlug = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        LastGetItemsCall = (categorySlug, search, page, pageSize);
        return Task.FromResult(new PagedResult<Item>(Items, Items.Count, page, pageSize, 1));
    }

    public Task<Item?> GetItemByIdAsync(int id) => Task.FromResult(ItemById?.Id == id ? ItemById : null);

    public Task<Item> CreateItemAsync(Item item)
    {
        item.Id = item.Id == 0 ? 99 : item.Id;
        CreatedItem = item;
        return Task.FromResult(item);
    }

    public Task<Item?> UpdateItemAsync(int id, UpdateItemRequest updates)
    {
        LastUpdateId = id;
        LastUpdate = updates;
        if (ItemById == null || ItemById.Id != id)
        {
            return Task.FromResult<Item?>(null);
        }

        var updated = new Item
        {
            Id = id,
            Title = updates.Title ?? ItemById.Title,
            Description = updates.Description ?? ItemById.Description,
            DailyRate = updates.DailyRate ?? ItemById.DailyRate,
            CategoryId = updates.CategoryId ?? ItemById.CategoryId,
            OwnerId = ItemById.OwnerId,
            IsAvailable = updates.IsAvailable ?? ItemById.IsAvailable,
            Latitude = updates.Latitude ?? ItemById.Latitude,
            Longitude = updates.Longitude ?? ItemById.Longitude
        };

        ItemById = updated;
        return Task.FromResult<Item?>(updated);
    }

    public Task<List<Item>> GetByOwnerAsync(int ownerId) =>
        Task.FromResult(Items.Where(i => i.OwnerId == ownerId).ToList());

    public Task<NearbySearchResult> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm = 5,
        string? categorySlug = null)
    {
        LastNearbyCall = (latitude, longitude, radiusKm, categorySlug);
        return Task.FromResult(NearbyResult);
    }

    public Task<List<Category>> GetAllCategoriesAsync() => Task.FromResult(Categories);
}

internal sealed class FakeUserProfileRepository : IUserProfileRepository
{
    public UserProfile? Profile { get; set; }
    public int? LastUserId { get; private set; }

    public Task<UserProfile?> GetProfileAsync(int userId)
    {
        LastUserId = userId;
        return Task.FromResult(Profile);
    }
}
