using RentalApp.Database.Models;

namespace RentalApp.Database.Repositories;

public interface IRentalRepository : IRepository<Rental>
{
    Task<Rental> CreateAsync(Rental rental);
    Task<Rental?> GetByIdAsync(int id);
    Task<List<Rental>> GetIncomingForOwnerAsync(int ownerId, RentalStatus? statusFilter = null);
    Task<List<Rental>> GetOutgoingForBorrowerAsync(int borrowerId, RentalStatus? statusFilter = null);
    Task<Rental?> UpdateStatusAsync(int rentalId, RentalStatus newStatus);
    Task<bool> HasActiveOverlapAsync(int itemId, DateTime startDate, DateTime endDate);
}
