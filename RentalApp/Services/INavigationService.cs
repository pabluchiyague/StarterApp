namespace RentalApp.Services;

/// <summary>
/// Abstraction over Shell navigation. Exists so ViewModels can depend on an
/// interface rather than calling <c>Shell.Current</c> directly — which matters
/// for unit testing (you can swap in a mock) and for keeping ViewModels free
/// of MAUI UI types.
/// </summary>
public interface INavigationService
{
    /// <summary>Navigate to a registered Shell route (e.g. "items", "about").</summary>
    Task NavigateToAsync(string route);

    /// <summary>Navigate to a route passing query parameters.</summary>
    Task NavigateToAsync(string route, Dictionary<string, object> parameters);

    /// <summary>Pop one page off the navigation stack.</summary>
    Task NavigateBackAsync();

    /// <summary>Jump to the application root.</summary>
    Task NavigateToRootAsync();

    /// <summary>Pop every modal/pushed page back to the Shell root.</summary>
    Task PopToRootAsync();
}
