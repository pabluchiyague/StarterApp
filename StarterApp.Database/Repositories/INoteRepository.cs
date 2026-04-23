using StarterApp.Database.Models;

namespace StarterApp.Database.Repositories;

/// <summary>
/// Repository interface for Note and Category data access.
/// Abstracts the data source (local database, REST API, cache, etc.)
/// </summary>
public interface INoteRepository
{
    // ==================== Note Operations ====================

    /// <summary>
    /// Get all notes, optionally filtered by category
    /// </summary>
    /// <param name="categoryId">Filter by category ID. Null returns all notes.</param>
    /// <returns>List of notes with category information included</returns>
    Task<List<Note>> GetAllNotesAsync(int? categoryId = null);

    /// <summary>
    /// Get a single note by ID
    /// </summary>
    /// <param name="id">Note ID</param>
    /// <returns>Note with category information, or null if not found</returns>
    Task<Note?> GetNoteByIdAsync(int id);

    /// <summary>
    /// Create a new note
    /// </summary>
    /// <param name="note">Note to create (Id will be generated)</param>
    /// <returns>Created note with generated ID</returns>
    Task<Note> CreateNoteAsync(Note note);

    /// <summary>
    /// Update an existing note
    /// </summary>
    /// <param name="note">Note with updated properties</param>
    /// <returns>Updated note, or null if not found</returns>
    Task<Note?> UpdateNoteAsync(Note note);

    /// <summary>
    /// Delete a note by ID
    /// </summary>
    /// <param name="id">Note ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteNoteAsync(int id);

    // ==================== Category Operations ====================

    /// <summary>
    /// Get all categories
    /// </summary>
    /// <returns>List of all categories ordered by name</returns>
    Task<List<Category>> GetAllCategoriesAsync();

    /// <summary>
    /// Get a single category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Category or null if not found</returns>
    Task<Category?> GetCategoryByIdAsync(int id);

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="category">Category to create</param>
    /// <returns>Created category with generated ID</returns>
    Task<Category> CreateCategoryAsync(Category category);

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="category">Category with updated properties</param>
    /// <returns>Updated category, or null if not found</returns>
    Task<Category?> UpdateCategoryAsync(Category category);

    /// <summary>
    /// Delete a category by ID
    /// </summary>
    /// <param name="id">Category ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <remarks>
    /// Notes in this category will have CategoryId set to NULL (based on DbContext configuration)
    /// </remarks>
    Task<bool> DeleteCategoryAsync(int id);
}