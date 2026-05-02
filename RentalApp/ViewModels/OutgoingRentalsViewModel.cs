using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Services;
using RentalApp.Views;

namespace RentalApp.ViewModels;

public partial class OutgoingRentalsViewModel : BaseViewModel
{
    private readonly IRentalRepository _rentalRepository;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private ObservableCollection<Rental> rentals = new();

    [ObservableProperty]
    private string emptyMessage = "You have not requested any rentals yet.";

    /// <summary>
    /// This stores the services used to load rentals requested by the current
    /// user, update borrower-driven status transitions, and navigate to review
    /// creation.
    /// </summary>
    public OutgoingRentalsViewModel(
        IRentalRepository rentalRepository,
        IAuthenticationService authService,
        INavigationService navigation)
    {
        _rentalRepository = rentalRepository;
        _authService = authService;
        _navigation = navigation;
        Title = "My rentals";
    }

    /// <summary>
    /// This loads rentals requested by the signed-in user from the active
    /// repository and displays the newest requests first.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_authService.CurrentUser == null)
        {
            SetError("You must be signed in to view your rentals.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var outgoing = await _rentalRepository.GetOutgoingForBorrowerAsync(_authService.CurrentUser.Id);
            Rentals = new ObservableCollection<Rental>(outgoing);
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
    /// This marks a rental as returned when the borrower has given the item
    /// back to the owner.
    /// </summary>
    [RelayCommand]
    private async Task MarkReturnedAsync(Rental rental)
    {
        if (rental == null || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await _rentalRepository.UpdateStatusAsync(rental.Id, RentalStatus.Returned);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    /// <summary>
    /// This opens the review form for a completed rental so the borrower can
    /// rate the owner/item experience.
    /// </summary>
    [RelayCommand]
    private async Task LeaveReviewAsync(Rental rental)
    {
        if (rental == null)
        {
            return;
        }

        await _navigation.NavigateToAsync(nameof(LeaveReviewPage), new Dictionary<string, object>
        {
            ["RentalId"] = rental.Id
        });
    }
}
