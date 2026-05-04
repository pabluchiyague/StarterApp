using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Models.Api;
using RentalApp.Services;
using RentalApp.Views;

namespace RentalApp.ViewModels;

[QueryProperty(nameof(ItemId), nameof(ItemId))]
public partial class ItemDetailViewModel : BaseViewModel
{
    private readonly IItemRepository _repository;
    private readonly IRentalRepository _rentalRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IAuthenticationService _auth;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private Item? item;

    [ObservableProperty]
    private int itemId;

    [ObservableProperty]
    private DateTime startDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private DateTime endDate = DateTime.Today.AddDays(2);

    [ObservableProperty]
    private string successMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Review> reviews = new();

    [ObservableProperty]
    private string reviewsTitle = "Reviews";

    public bool IsOwner => Item != null && _auth.CurrentUser?.Id == Item.OwnerId;

    public bool CanRequestRental => Item != null && !IsOwner && Item.IsAvailable;

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public bool HasReviews => Reviews.Count > 0;

    /// <summary>
    /// This stores repositories and authentication service used to load item
    /// details, decide ownership, and create rental requests.
    /// </summary>
    public ItemDetailViewModel(
        IItemRepository repository,
        IRentalRepository rentalRepository,
        IReviewRepository reviewRepository,
        IAuthenticationService auth,
        INavigationService navigation)
    {
        _repository = repository;
        _rentalRepository = rentalRepository;
        _reviewRepository = reviewRepository;
        _auth = auth;
        _navigation = navigation;
        Title = "Item details";
    }

    /// <summary>
    /// This refreshes the visible success state when the success text changes.
    /// </summary>
    partial void OnSuccessMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasSuccess));
    }

    /// <summary>
    /// This refreshes the visible review state when the review collection is
    /// replaced after loading from the repository.
    /// </summary>
    partial void OnReviewsChanged(ObservableCollection<Review> value)
    {
        OnPropertyChanged(nameof(HasReviews));
    }

    /// <summary>
    /// This reacts to Shell navigation by loading the item whose id was passed
    /// in the route parameters.
    /// </summary>
    partial void OnItemIdChanged(int value)
    {
        _ = LoadAsync();
    }

    /// <summary>
    /// This loads the selected item from the repository and updates the owner
    /// visibility state used by the detail page.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (ItemId <= 0 || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            Item = await _repository.GetItemByIdAsync(ItemId);
            if (Item == null)
            {
                SetError("Item not found.");
            }

            OnPropertyChanged(nameof(IsOwner));
            OnPropertyChanged(nameof(CanRequestRental));

            if (Item != null)
            {
                await LoadReviewsAsync();
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
    /// This loads the latest public reviews for the current item so borrowers
    /// can see previous feedback before requesting a rental.
    /// </summary>
    private async Task LoadReviewsAsync()
    {
        if (Item == null)
        {
            Reviews = new ObservableCollection<Review>();
            ReviewsTitle = "Reviews";
            return;
        }

        var result = await _reviewRepository.GetForItemAsync(Item.Id, page: 1, pageSize: 10);
        Reviews = new ObservableCollection<Review>(result.Items);
        ReviewsTitle = result.TotalItems == 1
            ? "Reviews (1)"
            : $"Reviews ({result.TotalItems})";
    }

    /// <summary>
    /// This opens the public profile for the owner of the current item.
    /// </summary>
    [RelayCommand]
    private async Task ViewOwnerProfileAsync()
    {
        if (Item?.OwnerId is not > 0)
        {
            SetError("Owner profile is not available for this item.");
            return;
        }

        await _navigation.NavigateToAsync(nameof(UserProfilePage), new Dictionary<string, object>
        {
            ["UserId"] = Item.OwnerId
        });
    }

    [RelayCommand]
    private async Task EditItemAsync()
    {
        if (Item == null || !IsOwner)
        {
            return;
        }

        await _navigation.NavigateToAsync(nameof(EditItemPage), new Dictionary<string, object>
        {
            ["ItemId"] = Item.Id
        });
    }

    /// <summary>
    /// This toggles availability for the current item when the signed-in user
    /// is the owner.
    /// </summary>
    [RelayCommand]
    private async Task ToggleAvailabilityAsync()
    {
        if (Item == null || !IsOwner)
        {
            return;
        }

        var updated = await _repository.UpdateItemAsync(Item.Id, new UpdateItemRequest
        {
            IsAvailable = !Item.IsAvailable
        });

        if (updated != null)
        {
            Item = updated;
            OnPropertyChanged(nameof(IsOwner));
            OnPropertyChanged(nameof(CanRequestRental));
        }
    }

    /// <summary>
    /// This sends a rental request for the current item to the API using the
    /// selected date range.
    /// </summary>
    [RelayCommand]
    private async Task RequestRentalAsync()
    {
        if (Item == null)
        {
            SetError("Item not found.");
            return;
        }

        if (_auth.CurrentUser == null)
        {
            SetError("You must be signed in to request a rental.");
            return;
        }

        if (IsOwner)
        {
            SetError("You cannot rent your own item.");
            return;
        }

        if (!Item.IsAvailable)
        {
            SetError("This item is not currently available.");
            return;
        }

        if (StartDate.Date < DateTime.Today)
        {
            SetError("Start date must be today or later.");
            return;
        }

        if (EndDate.Date <= StartDate.Date)
        {
            SetError("End date must be after the start date.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            SuccessMessage = string.Empty;

            var rental = await _rentalRepository.CreateAsync(new Rental
            {
                ItemId = Item.Id,
                BorrowerId = _auth.CurrentUser.Id,
                StartDate = StartDate.Date,
                EndDate = EndDate.Date,
                Status = RentalStatus.Requested
            });

            SuccessMessage = $"Request sent. Status: {rental.Status}. Total: £{rental.TotalPrice:0.00}.";
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
