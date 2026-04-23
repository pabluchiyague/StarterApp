using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarterApp.Database.Models;
using StarterApp.Database.Repositories;
using System.Linq;

namespace StarterApp.ViewModels;

/// <summary>
/// ViewModel for creating or editing a single note
/// </summary>
public partial class NoteViewModel : BaseViewModel
{
    private readonly INoteRepository _repository;
    private int? _noteId;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private List<Category> categories = new();

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private NoteImportance importance = NoteImportance.Normal; 

    public NoteViewModel(INoteRepository repository)
    {
        _repository = repository;
        Title = "New Note";
    }

    public async Task InitializeAsync(int? noteId = null)
    {
        try
        {
            IsBusy = true;

            Categories = await _repository.GetAllCategoriesAsync();

            if (noteId.HasValue)
            {
                _noteId = noteId.Value;
                IsEditMode = true;
                Title = "Edit Note";

                var note = await _repository.GetNoteByIdAsync(noteId.Value);
                if (note != null)
                {
                    this.title = note.Title;
                    this.content = note.Content;
                    this.selectedCategory = Categories.FirstOrDefault(c => c.Id == note.CategoryId);
                    this.importance = note.Importance;
                    OnPropertyChanged(nameof(Title));
                    OnPropertyChanged(nameof(Content));
                    OnPropertyChanged(nameof(SelectedCategory));
                    OnPropertyChanged(nameof(Importance));
                }
            }
            else
            {
                IsEditMode = false;
                Title = "New Note";
                Importance = NoteImportance.Normal;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message} | Inner: {ex.InnerException?.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title is required";
            return;
        }

        if (string.IsNullOrWhiteSpace(Content))
        {
            ErrorMessage = "Content is required";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (IsEditMode && _noteId.HasValue)
            {
                var note = new Note
                {
                    Id = _noteId.Value,
                    Title = Title,
                    Content = Content,
                    CategoryId = SelectedCategory?.Id,
                    Importance = Importance
                };

                await _repository.UpdateNoteAsync(note);
            }
            else
            {
                var note = new Note
                {
                    Title = Title,
                    Content = Content,
                    CategoryId = SelectedCategory?.Id,
                    Importance = Importance
                };

                await _repository.CreateNoteAsync(note);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message} | Inner: {ex.InnerException?.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsEditMode || !_noteId.HasValue)
            return;

        bool confirm = await Application.Current.MainPage.DisplayAlert(
            "Delete Note",
            "Are you sure you want to delete this note?",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;

            var deleted = await _repository.DeleteNoteAsync(_noteId.Value);

            if (deleted)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                ErrorMessage = "Note not found";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message} | Inner: {ex.InnerException?.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}