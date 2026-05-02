using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Services;
using RentalApp.Views;

namespace RentalApp.ViewModels;

public partial class NearbyItemsViewModel : BaseViewModel
{
    private readonly IItemRepository _itemRepository;
    private readonly ILocationService _locationService;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private ObservableCollection<Item> items = new();

    [ObservableProperty]
    private double radiusKm = 5;

    [ObservableProperty]
    private string searchSummary = "Use your current location to find nearby items.";

    public NearbyItemsViewModel(
        IItemRepository itemRepository,
        ILocationService locationService,
        INavigationService navigation)
    {
        _itemRepository = itemRepository;
        _locationService = locationService;
        _navigation = navigation;
        Title = "Near me";
    }

    /// <summary>
    /// This gets the current device location through ILocationService and asks
    /// the repository for items inside the selected radius.
    /// </summary>
    [RelayCommand]
    private async Task FindNearMeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var location = await _locationService.GetCurrentLocationAsync();
            if (location == null)
            {
                SetError("Location permission was not granted or GPS is unavailable.");
                return;
            }

            var result = await _itemRepository.GetNearbyAsync(
                location.Value.Latitude,
                location.Value.Longitude,
                RadiusKm);

            Items = new ObservableCollection<Item>(result.Items);
            SearchSummary = $"Found {result.TotalResults} item(s) within {result.RadiusKm:0.#}km.";
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
    /// This opens the selected nearby item on the detail page.
    /// </summary>
    [RelayCommand]
    private async Task OpenItemAsync(Item item)
    {
        if (item == null)
        {
            return;
        }

        await _navigation.NavigateToAsync(nameof(ItemDetailPage), new Dictionary<string, object>
        {
            ["ItemId"] = item.Id
        });
    }
}
