using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class IncomingRequestsPage : ContentPage
{
    private readonly IncomingRequestsViewModel _viewModel;

    /// <summary>
    /// This creates the incoming requests page, stores its view-model, and
    /// connects the XAML bindings to that view-model.
    /// </summary>
    public IncomingRequestsPage(IncomingRequestsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>
    /// This reloads incoming requests whenever the page appears so owner
    /// decisions use the latest server state.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
