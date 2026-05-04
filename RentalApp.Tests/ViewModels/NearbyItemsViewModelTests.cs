using RentalApp.Database.Models;
using RentalApp.ViewModels;

namespace RentalApp.Tests;

public class NearbyItemsViewModelTests
{
    [Fact]
    public async Task FindNearMeCommand_MockedLocationService_UsesInjectedGpsCoordinates()
    {
        var repository = new FakeItemRepository
        {
            NearbyResult = new NearbySearchResult([], 55.95, -3.18, 7.5, 0)
        };
        var location = new FakeLocationService { CurrentLocation = (55.95, -3.18) };
        var viewModel = new NearbyItemsViewModel(repository, location, new FakeNavigationService())
        {
            RadiusKm = 7.5
        };

        await viewModel.FindNearMeCommand.ExecuteAsync(null);

        Assert.Equal(1, location.CallCount);
        Assert.Equal(55.95, repository.LastNearbyCall!.Value.Latitude);
        Assert.Equal(-3.18, repository.LastNearbyCall.Value.Longitude);
        Assert.Equal(7.5, repository.LastNearbyCall.Value.RadiusKm);
    }

    [Fact]
    public async Task FindNearMeCommand_LocationAvailable_LoadsNearbyItems()
    {
        var repository = new FakeItemRepository
        {
            NearbyResult = new NearbySearchResult(
                [new Item { Id = 5, Title = "Nearby drill", DistanceKm = 1.2 }],
                55.9533,
                -3.1883,
                10,
                1)
        };
        var location = new FakeLocationService { CurrentLocation = (55.9533, -3.1883) };
        var viewModel = new NearbyItemsViewModel(repository, location, new FakeNavigationService())
        {
            RadiusKm = 10
        };

        await viewModel.FindNearMeCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Items);
        Assert.Equal("Nearby drill", viewModel.Items[0].Title);
        Assert.Equal(55.9533, repository.LastNearbyCall!.Value.Latitude);
        Assert.Equal(-3.1883, repository.LastNearbyCall.Value.Longitude);
        Assert.Equal(10, repository.LastNearbyCall.Value.RadiusKm);
        Assert.Equal("Found 1 item(s) within 10km.", viewModel.SearchSummary);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task FindNearMeCommand_NoLocation_SetsError()
    {
        var repository = new FakeItemRepository();
        var viewModel = new NearbyItemsViewModel(
            repository,
            new FakeLocationService { CurrentLocation = null },
            new FakeNavigationService());

        await viewModel.FindNearMeCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Location permission was not granted or GPS is unavailable.", viewModel.ErrorMessage);
        Assert.Null(repository.LastNearbyCall);
    }

    [Fact]
    public async Task OpenItemCommand_ItemSelected_NavigatesToDetailWithItemId()
    {
        var navigation = new FakeNavigationService();
        var viewModel = new NearbyItemsViewModel(
            new FakeItemRepository(),
            new FakeLocationService(),
            navigation);

        await viewModel.OpenItemCommand.ExecuteAsync(new Item { Id = 5, Title = "Nearby drill" });

        Assert.Equal("ItemDetailPage", navigation.LastRoute);
        Assert.Equal(5, navigation.LastParameters!["ItemId"]);
    }
}
