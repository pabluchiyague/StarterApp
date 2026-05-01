using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public class LocalItemRepository : IItemRepository
{
    private readonly AppDbContext _context;

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

        if (updates.IsAvailable is { } isAvailable)
        {
            existing.IsAvailable = isAvailable;
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
    /// This returns every locally seeded category ordered by display name for
    /// pickers and filters.
    /// </summary>
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
