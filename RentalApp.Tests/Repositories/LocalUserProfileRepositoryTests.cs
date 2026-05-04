using RentalApp.Database.Models;
using RentalApp.Database.Repositories;

namespace RentalApp.Tests;

[Collection("Database")]
public class LocalUserProfileRepositoryTests
{
    private readonly DatabaseFixture _fixture;
    private readonly LocalUserProfileRepository _repository;

    public LocalUserProfileRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.Seed();
        _repository = new LocalUserProfileRepository(_fixture.TestDbContext);
    }

    [Fact]
    public async Task GetProfileAsync_UserWithListingsRentalsAndReviews_ReturnsProfileSummary()
    {
        var profileUser = await CreateUserAsync("Profile", "Owner", "profile-owner");
        var reviewer = await CreateUserAsync("Mike", "Johnson", "reviewer");
        var otherOwner = await CreateUserAsync("Other", "Owner", "other-owner");

        var ownedItem = await CreateItemAsync(profileUser.Id, "tools", "Profile drill");
        var reviewedRental = await CreateRentalAsync(ownedItem.Id, reviewer.Id, RentalStatus.Completed);
        var secondReviewedRental = await CreateRentalAsync(ownedItem.Id, reviewer.Id, RentalStatus.Completed);
        var borrowedItem = await CreateItemAsync(otherOwner.Id, "games", "Borrowed game");
        await CreateRentalAsync(borrowedItem.Id, profileUser.Id, RentalStatus.Completed);

        _fixture.TestDbContext.Reviews.AddRange(
            new Review
            {
                RentalId = reviewedRental.Id,
                ReviewerId = reviewer.Id,
                Rating = 5,
                Comment = "Great item, very helpful owner!",
                CreatedAt = new DateTime(2026, 1, 20, 14, 0, 0, DateTimeKind.Utc)
            },
            new Review
            {
                RentalId = secondReviewedRental.Id,
                ReviewerId = reviewer.Id,
                Rating = 3,
                Comment = "Good overall",
                CreatedAt = new DateTime(2026, 1, 19, 14, 0, 0, DateTimeKind.Utc)
            });
        await _fixture.TestDbContext.SaveChangesAsync();

        var profile = await _repository.GetProfileAsync(profileUser.Id);

        Assert.NotNull(profile);
        Assert.Equal(profileUser.Id, profile!.Id);
        Assert.Equal("Profile", profile.FirstName);
        Assert.Equal("Owner", profile.LastName);
        Assert.Equal(4.0, profile.AverageRating);
        Assert.Equal(1, profile.ItemsListed);
        Assert.Equal(1, profile.RentalsCompleted);
        Assert.Equal(2, profile.Reviews.Count);
        Assert.Equal(5, profile.Reviews[0].Rating);
        Assert.Equal("Mike Johnson", profile.Reviews[0].Reviewer!.FullName);
    }

    [Fact]
    public async Task GetProfileAsync_MissingUser_ReturnsNull()
    {
        var profile = await _repository.GetProfileAsync(-1);

        Assert.Null(profile);
    }

    private async Task<User> CreateUserAsync(string firstName, string lastName, string label)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{label}.{Guid.NewGuid():N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sup3rSecret!"),
            PasswordSalt = string.Empty,
            IsActive = true
        };

        _fixture.TestDbContext.Users.Add(user);
        await _fixture.TestDbContext.SaveChangesAsync();
        return user;
    }

    private async Task<Item> CreateItemAsync(int ownerId, string categorySlug, string title)
    {
        var category = _fixture.TestDbContext.Categories.Single(c => c.Slug == categorySlug);
        var item = new Item
        {
            Title = $"{title} {Guid.NewGuid():N}",
            Description = title,
            DailyRate = 5m,
            CategoryId = category.Id,
            OwnerId = ownerId,
            IsAvailable = true
        };

        _fixture.TestDbContext.Items.Add(item);
        await _fixture.TestDbContext.SaveChangesAsync();
        return item;
    }

    private async Task<Rental> CreateRentalAsync(int itemId, int borrowerId, RentalStatus status)
    {
        var rental = new Rental
        {
            ItemId = itemId,
            BorrowerId = borrowerId,
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 9, 2),
            Status = status,
            TotalPrice = 10m
        };

        _fixture.TestDbContext.Rentals.Add(rental);
        await _fixture.TestDbContext.SaveChangesAsync();
        return rental;
    }
}
