using RentalApp.Views;

namespace RentalApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Routes for pages that aren't direct children of the Shell.
        // (login, notes, about are registered via <ShellContent Route="..."/>
        // in AppShell.xaml — RegisterPage is pushed onto the nav stack from
        // LoginPage so it needs an explicit route registration here.)
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
    }
}
