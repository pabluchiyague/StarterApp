using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class ItemsListPage : ContentPage
{
    private readonly ItemsListViewModel _viewModel;

    /// <summary>
    /// This creates the browse page, stores its view-model, and connects the
    /// XAML bindings to that view-model.
    /// </summary>
    public ItemsListPage(ItemsListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>
    /// This refreshes the item list whenever the page appears so newly created
    /// listings are visible after navigating back.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
