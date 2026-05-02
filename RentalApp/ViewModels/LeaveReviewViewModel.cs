using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Database.Services;
using RentalApp.Services;

namespace RentalApp.ViewModels;

[QueryProperty(nameof(RentalId), nameof(RentalId))]
public partial class LeaveReviewViewModel : BaseViewModel
{
    private readonly IRentalRepository _rentalRepository;
    private readonly ReviewService _reviewService;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private int rentalId;

    [ObservableProperty]
    private Rental? rental;

    [ObservableProperty]
    private int rating = 5;

    [ObservableProperty]
    private string comment = string.Empty;

    [ObservableProperty]
    private string successMessage = string.Empty;

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    /// <summary>
    /// This stores services used to load the target rental, validate review
    /// ownership locally, submit the review, and return to the rentals page.
    /// </summary>
    public LeaveReviewViewModel(
        IRentalRepository rentalRepository,
        ReviewService reviewService,
        IAuthenticationService authService,
        INavigationService navigation)
    {
        _rentalRepository = rentalRepository;
        _reviewService = reviewService;
        _authService = authService;
        _navigation = navigation;
        Title = "Leave review";
    }

    /// <summary>
    /// This refreshes the visible success state when review submission
    /// succeeds.
    /// </summary>
    partial void OnSuccessMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasSuccess));
    }

    /// <summary>
    /// This loads the rental passed through Shell navigation.
    /// </summary>
    partial void OnRentalIdChanged(int value)
    {
        _ = LoadAsync();
    }

    /// <summary>
    /// This loads the rental being reviewed so the form can display context
    /// and check whether the rental is completed.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (RentalId <= 0 || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            Rental = await _rentalRepository.GetByIdAsync(RentalId);
            if (Rental == null)
            {
                SetError("Rental not found.");
            }
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

    /// <summary>
    /// This validates the review form and sends the completed-rental review
    /// through the active review repository.
    /// </summary>
    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (Rental == null)
        {
            SetError("Rental not found.");
            return;
        }

        if (_authService.CurrentUser == null)
        {
            SetError("You must be signed in to leave a review.");
            return;
        }

        if (Rental.Status != RentalStatus.Completed)
        {
            SetError("You can only review completed rentals.");
            return;
        }

        if (Rating is < 1 or > 5)
        {
            SetError("Rating must be between 1 and 5.");
            return;
        }

        if (Comment.Length > 500)
        {
            SetError("Comment must be 500 characters or fewer.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            SuccessMessage = string.Empty;

            await _reviewService.SubmitReviewAsync(Rental.Id, _authService.CurrentUser.Id, Rating, Comment);

            SuccessMessage = "Review submitted.";
            await _navigation.NavigateBackAsync();
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
