using RentalApp.Database.Models;
using RentalApp.Database.Repositories;

namespace RentalApp.Database.Services;

/// <summary>
/// This keeps review business rules out of ViewModels and coordinates review
/// submission through rental and review repositories.
/// </summary>
public class ReviewService
{
    private readonly IRentalRepository _rentalRepository;
    private readonly IReviewRepository _reviewRepository;

    public ReviewService(IRentalRepository rentalRepository, IReviewRepository reviewRepository)
    {
        _rentalRepository = rentalRepository;
        _reviewRepository = reviewRepository;
    }

    /// <summary>
    /// This validates that a signed-in borrower is reviewing a completed rental,
    /// checks rating/comment limits, and saves the review.
    /// </summary>
    public async Task<Review> SubmitReviewAsync(int rentalId, int reviewerId, int rating, string? comment)
    {
        var rental = await _rentalRepository.GetByIdAsync(rentalId)
            ?? throw new ArgumentException("Rental not found.");

        if (rental.Status != RentalStatus.Completed)
        {
            throw new InvalidOperationException("You can only review completed rentals.");
        }

        if (rental.BorrowerId != reviewerId)
        {
            throw new UnauthorizedAccessException("You can only review rentals you borrowed.");
        }

        if (rating is < 1 or > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5.");
        }

        if (comment?.Length > 500)
        {
            throw new ArgumentException("Comment must be 500 characters or fewer.");
        }

        return await _reviewRepository.CreateAsync(new Review
        {
            RentalId = rentalId,
            ReviewerId = reviewerId,
            Rating = rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        });
    }
}
