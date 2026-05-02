using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class UserProfilePage : ContentPage
{
    private readonly UserProfileViewModel _viewModel;

    public UserProfilePage(UserProfileViewModel viewModel)
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
