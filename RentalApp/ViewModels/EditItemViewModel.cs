using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Models.Api;
using RentalApp.Services;

namespace RentalApp.ViewModels;

[QueryProperty(nameof(ItemId), nameof(ItemId))]
public partial class EditItemViewModel : BaseViewModel
{
    private readonly IItemRepository _repository;
    private readonly IAuthenticationService _auth;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private int itemId;

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
    private string latitudeText = string.Empty;

    [ObservableProperty]
    private string longitudeText = string.Empty;

    [ObservableProperty]
    private bool isAvailable = true;

    public EditItemViewModel(
        IItemRepository repository,
        IAuthenticationService auth,
        INavigationService navigation)
    {
        _repository = repository;
        _auth = auth;
        _navigation = navigation;
        Title = "Edit item";
    }

    partial void OnItemIdChanged(int value)
    {
        _ = LoadAsync();
    }

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

            if (Categories.Count == 0)
            {
                Categories = new ObservableCollection<Category>(await _repository.GetAllCategoriesAsync());
            }

            var item = await _repository.GetItemByIdAsync(ItemId);
            if (item == null)
            {
                SetError("Item not found.");
                return;
            }

            if (_auth.CurrentUser?.Id != item.OwnerId)
            {
                SetError("Only the owner can edit this item.");
                return;
            }

            TitleText = item.Title;
            Description = item.Description ?? string.Empty;
            DailyRateText = item.DailyRate.ToString("0.00", CultureInfo.CurrentCulture);
            LatitudeText = item.Latitude?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            LongitudeText = item.Longitude?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            IsAvailable = item.IsAvailable;
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == item.CategoryId)
                ?? Categories.FirstOrDefault();
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

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (ItemId <= 0)
        {
            SetError("Item not found.");
            return;
        }

        if (_auth.CurrentUser == null)
        {
            SetError("You must be signed in to edit an item.");
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleText) || TitleText.Trim().Length < 5)
        {
            SetError("Title must be at least 5 characters.");
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

            var updated = await _repository.UpdateItemAsync(ItemId, new UpdateItemRequest
            {
                Title = TitleText.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                DailyRate = dailyRate,
                CategoryId = SelectedCategory.Id,
                IsAvailable = IsAvailable,
                Latitude = latitude,
                Longitude = longitude
            });

            if (updated == null)
            {
                SetError("Item not found.");
                return;
            }

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
