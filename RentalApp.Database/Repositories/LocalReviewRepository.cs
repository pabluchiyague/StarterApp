using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public class LocalReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// This stores the EF Core context used to validate and persist reviews in
    /// local mode.
    /// </summary>
    public LocalReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// This creates a local review only when the rental is completed, the
    /// reviewer is the borrower, the rating is valid, and no review exists yet.
    /// </summary>
    public async Task<Review> CreateAsync(Review review)
    {
        if (review.Rating is < 1 or > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }

        var rental = await _context.Rentals.FirstOrDefaultAsync(r => r.Id == review.RentalId);
        if (rental == null)
        {
            throw new ArgumentException("Rental not found.");
        }

        if (rental.Status != RentalStatus.Completed)
        {
            throw new InvalidOperationException("Only completed rentals can be reviewed.");
        }

        if (rental.BorrowerId != review.ReviewerId)
        {
            throw new UnauthorizedAccessException("Only the borrower can review this rental.");
        }

        if (await _context.Reviews.AnyAsync(r => r.RentalId == review.RentalId))
        {
            throw new InvalidOperationException("This rental has already been reviewed.");
        }

        review.CreatedAt = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    /// <summary>
    /// This returns paginated reviews for rentals attached to one item.
    /// </summary>
    public async Task<PagedResult<Review>> GetForItemAsync(int itemId, int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Rental)
            .ThenInclude(r => r!.Item)
            .Where(r => r.Rental != null && r.Rental.ItemId == itemId);

        return await PageAsync(query, page, pageSize);
    }

    /// <summary>
    /// This returns paginated reviews left on items owned by one user.
    /// </summary>
    public async Task<PagedResult<Review>> GetForUserAsync(int userId, int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Rental)
            .ThenInclude(r => r!.Item)
            .Where(r => r.Rental != null &&
                        r.Rental.Item != null &&
                        r.Rental.Item.OwnerId == userId);

        return await PageAsync(query, page, pageSize);
    }

    /// <summary>
    /// This applies ordering, skip/take pagination, and total counts to any
    /// review query.
    /// </summary>
    private static async Task<PagedResult<Review>> PageAsync(IQueryable<Review> query, int page, int pageSize)
    {
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Review>(reviews, totalItems, page, pageSize, totalPages);
    }
}
