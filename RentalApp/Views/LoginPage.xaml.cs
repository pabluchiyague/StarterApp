using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class LoginPage : ContentPage
{
    /// <summary>
    /// This creates the login page and attaches the injected login view-model
    /// so the XAML fields and commands talk to the authentication workflow.
    /// </summary>
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// This focuses the email field when the page appears so the user can
    /// immediately enter their own live API account details.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Dispatcher.Dispatch(() => EmailEntry.Focus());
    }
}
