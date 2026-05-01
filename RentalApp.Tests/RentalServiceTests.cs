using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Database.Services;

namespace RentalApp.Tests;

[Collection("Database")]
public class RentalServiceTests
{
    private readonly DatabaseFixture _fixture;
    private readonly LocalRentalRepository _repository;
    private readonly RentalService _service;

    public RentalServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.Seed();
        _repository = new LocalRentalRepository(_fixture.TestDbContext);
        _service = new RentalService(_fixture.TestDbContext, _repository);
    }

    [Fact]
    public async Task RequestRentalAsync_ValidRequest_CreatesRequestedRentalWithInclusiveTotal()
    {
        var (item, borrower) = await CreateItemAndBorrowerAsync("valid");

        var rental = await _service.RequestRentalAsync(
            item.Id,
            borrower.Id,
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 3));

        Assert.NotEqual(0, rental.Id);
        Assert.Equal(RentalStatus.Requested, rental.Status);
        Assert.Equal(item.DailyRate * 3, rental.TotalPrice);
    }

    [Fact]
    public async Task RequestRentalAsync_StartAfterEnd_ThrowsArgumentException()
    {
        var (item, borrower) = await CreateItemAndBorrowerAsync("dates");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RequestRentalAsync(item.Id, borrower.Id, new DateTime(2026, 6, 5), new DateTime(2026, 6, 1)));
    }

    [Fact]
    public async Task RequestRentalAsync_OwnItem_ThrowsInvalidOperationException()
    {
        var (item, _) = await CreateItemAndBorrowerAsync("own");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RequestRentalAsync(item.Id, item.OwnerId, new DateTime(2026, 6, 1), new DateTime(2026, 6, 2)));
    }

    [Fact]
    public async Task RequestRentalAsync_OverlappingApprovedRental_ThrowsInvalidOperationException()
    {
        var (item, borrower) = await CreateItemAndBorrowerAsync("overlap");
        _fixture.TestDbContext.Rentals.Add(new Rental
        {
            ItemId = item.Id,
            BorrowerId = borrower.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 5),
            Status = RentalStatus.Approved,
            TotalPrice = 50m
        });
        await _fixture.TestDbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RequestRentalAsync(item.Id, borrower.Id, new DateTime(2026, 7, 3), new DateTime(2026, 7, 8)));
    }

    [Fact]
    public async Task ApproveRentalAsync_RequestedRental_UpdatesStatus()
    {
        var (item, borrower) = await CreateItemAndBorrowerAsync("approve");
        var rental = await _service.RequestRentalAsync(item.Id, borrower.Id, new DateTime(2026, 8, 1), new DateTime(2026, 8, 1));

        var approved = await _service.ApproveRentalAsync(rental.Id);

        Assert.NotNull(approved);
        Assert.Equal(RentalStatus.Approved, approved!.Status);
        Assert.NotNull(approved.ApprovedAt);
    }

    private async Task<(Item item, User borrower)> CreateItemAndBorrowerAsync(string label)
    {
        var owner = new User
        {
            FirstName = "Rental",
            LastName = "Owner",
            Email = $"owner.{label}.{Guid.NewGuid():N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sup3rSecret!"),
            PasswordSalt = string.Empty,
            IsActive = true
        };
        var borrower = new User
        {
            FirstName = "Rental",
            LastName = "Borrower",
            Email = $"borrower.{label}.{Guid.NewGuid():N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sup3rSecret!"),
            PasswordSalt = string.Empty,
            IsActive = true
        };
        _fixture.TestDbContext.Users.AddRange(owner, borrower);
        await _fixture.TestDbContext.SaveChangesAsync();

        var category = _fixture.TestDbContext.Categories.First(c => c.Slug == "tools");
        var item = new Item
        {
            Title = $"Rental item {Guid.NewGuid():N}",
            DailyRate = 10m,
            CategoryId = category.Id,
            OwnerId = owner.Id,
            IsAvailable = true
        };
        _fixture.TestDbContext.Items.Add(item);
        await _fixture.TestDbContext.SaveChangesAsync();

        return (item, borrower);
    }
}
