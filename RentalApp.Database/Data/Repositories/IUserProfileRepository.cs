using RentalApp.Database.Models;

namespace RentalApp.Database.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetProfileAsync(int userId);
}
