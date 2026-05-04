using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class EditItemPage : ContentPage
{
    private readonly EditItemViewModel _viewModel;

    public EditItemPage(EditItemViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
