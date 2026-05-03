using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public interface IItemRepository : IRepository<Item>
{
    Task<PagedResult<Item>> GetItemsAsync(
        string? categorySlug = null,
        string? search = null,
        int page = 1,
        int pageSize = 20);

    Task<Item?> GetItemByIdAsync(int id);

    Task<Item> CreateItemAsync(Item item);

    Task<Item?> UpdateItemAsync(int id, UpdateItemRequest updates);

    Task<List<Item>> GetByOwnerAsync(int ownerId);

    Task<NearbySearchResult> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm = 5,
        string? categorySlug = null);

    Task<List<Category>> GetAllCategoriesAsync();
}
