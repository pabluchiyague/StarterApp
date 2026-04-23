using StarterApp.ViewModels;

namespace StarterApp.Views;

public partial class NotePage : ContentPage
{
    private readonly NoteViewModel _viewModel;

    public NotePage(NoteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Parse query parameter if navigating with ID
        if (BindingContext is NoteViewModel vm)
        {
            var idParam = this.GetQueryParameter("id");
            int? noteId = null;

            if (!string.IsNullOrEmpty(idParam) && int.TryParse(idParam, out int id))
            {
                noteId = id;
            }

            await vm.InitializeAsync(noteId);
        }
    }

    private string GetQueryParameter(string key)
    {
        if (Shell.Current.CurrentState.Location.OriginalString.Contains($"{key}="))
        {
            var query = Shell.Current.CurrentState.Location.OriginalString.Split('?')[1];
            var pairs = query.Split('&');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts[0] == key)
                    return parts[1];
            }
        }
        return null;
    }
}
