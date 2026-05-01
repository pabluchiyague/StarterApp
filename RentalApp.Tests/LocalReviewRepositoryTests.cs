using RentalApp.Database.Models;
using RentalApp.Database.Repositories;

namespace RentalApp.Tests;

[Collection("Database")]
public class LocalReviewRepositoryTests
{
    private readonly DatabaseFixture _fixture;
    private readonly LocalReviewRepository _repository;

    public LocalReviewRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.Seed();
        _repository = new LocalReviewRepository(_fixture.TestDbContext);
    }

    [Fact]
    public async Task CreateAsync_CompletedRentalByBorrower_CreatesReview()
    {
        var rental = await CreateCompletedRentalAsync("create");

        var review = await _repository.CreateAsync(new Review
        {
            RentalId = rental.Id,
            ReviewerId = rental.BorrowerId,
            Rating = 5,
            Comment = "Great item"
        });

        Assert.NotEqual(0, review.Id);
        Assert.Equal(5, review.Rating);
    }

    [Fact]
    public async Task CreateAsync_NotCompletedRental_ThrowsInvalidOperationException()
    {
        var rental = await CreateCompletedRentalAsync("status");
        rental.Status = RentalStatus.Returned;
        await _fixture.TestDbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.CreateAsync(new Review
        {
            RentalId = rental.Id,
            ReviewerId = rental.BorrowerId,
            Rating = 4
        }));
    }

    [Fact]
    public async Task CreateAsync_DuplicateRentalReview_ThrowsInvalidOperationException()
    {
        var rental = await CreateCompletedRentalAsync("duplicate");
        await _repository.CreateAsync(new Review
        {
            RentalId = rental.Id,
            ReviewerId = rental.BorrowerId,
            Rating = 4
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.CreateAsync(new Review
        {
            RentalId = rental.Id,
            ReviewerId = rental.BorrowerId,
            Rating = 5
        }));
    }

    [Fact]
    public async Task GetForItemAsync_ReturnsReviewsForThatItem()
    {
        var rental = await CreateCompletedRentalAsync("item");
        await _repository.CreateAsync(new Review
        {
            RentalId = rental.Id,
            ReviewerId = rental.BorrowerId,
            Rating = 4
        });

        var result = await _repository.GetForItemAsync(rental.ItemId);

        Assert.Contains(result.Items, review => review.RentalId == rental.Id);
    }

    private async Task<Rental> CreateCompletedRentalAsync(string label)
    {
        var owner = new User
        {
            FirstName = "Review",
            LastName = "Owner",
            Email = $"review.owner.{label}.{Guid.NewGuid():N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sup3rSecret!"),
            PasswordSalt = string.Empty,
            IsActive = true
        };
        var borrower = new User
        {
            FirstName = "Review",
            LastName = "Borrower",
            Email = $"review.borrower.{label}.{Guid.NewGuid():N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sup3rSecret!"),
            PasswordSalt = string.Empty,
            IsActive = true
        };
        _fixture.TestDbContext.Users.AddRange(owner, borrower);
        await _fixture.TestDbContext.SaveChangesAsync();

        var category = _fixture.TestDbContext.Categories.First(c => c.Slug == "games");
        var item = new Item
        {
            Title = $"Review item {Guid.NewGuid():N}",
            DailyRate = 6m,
            CategoryId = category.Id,
            OwnerId = owner.Id,
            IsAvailable = true
        };
        _fixture.TestDbContext.Items.Add(item);
        await _fixture.TestDbContext.SaveChangesAsync();

        var rental = new Rental
        {
            ItemId = item.Id,
            BorrowerId = borrower.Id,
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 9, 2),
            Status = RentalStatus.Completed,
            TotalPrice = 12m
        };
        _fixture.TestDbContext.Rentals.Add(rental);
        await _fixture.TestDbContext.SaveChangesAsync();

        return rental;
    }
}
