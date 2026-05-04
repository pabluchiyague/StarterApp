using RentalApp.Database.Models;
using RentalApp.ViewModels;

namespace RentalApp.Tests;

public class ItemsListViewModelTests
{
    [Fact]
    public async Task LoadAsync_WithSearchAndCategory_LoadsCategoriesAndItems()
    {
        var repository = new FakeItemRepository();
        repository.Items.Add(new Item { Id = 1, Title = "Cordless drill", CategoryId = 1 });
        var viewModel = new ItemsListViewModel(repository, new FakeNavigationService())
        {
            SelectedCategory = repository.Categories[0],
            SearchText = "drill",
            Page = 2
        };

        await viewModel.LoadAsync();

        Assert.Equal(repository.Categories.Count, viewModel.Categories.Count);
        Assert.Single(viewModel.Items);
        Assert.Equal("tools", repository.LastGetItemsCall!.Value.CategorySlug);
        Assert.Equal("drill", repository.LastGetItemsCall.Value.Search);
        Assert.Equal(2, repository.LastGetItemsCall.Value.Page);
        Assert.Equal(20, repository.LastGetItemsCall.Value.PageSize);
    }

    [Fact]
    public async Task OpenItemCommand_ItemSelected_NavigatesToDetailWithItemId()
    {
        var navigation = new FakeNavigationService();
        var viewModel = new ItemsListViewModel(new FakeItemRepository(), navigation);

        await viewModel.OpenItemCommand.ExecuteAsync(new Item { Id = 42, Title = "Tent" });

        Assert.Equal("ItemDetailPage", navigation.LastRoute);
        Assert.Equal(42, navigation.LastParameters!["ItemId"]);
    }

    [Fact]
    public async Task ClearFiltersCommand_ExistingFilters_ClearsAndReloadsFirstPage()
    {
        var repository = new FakeItemRepository();
        var viewModel = new ItemsListViewModel(repository, new FakeNavigationService())
        {
            SelectedCategory = repository.Categories[0],
            SearchText = "drill",
            Page = 3
        };

        await viewModel.ClearFiltersCommand.ExecuteAsync(null);

        Assert.Null(viewModel.SelectedCategory);
        Assert.Equal(string.Empty, viewModel.SearchText);
        Assert.Equal(1, viewModel.Page);
        Assert.Null(repository.LastGetItemsCall!.Value.CategorySlug);
        Assert.Equal(string.Empty, repository.LastGetItemsCall.Value.Search);
    }
}
