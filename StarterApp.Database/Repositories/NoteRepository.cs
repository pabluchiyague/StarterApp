using Microsoft.EntityFrameworkCore;
using StarterApp.Database.Data;
using StarterApp.Database.Models;

namespace StarterApp.Database.Repositories;

/// <summary>
/// Implementation of INoteRepository using Entity Framework Core and PostgreSQL.
/// Provides local database persistence for notes and categories.
/// </summary>
public class NoteRepository : INoteRepository
{
    private readonly AppDbContext _context;

    public NoteRepository(AppDbContext context)
    {
        _context = context;
    }

    // ==================== Note Operations ====================

    /// <inheritdoc/>
    public async Task<List<Note>> GetAllNotesAsync(int? categoryId = null)
    {
        try
        {
            IQueryable<Note> query = _context.Notes.Include(n => n.Category);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(n => n.CategoryId == categoryId.Value);
            }

            var notes = await query
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            return notes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading notes: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        try
        {
            var note = await _context.Notes
                .Include(n => n.Category)
                .FirstOrDefaultAsync(n => n.Id == id);

            return note;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading note {id}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Note> CreateNoteAsync(Note note)
    {
        try
        {
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            if (note.CategoryId.HasValue)
            {
                await _context.Entry(note)
                    .Reference(n => n.Category)
                    .LoadAsync();
            }

            return note;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating note: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Note?> UpdateNoteAsync(Note note)
    {
        try
        {
            var existingNote = await _context.Notes.FindAsync(note.Id);
            if (existingNote == null)
            {
                return null;
            }

            existingNote.Title = note.Title;
            existingNote.Content = note.Content;
            existingNote.CategoryId = note.CategoryId;
            existingNote.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _context.Entry(existingNote)
                .Reference(n => n.Category)
                .LoadAsync();

            return existingNote;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating note {note.Id}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteNoteAsync(int id)
    {
        try
        {
            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return false;
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting note {id}: {ex.Message}");
            throw;
        }
    }

    // ==================== Category Operations ====================

    /// <inheritdoc/>
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return categories;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading categories: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        try
        {
            var category = await _context.Categories
                .Include(c => c.Notes)
                .FirstOrDefaultAsync(c => c.Id == id);

            return category;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading category {id}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Category> CreateCategoryAsync(Category category)
    {
        try
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return category;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating category: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Category?> UpdateCategoryAsync(Category category)
    {
        try
        {
            var existingCategory = await _context.Categories.FindAsync(category.Id);
            if (existingCategory == null)
            {
                return null;
            }

            existingCategory.Name = category.Name;
            existingCategory.ColorHex = category.ColorHex;
            existingCategory.Description = category.Description;

            await _context.SaveChangesAsync();

            return existingCategory;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating category {category.Id}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return false;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting category {id}: {ex.Message}");
            throw;
        }
    }
}