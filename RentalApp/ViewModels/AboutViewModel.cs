using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class AboutViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly IReviewRepository _reviewRepository;

    public string Version => AppInfo.VersionString;

    public string Message => "Peer-to-peer rental marketplace using MAUI, MVVM, repositories, services, JWT API auth, and state-based rental workflow.";

    [ObservableProperty]
    private string userName = "Not signed in";

    [ObservableProperty]
    private string userEmail = string.Empty;

    [ObservableProperty]
    private string averageRating = "No rating yet";

    [ObservableProperty]
    private string itemsListed = "0";

    [ObservableProperty]
    private string rentalsCompleted = "0";

    [ObservableProperty]
    private string userReviewsTitle = "Reviews left for you";

    [ObservableProperty]
    private ObservableCollection<Review> userReviews = new();

    public bool HasUserReviews => UserReviews.Count > 0;

    public AboutViewModel(IAuthenticationService authService, IReviewRepository reviewRepository)
    {
        _authService = authService;
        _reviewRepository = reviewRepository;
        Title = AppInfo.Name;
        RefreshProfile();
    }

    partial void OnUserReviewsChanged(ObservableCollection<Review> value)
    {
        OnPropertyChanged(nameof(HasUserReviews));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        RefreshProfile();

        var user = _authService.CurrentUser;
        if (user == null)
        {
            UserReviews = new ObservableCollection<Review>();
            UserReviewsTitle = "Reviews left for you";
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var result = await _reviewRepository.GetForUserAsync(user.Id, page: 1, pageSize: 10);
            UserReviews = new ObservableCollection<Review>(result.Items);
            UserReviewsTitle = result.TotalItems == 1
                ? "Reviews left for you (1)"
                : $"Reviews left for you ({result.TotalItems})";
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

    private void RefreshProfile()
    {
        var user = _authService.CurrentUser;
        UserName = string.IsNullOrWhiteSpace(user?.FullName) ? "Not signed in" : user.FullName;
        UserEmail = user?.Email ?? string.Empty;
        AverageRating = user?.AverageRating == null ? "No rating yet" : $"{user.AverageRating:0.0}/5";
        ItemsListed = user == null ? "0" : user.ItemsListed.ToString();
        RentalsCompleted = user == null ? "0" : user.RentalsCompleted.ToString();
    }
}
