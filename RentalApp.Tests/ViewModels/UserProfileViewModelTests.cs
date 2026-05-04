using RentalApp.Database.Models;
using RentalApp.ViewModels;

namespace RentalApp.Tests;

public class UserProfileViewModelTests
{
    [Fact]
    public async Task LoadAsync_ProfileFound_MapsSummaryAndReviews()
    {
        var repository = new FakeUserProfileRepository
        {
            Profile = new UserProfile
            {
                Id = 456,
                FirstName = "Sarah",
                LastName = "Smith",
                AverageRating = 4.8,
                ItemsListed = 8,
                RentalsCompleted = 24,
                Reviews =
                [
                    new Review
                    {
                        Id = 101,
                        Rating = 5,
                        Comment = "Great item",
                        Reviewer = new User { FirstName = "Mike", LastName = "Johnson" }
                    }
                ]
            }
        };
        var viewModel = new UserProfileViewModel(
            repository,
            new FakeAuthenticationService { CurrentUser = new User { Id = 123 } })
        {
            UserId = 456
        };

        await viewModel.LoadAsync();

        Assert.Equal(456, repository.LastUserId);
        Assert.Equal("Sarah Smith", viewModel.ProfileName);
        Assert.Equal("4.8/5", viewModel.AverageRating);
        Assert.Equal("8", viewModel.ItemsListed);
        Assert.Equal("24", viewModel.RentalsCompleted);
        Assert.Equal("Reviews received (1)", viewModel.ReviewsTitle);
        Assert.True(viewModel.HasReviews);
        Assert.Single(viewModel.Reviews);
    }

    [Fact]
    public async Task LoadAsync_NoRouteUserAndNotSignedIn_SetsSignInError()
    {
        var repository = new FakeUserProfileRepository();
        var viewModel = new UserProfileViewModel(repository, new FakeAuthenticationService());

        await viewModel.LoadAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Sign in to view your profile.", viewModel.ErrorMessage);
        Assert.Null(repository.LastUserId);
    }

    [Fact]
    public async Task LoadAsync_ProfileMissing_SetsNotFoundError()
    {
        var repository = new FakeUserProfileRepository();
        var viewModel = new UserProfileViewModel(
            repository,
            new FakeAuthenticationService { CurrentUser = new User { Id = 123 } });

        await viewModel.LoadAsync();

        Assert.Equal(123, repository.LastUserId);
        Assert.True(viewModel.HasError);
        Assert.Equal("User not found.", viewModel.ErrorMessage);
        Assert.Empty(viewModel.Reviews);
    }
}
