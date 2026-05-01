using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class IncomingRequestsViewModel : BaseViewModel
{
    private readonly IRentalRepository _rentalRepository;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private ObservableCollection<Rental> rentals = new();

    [ObservableProperty]
    private string emptyMessage = "No rental requests yet.";

    /// <summary>
    /// This stores the rental repository and authentication service used to
    /// load and manage requests made against the signed-in user's items.
    /// </summary>
    public IncomingRequestsViewModel(
        IRentalRepository rentalRepository,
        IAuthenticationService authService)
    {
        _rentalRepository = rentalRepository;
        _authService = authService;
        Title = "Requests";
    }

    /// <summary>
    /// This loads incoming rental requests for items owned by the signed-in
    /// user and displays them newest first.
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
            SetError("You must be signed in to view rental requests.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var incoming = await _rentalRepository.GetIncomingForOwnerAsync(_authService.CurrentUser.Id);
            Rentals = new ObservableCollection<Rental>(incoming);
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
    /// This approves a requested rental and reloads the page so the new status
    /// is visible immediately.
    /// </summary>
    [RelayCommand]
    private async Task ApproveAsync(Rental rental)
    {
        await UpdateStatusAsync(rental, RentalStatus.Approved);
    }

    /// <summary>
    /// This rejects a requested rental and reloads the page so the new status
    /// is visible immediately.
    /// </summary>
    [RelayCommand]
    private async Task RejectAsync(Rental rental)
    {
        await UpdateStatusAsync(rental, RentalStatus.Rejected);
    }

    /// <summary>
    /// This marks an approved rental as out for rent when the owner has handed
    /// the item over to the borrower.
    /// </summary>
    [RelayCommand]
    private async Task MarkOutForRentAsync(Rental rental)
    {
        await UpdateStatusAsync(rental, RentalStatus.OutForRent);
    }

    /// <summary>
    /// This completes a returned rental after the owner confirms the item has
    /// come back in acceptable condition.
    /// </summary>
    [RelayCommand]
    private async Task CompleteAsync(Rental rental)
    {
        await UpdateStatusAsync(rental, RentalStatus.Completed);
    }

    /// <summary>
    /// This sends one owner status update to the repository and refreshes the
    /// incoming request list when the server accepts it.
    /// </summary>
    private async Task UpdateStatusAsync(Rental? rental, RentalStatus status)
    {
        if (rental == null || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            await _rentalRepository.UpdateStatusAsync(rental.Id, status);
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
}
