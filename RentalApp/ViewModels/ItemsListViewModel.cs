using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Services;
using RentalApp.Views;

namespace RentalApp.ViewModels;

public partial class ItemsListViewModel : BaseViewModel
{
    private readonly IItemRepository _repository;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private ObservableCollection<Item> items = new();

    [ObservableProperty]
    private ObservableCollection<Category> categories = new();

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int page = 1;

    [ObservableProperty]
    private int totalPages;

    /// <summary>
    /// This stores the item repository and navigation service used by the
    /// browse page.
    /// </summary>
    public ItemsListViewModel(IItemRepository repository, INavigationService navigation)
    {
        _repository = repository;
        _navigation = navigation;
        Title = "Browse items";
    }

    /// <summary>
    /// This loads categories and the current page of items using the selected
    /// category and search text.
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

            var categoriesResult = await _repository.GetAllCategoriesAsync();
            Categories = new ObservableCollection<Category>(categoriesResult);

            var result = await _repository.GetItemsAsync(
                SelectedCategory?.Slug,
                SearchText,
                Page,
                pageSize: 20);

            Items = new ObservableCollection<Item>(result.Items);
            TotalPages = result.TotalPages;
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
    /// This resets pagination and reloads items using the current search text.
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        Page = 1;
        await LoadAsync();
    }

    /// <summary>
    /// This clears the category and search filters, returns to page one, and
    /// reloads the item list.
    /// </summary>
    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SelectedCategory = null;
        SearchText = string.Empty;
        Page = 1;
        await LoadAsync();
    }

    /// <summary>
    /// This navigates from the list page to the selected item detail page.
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

    /// <summary>
    /// This navigates to the create-item page.
    /// </summary>
    [RelayCommand]
    private async Task CreateItemAsync()
    {
        await _navigation.NavigateToAsync(nameof(CreateItemPage));
    }
}
