using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class LeaveReviewPage : ContentPage
{
    /// <summary>
    /// This creates the leave-review page and connects the XAML bindings to
    /// the injected view-model.
    /// </summary>
    public LeaveReviewPage(LeaveReviewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
