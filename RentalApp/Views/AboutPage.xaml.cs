namespace RentalApp.Views;

public partial class AboutPage : ContentPage
{
	private readonly ViewModels.AboutViewModel _viewModel;

	public AboutPage(ViewModels.AboutViewModel viewModel)
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
