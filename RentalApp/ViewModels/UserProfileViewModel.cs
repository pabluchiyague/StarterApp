using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Services;

namespace RentalApp.ViewModels;

[QueryProperty(nameof(UserId), nameof(UserId))]
public partial class UserProfileViewModel : BaseViewModel
{
    private readonly IUserProfileRepository _profileRepository;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private int userId;

    [ObservableProperty]
    private string profileName = "Profile";

    [ObservableProperty]
    private string averageRating = "No rating yet";

    [ObservableProperty]
    private string itemsListed = "0";

    [ObservableProperty]
    private string rentalsCompleted = "0";

    [ObservableProperty]
    private string reviewsTitle = "Reviews";

    [ObservableProperty]
    private ObservableCollection<Review> reviews = new();

    public bool HasReviews => Reviews.Count > 0;

    public UserProfileViewModel(
        IUserProfileRepository profileRepository,
        IAuthenticationService authService)
    {
        _profileRepository = profileRepository;
        _authService = authService;
        Title = "User profile";
    }

    partial void OnUserIdChanged(int value)
    {
        _ = LoadAsync();
    }

    partial void OnReviewsChanged(ObservableCollection<Review> value)
    {
        OnPropertyChanged(nameof(HasReviews));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var profileUserId = UserId > 0 ? UserId : _authService.CurrentUser?.Id ?? 0;
        if (profileUserId <= 0 || IsBusy)
        {
            if (profileUserId <= 0)
            {
                SetError("Sign in to view your profile.");
            }

            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var profile = await _profileRepository.GetProfileAsync(profileUserId);
            if (profile == null)
            {
                SetError("User not found.");
                Reviews = new ObservableCollection<Review>();
                ReviewsTitle = "Reviews";
                return;
            }

            UserId = profile.Id;
            ProfileName = string.IsNullOrWhiteSpace(profile.FullName) ? "User profile" : profile.FullName;
            Title = ProfileName;
            AverageRating = profile.AverageRating == null ? "No rating yet" : $"{profile.AverageRating:0.0}/5";
            ItemsListed = profile.ItemsListed.ToString();
            RentalsCompleted = profile.RentalsCompleted.ToString();
            Reviews = new ObservableCollection<Review>(profile.Reviews);
            ReviewsTitle = profile.Reviews.Count == 1
                ? "Reviews received (1)"
                : $"Reviews received ({profile.Reviews.Count})";
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
