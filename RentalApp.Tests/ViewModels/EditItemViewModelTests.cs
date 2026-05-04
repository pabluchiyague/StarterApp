using RentalApp.Database.Models;
using RentalApp.ViewModels;

namespace RentalApp.Tests;

public class EditItemViewModelTests
{
    [Fact]
    public async Task LoadAsync_OwnerItem_PopulatesEditableFields()
    {
        var repository = new FakeItemRepository
        {
            ItemById = new Item
            {
                Id = 12,
                Title = "Camera tripod",
                Description = "Lightweight tripod",
                DailyRate = 6.25m,
                CategoryId = 2,
                OwnerId = 7,
                IsAvailable = false,
                Latitude = 55.9533,
                Longitude = -3.1883
            }
        };
        var viewModel = new EditItemViewModel(
            repository,
            new FakeAuthenticationService { CurrentUser = new User { Id = 7 } },
            new FakeNavigationService())
        {
            ItemId = 12
        };

        await viewModel.LoadAsync();

        Assert.Equal("Camera tripod", viewModel.TitleText);
        Assert.Equal("Lightweight tripod", viewModel.Description);
        Assert.Equal("6.25", viewModel.DailyRateText);
        Assert.Equal("55.9533", viewModel.LatitudeText);
        Assert.Equal("-3.1883", viewModel.LongitudeText);
        Assert.False(viewModel.IsAvailable);
        Assert.Equal(2, viewModel.SelectedCategory!.Id);
    }

    [Fact]
    public async Task LoadAsync_NonOwnerItem_SetsOwnerError()
    {
        var repository = new FakeItemRepository
        {
            ItemById = new Item { Id = 12, Title = "Camera tripod", CategoryId = 1, OwnerId = 99 }
        };
        var viewModel = new EditItemViewModel(
            repository,
            new FakeAuthenticationService { CurrentUser = new User { Id = 7 } },
            new FakeNavigationService())
        {
            ItemId = 12
        };

        await viewModel.LoadAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Only the owner can edit this item.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveCommand_ValidForm_UpdatesAllEditableFieldsAndNavigatesBack()
    {
        var repository = new FakeItemRepository
        {
            ItemById = new Item
            {
                Id = 12,
                Title = "Old title",
                DailyRate = 4m,
                CategoryId = 1,
                OwnerId = 7,
                IsAvailable = true
            }
        };
        var navigation = new FakeNavigationService();
        var viewModel = new EditItemViewModel(
            repository,
            new FakeAuthenticationService { CurrentUser = new User { Id = 7 } },
            navigation)
        {
            ItemId = 12,
            SelectedCategory = repository.Categories[1],
            TitleText = "Updated camera kit",
            Description = "Updated description",
            DailyRateText = "11.25",
            IsAvailable = false,
            LatitudeText = "55.9533",
            LongitudeText = "-3.1883"
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(12, repository.LastUpdateId);
        Assert.NotNull(repository.LastUpdate);
        Assert.Equal("Updated camera kit", repository.LastUpdate!.Title);
        Assert.Equal("Updated description", repository.LastUpdate.Description);
        Assert.Equal(11.25m, repository.LastUpdate.DailyRate);
        Assert.Equal(repository.Categories[1].Id, repository.LastUpdate.CategoryId);
        Assert.False(repository.LastUpdate.IsAvailable!.Value);
        Assert.Equal(55.9533, repository.LastUpdate.Latitude);
        Assert.Equal(-3.1883, repository.LastUpdate.Longitude);
        Assert.Equal(1, navigation.BackCount);
        Assert.False(viewModel.HasError);
    }
}
