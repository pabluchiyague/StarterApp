using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class CreateItemViewModel : BaseViewModel
{
    private readonly IItemRepository _repository;
    private readonly IAuthenticationService _auth;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private ObservableCollection<Category> categories = new();

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private string titleText = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string dailyRateText = string.Empty;

    [ObservableProperty]
    private string latitudeText = "55.9533";

    [ObservableProperty]
    private string longitudeText = "-3.1883";

    /// <summary>
    /// This stores repository, authentication, and navigation services used to
    /// create a new item for the signed-in user.
    /// </summary>
    public CreateItemViewModel(
        IItemRepository repository,
        IAuthenticationService auth,
        INavigationService navigation)
    {
        _repository = repository;
        _auth = auth;
        _navigation = navigation;
        Title = "List an item";
    }

    /// <summary>
    /// This loads categories so the create-item form can send the API-required
    /// category id with the new listing.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();
            Categories = new ObservableCollection<Category>(await _repository.GetAllCategoriesAsync());
            SelectedCategory ??= Categories.FirstOrDefault();
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
    /// This validates the form, creates the item through the active repository,
    /// and returns to the previous page when the save succeeds.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_auth.CurrentUser == null)
        {
            SetError("You must be signed in to list an item.");
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleText))
        {
            SetError("Title is required.");
            return;
        }

        if (SelectedCategory == null)
        {
            SetError("Choose a category.");
            return;
        }

        if (!decimal.TryParse(DailyRateText, NumberStyles.Currency, CultureInfo.CurrentCulture, out var dailyRate) ||
            dailyRate <= 0)
        {
            SetError("Enter a valid daily rate.");
            return;
        }

        if (!double.TryParse(LatitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            latitude < -90 ||
            latitude > 90)
        {
            SetError("Enter a latitude between -90 and 90.");
            return;
        }

        if (!double.TryParse(LongitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude) ||
            longitude < -180 ||
            longitude > 180)
        {
            SetError("Enter a longitude between -180 and 180.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var item = new Item
            {
                Title = TitleText.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                DailyRate = dailyRate,
                CategoryId = SelectedCategory.Id,
                OwnerId = _auth.CurrentUser.Id,
                IsAvailable = true,
                Latitude = latitude,
                Longitude = longitude
            };

            await _repository.CreateItemAsync(item);
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
