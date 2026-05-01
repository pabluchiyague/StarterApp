using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;

namespace RentalApp.Database.Services;

public class RentalService
{
    private readonly AppDbContext _context;
    private readonly IRentalRepository _repository;

    /// <summary>
    /// This stores the database context and rental repository used to validate
    /// requests and persist workflow changes.
    /// </summary>
    public RentalService(AppDbContext context, IRentalRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    /// <summary>
    /// This validates a new rental request, prevents invalid dates, own-item
    /// rentals, unavailable items, and overlapping active rentals, then creates
    /// the requested rental with an inclusive-day total price.
    /// </summary>
    public async Task<Rental> RequestRentalAsync(int itemId, int borrowerId, DateTime startDate, DateTime endDate)
    {
        if (startDate.Date > endDate.Date)
        {
            throw new ArgumentException("Start date must be before or equal to end date.");
        }

        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null)
        {
            throw new ArgumentException("Item not found.");
        }

        if (!item.IsAvailable)
        {
            throw new InvalidOperationException("Item is not available.");
        }

        if (item.OwnerId == borrowerId)
        {
            throw new InvalidOperationException("You cannot rent your own item.");
        }

        if (await _repository.HasActiveOverlapAsync(itemId, startDate.Date, endDate.Date))
        {
            throw new InvalidOperationException("Item is already rented for those dates.");
        }

        var days = (endDate.Date - startDate.Date).Days + 1;
        return await _repository.CreateAsync(new Rental
        {
            ItemId = itemId,
            BorrowerId = borrowerId,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Status = RentalStatus.Requested,
            TotalPrice = item.DailyRate * days
        });
    }

    /// <summary>
    /// This moves a requested rental into the approved state.
    /// </summary>
    public Task<Rental?> ApproveRentalAsync(int rentalId) =>
        TransitionAsync(rentalId, rental => rental.State.Approve().Status);

    /// <summary>
    /// This moves a requested rental into the rejected terminal state.
    /// </summary>
    public Task<Rental?> RejectRentalAsync(int rentalId) =>
        TransitionAsync(rentalId, rental => rental.State.Reject().Status);

    /// <summary>
    /// This moves an approved rental into the out-for-rent state.
    /// </summary>
    public Task<Rental?> MarkOutForRentAsync(int rentalId) =>
        TransitionAsync(rentalId, rental => rental.State.MarkOutForRent().Status);

    /// <summary>
    /// This moves an out-for-rent or overdue rental into the returned state.
    /// </summary>
    public Task<Rental?> MarkReturnedAsync(int rentalId) =>
        TransitionAsync(rentalId, rental => rental.State.MarkReturned().Status);

    /// <summary>
    /// This moves an out-for-rent rental into the overdue state.
    /// </summary>
    public Task<Rental?> MarkOverdueAsync(int rentalId) =>
        TransitionAsync(rentalId, rental => rental.State.MarkOverdue().Status);

    /// <summary>
    /// This moves a returned rental into the completed terminal state.
    /// </summary>
    public Task<Rental?> CompleteAsync(int rentalId) =>
        TransitionAsync(rentalId, rental => rental.State.Complete().Status);

    /// <summary>
    /// This loads the rental, asks the current state object for the next
    /// status, and persists that status through the repository.
    /// </summary>
    private async Task<Rental?> TransitionAsync(int rentalId, Func<Rental, RentalStatus> transition)
    {
        var rental = await _repository.GetByIdAsync(rentalId);
        if (rental == null)
        {
            return null;
        }

        var newStatus = transition(rental);
        return await _repository.UpdateStatusAsync(rentalId, newStatus);
    }
}
