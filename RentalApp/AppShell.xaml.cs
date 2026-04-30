using RentalApp.Views;

namespace RentalApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("note", typeof(NotePage));
    }
}
