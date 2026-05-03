using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Database.Repositories;

public class LocalRentalRepository : IRentalRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// This stores the EF Core context used to persist and query rentals in
    /// local mode and integration tests.
    /// </summary>
    public LocalRentalRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// This inserts a new rental request locally, stamps timestamps, saves it,
    /// and reloads it with related item and borrower data.
    /// </summary>
    public async Task<Rental> CreateAsync(Rental rental)
    {
        rental.CreatedAt = DateTime.UtcNow;
        rental.UpdatedAt = DateTime.UtcNow;
        _context.Rentals.Add(rental);
        await _context.SaveChangesAsync();
        return (await GetByIdAsync(rental.Id))!;
    }

    /// <summary>
    /// This loads one rental with its item, owner, category, borrower, and
    /// review so workflow screens can show the full context.
    /// </summary>
    public async Task<Rental?> GetByIdAsync(int id)
    {
        return await _context.Rentals
            .Include(r => r.Item)
            .ThenInclude(i => i!.Owner)
            .Include(r => r.Item)
            .ThenInclude(i => i!.Category)
            .Include(r => r.Borrower)
            .Include(r => r.Review)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <summary>
    /// This returns rentals requested on items owned by a specific user and
    /// optionally limits the result to a single workflow status.
    /// </summary>
    public async Task<List<Rental>> GetIncomingForOwnerAsync(int ownerId, RentalStatus? statusFilter = null)
    {
        IQueryable<Rental> query = _context.Rentals
            .Include(r => r.Item)
            .ThenInclude(i => i!.Category)
            .Include(r => r.Borrower)
            .Where(r => r.Item != null && r.Item.OwnerId == ownerId);

        if (statusFilter != null)
        {
            query = query.Where(r => r.Status == statusFilter);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// This returns rentals requested by a specific borrower and optionally
    /// limits the result to a single workflow status.
    /// </summary>
    public async Task<List<Rental>> GetOutgoingForBorrowerAsync(int borrowerId, RentalStatus? statusFilter = null)
    {
        IQueryable<Rental> query = _context.Rentals
            .Include(r => r.Item)
            .ThenInclude(i => i!.Owner)
            .Include(r => r.Item)
            .ThenInclude(i => i!.Category)
            .Where(r => r.BorrowerId == borrowerId);

        if (statusFilter != null)
        {
            query = query.Where(r => r.Status == statusFilter);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// This updates the stored rental status and records approval time when
    /// the new status is Approved.
    /// </summary>
    public async Task<Rental?> UpdateStatusAsync(int rentalId, RentalStatus newStatus)
    {
        var rental = await _context.Rentals.FindAsync(rentalId);
        if (rental == null)
        {
            return null;
        }

        rental.Status = newStatus;
        rental.UpdatedAt = DateTime.UtcNow;
        if (newStatus == RentalStatus.Approved)
        {
            rental.ApprovedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(rentalId);
    }

    /// <summary>
    /// This checks whether an item already has an active rental whose date
    /// range overlaps the requested inclusive date range.
    /// </summary>
    public async Task<bool> HasActiveOverlapAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        return await _context.Rentals.AnyAsync(r =>
            r.ItemId == itemId &&
            (r.Status == RentalStatus.Approved ||
             r.Status == RentalStatus.OutForRent ||
             r.Status == RentalStatus.Overdue) &&
            r.StartDate <= endDate &&
            r.EndDate >= startDate);
    }
}
