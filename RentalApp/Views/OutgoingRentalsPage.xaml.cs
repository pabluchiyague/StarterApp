using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class OutgoingRentalsPage : ContentPage
{
    private readonly OutgoingRentalsViewModel _viewModel;

    /// <summary>
    /// This creates the outgoing rentals page, stores its view-model, and
    /// connects the XAML bindings to that view-model.
    /// </summary>
    public OutgoingRentalsPage(OutgoingRentalsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>
    /// This reloads outgoing rentals whenever the page appears so status
    /// changes made by owners are visible to the borrower.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
