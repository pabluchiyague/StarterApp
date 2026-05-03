using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Database.Repositories;

public class LocalUserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _context;

    public LocalUserProfileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetProfileAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return null;
        }

        var reviews = await _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Rental)
            .ThenInclude(r => r!.Item)
            .Where(r => r.Rental != null &&
                        r.Rental.Item != null &&
                        r.Rental.Item.OwnerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var itemsListed = await _context.Items.CountAsync(i => i.OwnerId == userId);
        var rentalsCompleted = await _context.Rentals.CountAsync(r => r.BorrowerId == userId &&
                                                                      r.Status == RentalStatus.Completed);

        return new UserProfile
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AverageRating = reviews.Count == 0 ? null : reviews.Average(r => r.Rating),
            ItemsListed = itemsListed,
            RentalsCompleted = rentalsCompleted,
            Reviews = reviews
        };
    }
}
