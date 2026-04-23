using StarterApp.Views;

namespace StarterApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("note", typeof(NotePage));
    }
}
