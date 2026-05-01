using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class ItemDetailPage : ContentPage
{
    /// <summary>
    /// This creates the item detail page and connects it to the injected
    /// detail view-model.
    /// </summary>
    public ItemDetailPage(ItemDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
