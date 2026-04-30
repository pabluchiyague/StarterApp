namespace RentalApp.Services;

/// <summary>
/// Default <see cref="INavigationService"/> implementation. Each method is a
/// thin wrapper over <c>Shell.Current</c> so the rest of the app can navigate
/// without taking a hard dependency on MAUI's Shell API.
/// </summary>
public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    public async Task NavigateToAsync(string route, Dictionary<string, object> parameters)
    {
        await Shell.Current.GoToAsync(route, parameters);
    }

    public async Task NavigateBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public async Task NavigateToRootAsync()
    {
        // Root of the note-taking app is the Notes list page.
        // (Was "//login" in the original auth-focused RentalApp.)
        await Shell.Current.GoToAsync("//notes");
    }

    public async Task PopToRootAsync()
    {
        await Shell.Current.Navigation.PopToRootAsync();
    }
}
