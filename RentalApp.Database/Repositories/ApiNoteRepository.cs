using RentalApp.Database.Models;
using System.Net.Http.Json;

namespace RentalApp.Database.Repositories;

/// <summary>
/// Future implementation: Repository using REST API for data access.
/// This class is a skeleton showing how to implement API-based data access
/// without changing ViewModels.
/// </summary>
/// <remarks>
/// To enable API mode:
/// 1. Implement all methods in this class
/// 2. Add HttpClient configuration in MauiProgram.cs
/// 3. Change repository registration to use ApiNoteRepository
/// 4. ViewModels remain unchanged!
/// </remarks>
public class ApiNoteRepository : INoteRepository
{
    private readonly HttpClient _httpClient;
    private const string NotesEndpoint = "api/notes";
    private const string CategoriesEndpoint = "api/categories";

    public ApiNoteRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ==================== Note Operations ====================

    public async Task<List<Note>> GetAllNotesAsync(int? categoryId = null)
    {
        // TODO: Implement API call
        // Example implementation:
        /*
        var url = categoryId.HasValue
            ? $"{NotesEndpoint}?categoryId={categoryId}"
            : NotesEndpoint;

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        return notes ?? new List<Note>();
        */

        throw new NotImplementedException("API integration pending");
    }

    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        // TODO: Implement
        // Example: GET api/notes/{id}
        /*
        var response = await _httpClient.GetAsync($"{NotesEndpoint}/{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Note>();
        */

        throw new NotImplementedException("API integration pending");
    }

    public async Task<Note> CreateNoteAsync(Note note)
    {
        // TODO: Implement
        // Example: POST api/notes
        /*
        var response = await _httpClient.PostAsJsonAsync(NotesEndpoint, note);
        response.EnsureSuccessStatusCode();

        var createdNote = await response.Content.ReadFromJsonAsync<Note>();
        return createdNote!;
        */

        throw new NotImplementedException("API integration pending");
    }

    public async Task<Note?> UpdateNoteAsync(Note note)
    {
        // TODO: Implement
        // Example: PUT api/notes/{id}
        /*
        var response = await _httpClient.PutAsJsonAsync($"{NotesEndpoint}/{note.Id}", note);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Note>();
        */

        throw new NotImplementedException("API integration pending");
    }

    public async Task<bool> DeleteNoteAsync(int id)
    {
        // TODO: Implement
        // Example: DELETE api/notes/{id}
        /*
        var response = await _httpClient.DeleteAsync($"{NotesEndpoint}/{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
        */

        throw new NotImplementedException("API integration pending");
    }

    // ==================== Category Operations ====================

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        // TODO: Implement
        // Example: GET api/categories
        throw new NotImplementedException("API integration pending");
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        // TODO: Implement
        throw new NotImplementedException("API integration pending");
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        // TODO: Implement
        throw new NotImplementedException("API integration pending");
    }

    public async Task<Category?> UpdateCategoryAsync(Category category)
    {
        // TODO: Implement
        throw new NotImplementedException("API integration pending");
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        // TODO: Implement
        throw new NotImplementedException("API integration pending");
    }
}
