using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public interface IReviewRepository
{
    Task<Review> CreateAsync(Review review);
    Task<PagedResult<Review>> GetForItemAsync(int itemId, int page = 1, int pageSize = 10);
    Task<PagedResult<Review>> GetForUserAsync(int userId, int page = 1, int pageSize = 10);
}
