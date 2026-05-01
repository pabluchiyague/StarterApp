using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class CreateItemPage : ContentPage
{
    private readonly CreateItemViewModel _viewModel;

    /// <summary>
    /// This creates the item creation page, stores its view-model, and connects
    /// the XAML bindings to that view-model.
    /// </summary>
    public CreateItemPage(CreateItemViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>
    /// This loads form data whenever the page appears so category choices are
    /// available before the user saves a listing.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
