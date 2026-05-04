using RentalApp.Database.Models;
using RentalApp.ViewModels;

namespace RentalApp.Tests;

public class CreateItemViewModelTests
{
    [Fact]
    public async Task LoadAsync_CategoriesAvailable_SelectsFirstCategory()
    {
        var repository = new FakeItemRepository();
        var viewModel = new CreateItemViewModel(
            repository,
            new FakeAuthenticationService { CurrentUser = new User { Id = 7 } },
            new FakeNavigationService());

        await viewModel.LoadAsync();

        Assert.Equal(repository.Categories.Count, viewModel.Categories.Count);
        Assert.Equal(repository.Categories[0].Id, viewModel.SelectedCategory!.Id);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task SaveCommand_ValidForm_CreatesItemAndNavigatesBack()
    {
        var repository = new FakeItemRepository();
        var navigation = new FakeNavigationService();
        var viewModel = new CreateItemViewModel(
            repository,
            new FakeAuthenticationService { CurrentUser = new User { Id = 7 } },
            navigation)
        {
            SelectedCategory = repository.Categories[1],
            TitleText = "Cordless drill",
            Description = "Battery included",
            DailyRateText = "8.50",
            LatitudeText = "55.9533",
            LongitudeText = "-3.1883"
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.NotNull(repository.CreatedItem);
        Assert.Equal("Cordless drill", repository.CreatedItem!.Title);
        Assert.Equal("Battery included", repository.CreatedItem.Description);
        Assert.Equal(8.50m, repository.CreatedItem.DailyRate);
        Assert.Equal(repository.Categories[1].Id, repository.CreatedItem.CategoryId);
        Assert.Equal(7, repository.CreatedItem.OwnerId);
        Assert.Equal(55.9533, repository.CreatedItem.Latitude);
        Assert.Equal(-3.1883, repository.CreatedItem.Longitude);
        Assert.Equal(1, navigation.BackCount);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task SaveCommand_InvalidLatitude_SetsErrorAndDoesNotCreateItem()
    {
        var repository = new FakeItemRepository();
        var viewModel = new CreateItemViewModel(
            repository,
            new FakeAuthenticationService { CurrentUser = new User { Id = 7 } },
            new FakeNavigationService())
        {
            SelectedCategory = repository.Categories[0],
            TitleText = "Cordless drill",
            DailyRateText = "8.50",
            LatitudeText = "120",
            LongitudeText = "-3.1883"
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Enter a latitude between -90 and 90.", viewModel.ErrorMessage);
        Assert.Null(repository.CreatedItem);
    }
}
