using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarterApp.Database.Models;
using StarterApp.Database.Repositories;
using System.Collections.ObjectModel;

namespace StarterApp.ViewModels;

/// <summary>
/// ViewModel for displaying list of all notes
/// </summary>
public partial class NotesViewModel : BaseViewModel
{
    private readonly INoteRepository _repository;

    [ObservableProperty]
    private ObservableCollection<Note> notes = new();

    [ObservableProperty]
    private List<Category> categories = new();

    [ObservableProperty]
    private int? selectedCategoryId;

    [ObservableProperty]
    private bool isRefreshing;

    public NotesViewModel(INoteRepository repository)
    {
        _repository = repository;
        Title = "My Notes";
    }

    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await LoadNotesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var allCategories = await _repository.GetAllCategoriesAsync();

            Categories = new List<Category>
            {
                new Category { Id = 0, Name = "All Categories" }
            };
            Categories.AddRange(allCategories);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load categories: {ex.Message} | Inner: {ex.InnerException?.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadNotesAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var notesList = await _repository.GetAllNotesAsync(SelectedCategoryId);

            Notes.Clear();
            foreach (var note in notesList)
            {
                Notes.Add(note);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load notes: {ex.Message} | Inner: {ex.InnerException?.Message}";
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task AddNoteAsync()
    {
        await Shell.Current.GoToAsync("note");
    }

    [RelayCommand]
    private async Task EditNoteAsync(Note note)
    {
        if (note == null) return;
        await Shell.Current.GoToAsync($"note?id={note.Id}");
    }

    [RelayCommand]
    private async Task DeleteNoteAsync(Note note)
    {
        if (note == null) return;

        bool confirm = await Application.Current.MainPage.DisplayAlert(
            "Delete Note",
            $"Are you sure you want to delete '{note.Title}'?",
            "Delete",
            "Cancel");

        if (!confirm) return;

        try
        {
            IsBusy = true;

            var deleted = await _repository.DeleteNoteAsync(note.Id);

            if (deleted)
            {
                Notes.Remove(note);
            }
            else
            {
                ErrorMessage = "Note not found";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete note: {ex.Message} | Inner: {ex.InnerException?.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadNotesAsync();
    }

    partial void OnSelectedCategoryIdChanged(int? value)
    {
        _ = LoadNotesAsync();
    }
}