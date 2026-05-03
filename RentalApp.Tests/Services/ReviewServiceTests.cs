using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Database.Services;
using RentalApp.Models.Api;

namespace RentalApp.Tests;

public class ReviewServiceTests
{
    [Fact]
    public async Task SubmitReviewAsync_CompletedBorrowerRental_CreatesReview()
    {
        // Arrange
        var rentalRepository = new FakeRentalRepository(new Rental
        {
            Id = 5,
            BorrowerId = 10,
            Status = RentalStatus.Completed
        });
        var reviewRepository = new FakeReviewRepository();
        var service = new ReviewService(rentalRepository, reviewRepository);

        // Act
        var review = await service.SubmitReviewAsync(5, 10, 5, "Great item");

        // Assert
        Assert.Equal(5, review.RentalId);
        Assert.Equal(10, review.ReviewerId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Great item", review.Comment);
    }

    [Fact]
    public async Task SubmitReviewAsync_NotCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new ReviewService(
            new FakeRentalRepository(new Rental { Id = 5, BorrowerId = 10, Status = RentalStatus.Returned }),
            new FakeReviewRepository());

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitReviewAsync(5, 10, 5, "Too early"));
    }

    private sealed class FakeRentalRepository : IRentalRepository
    {
        private readonly Rental? _rental;

        public FakeRentalRepository(Rental? rental)
        {
            _rental = rental;
        }

        public Task<Rental> CreateAsync(Rental rental) => Task.FromResult(rental);
        public Task<Rental?> GetByIdAsync(int id) => Task.FromResult(_rental?.Id == id ? _rental : null);
        public Task<List<Rental>> GetIncomingForOwnerAsync(int ownerId, RentalStatus? statusFilter = null) => Task.FromResult(new List<Rental>());
        public Task<List<Rental>> GetOutgoingForBorrowerAsync(int borrowerId, RentalStatus? statusFilter = null) => Task.FromResult(new List<Rental>());
        public Task<Rental?> UpdateStatusAsync(int rentalId, RentalStatus newStatus) => Task.FromResult<Rental?>(null);
        public Task<bool> HasActiveOverlapAsync(int itemId, DateTime startDate, DateTime endDate) => Task.FromResult(false);
    }

    private sealed class FakeReviewRepository : IReviewRepository
    {
        public Task<Review> CreateAsync(Review review)
        {
            review.Id = 99;
            return Task.FromResult(review);
        }

        public Task<PagedResult<Review>> GetForItemAsync(int itemId, int page = 1, int pageSize = 10) =>
            Task.FromResult(new PagedResult<Review>(Array.Empty<Review>(), 0, page, pageSize, 0));

        public Task<PagedResult<Review>> GetForUserAsync(int userId, int page = 1, int pageSize = 10) =>
            Task.FromResult(new PagedResult<Review>(Array.Empty<Review>(), 0, page, pageSize, 0));
    }
}
