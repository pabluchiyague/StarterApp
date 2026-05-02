namespace RentalApp.Database.Models;

/// <summary>
/// Public profile shape returned by GET /users/{id}/profile and used by the
/// profile page.
/// </summary>
public class UserProfile
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public double? AverageRating { get; set; }

    public int ItemsListed { get; set; }

    public int RentalsCompleted { get; set; }

    public List<Review> Reviews { get; set; } = new();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
