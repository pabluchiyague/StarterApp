using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public class LocalItemRepository : IItemRepository
{
    private readonly AppDbContext _context;
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    /// <summary>
    /// This stores the EF Core context used by the local repository when the
    /// app is running in offline/local mode or when tests need a real database.
    /// </summary>
    public LocalItemRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// This queries local PostgreSQL items with optional category/search
    /// filters, includes display relationships, and returns a paged result.
    /// </summary>
    public async Task<PagedResult<Item>> GetItemsAsync(
        string? categorySlug = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Item> query = _context.Items
            .Include(i => i.Category)
            .Include(i => i.Owner);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == categorySlug);

            if (category == null)
            {
                return new PagedResult<Item>(Array.Empty<Item>(), 0, page, pageSize, 0);
            }

            query = query.Where(i => i.CategoryId == category.Id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.Title, like) ||
                EF.Functions.ILike(i.Description ?? string.Empty, like));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Item>(items, totalItems, page, pageSize, totalPages);
    }

    /// <summary>
    /// This loads one local item with its category, owner, and rentals so the
    /// detail page has the related data it needs.
    /// </summary>
    public async Task<Item?> GetItemByIdAsync(int id)
    {
        return await _context.Items
            .Include(i => i.Category)
            .Include(i => i.Owner)
            .Include(i => i.Rentals)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    /// <summary>
    /// This inserts a new local item, stamps its timestamps, saves it, and then
    /// reloads it with related category and owner data.
    /// </summary>
    public async Task<Item> CreateItemAsync(Item item)
    {
        EnsureLocationPoint(item);
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        return (await GetItemByIdAsync(item.Id))!;
    }

    /// <summary>
    /// This applies only the supplied update fields to a local item so partial
    /// updates do not accidentally erase untouched values.
    /// </summary>
    public async Task<Item?> UpdateItemAsync(int id, UpdateItemRequest updates)
    {
        var existing = await _context.Items.FindAsync(id);
        if (existing == null)
        {
            return null;
        }

        if (updates.Title is { } title)
        {
            existing.Title = title;
        }

        if (updates.Description is { } description)
        {
            existing.Description = description;
        }

        if (updates.DailyRate is { } dailyRate)
        {
            existing.DailyRate = dailyRate;
        }

        if (updates.CategoryId is { } categoryId)
        {
            existing.CategoryId = categoryId;
        }

        if (updates.IsAvailable is { } isAvailable)
        {
            existing.IsAvailable = isAvailable;
        }

        if (updates.Latitude is { } latitude)
        {
            existing.Latitude = latitude;
        }

        if (updates.Longitude is { } longitude)
        {
            existing.Longitude = longitude;
        }

        if (updates.Latitude.HasValue || updates.Longitude.HasValue)
        {
            EnsureLocationPoint(existing);
        }

        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetItemByIdAsync(id);
    }

    /// <summary>
    /// This returns all local items owned by one user for "my listings" style
    /// screens and tests.
    /// </summary>
    public async Task<List<Item>> GetByOwnerAsync(int ownerId)
    {
        return await _context.Items
            .Include(i => i.Category)
            .Include(i => i.Owner)
            .Where(i => i.OwnerId == ownerId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// This runs a local PostGIS ST_DWithin search around a latitude/longitude
    /// origin and returns available items inside the requested radius.
    /// </summary>
    public async Task<NearbySearchResult> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm = 5,
        string? categorySlug = null)
    {
        radiusKm = Math.Clamp(radiusKm, 0.1, 50);
        var radiusMeters = radiusKm * 1000;

        var itemsQuery = _context.Items
            .FromSqlInterpolated($@"
                SELECT *
                FROM items
                WHERE location IS NOT NULL
                  AND ""IsAvailable"" = TRUE
                  AND ST_DWithin(
                        location,
                        ST_SetSRID(ST_MakePoint({longitude}, {latitude}), 4326)::geography,
                        {radiusMeters})")
            .Include(i => i.Category)
            .Include(i => i.Owner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            itemsQuery = itemsQuery.Where(i => i.Category != null && i.Category.Slug == categorySlug);
        }

        var items = await itemsQuery.ToListAsync();
        foreach (var item in items)
        {
            item.Latitude = item.Location?.Y;
            item.Longitude = item.Location?.X;
            item.DistanceKm = item.Latitude.HasValue && item.Longitude.HasValue
                ? CalculateDistanceKm(latitude, longitude, item.Latitude.Value, item.Longitude.Value)
                : null;
        }

        var ordered = items
            .OrderBy(i => i.DistanceKm ?? double.MaxValue)
            .ToList();

        return new NearbySearchResult(ordered, latitude, longitude, radiusKm, ordered.Count);
    }

    /// <summary>
    /// This returns every locally seeded category ordered by display name for
    /// pickers and filters.
    /// </summary>
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    private static void EnsureLocationPoint(Item item)
    {
        if (item.Latitude == null || item.Longitude == null)
        {
            return;
        }

        item.Location = GeometryFactory.CreatePoint(new Coordinate(item.Longitude.Value, item.Latitude.Value));
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0088;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var startLat = ToRadians(lat1);
        var endLat = ToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(startLat) * Math.Cos(endLat) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
