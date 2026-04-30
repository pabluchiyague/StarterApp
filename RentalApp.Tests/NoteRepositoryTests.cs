using RentalApp.Database.Models;
using RentalApp.Database.Repositories;

namespace RentalApp.Tests;

/// <summary>
/// Unit tests for NoteRepository. Each test follows the AAA structure
/// (Arrange / Act / Assert) and is named MethodName_Scenario_ExpectedBehaviour
/// per the xUnit / .NET conventions covered in Tutorial 6.
/// </summary>
public class NoteRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly NoteRepository _repository;

    public NoteRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.Seed();
        _repository = new NoteRepository(_fixture.TestDbContext);
    }

    [Fact]
    public async Task CreateNoteAsync_WithValidNote_ShouldAssignId()
    {
        // Arrange
        var note = new Note
        {
            Title = "Test note",
            Content = "I am a test note",
            Importance = NoteImportance.Normal
        };

        // Act
        var saved = await _repository.CreateNoteAsync(note);

        // Assert
        Assert.NotEqual(0, saved.Id);
    }

    [Fact]
    public async Task GetNoteByIdAsync_WhenNoteExists_ReturnsNote()
    {
        // Arrange — fixture already seeded a note; pull its id
        var seeded = (await _repository.GetAllNotesAsync(null)).First();

        // Act
        var fetched = await _repository.GetNoteByIdAsync(seeded.Id);

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal(seeded.Title, fetched!.Title);
    }

    [Fact]
    public async Task GetNoteByIdAsync_WhenNoteMissing_ReturnsNull()
    {
        // Act
        var fetched = await _repository.GetNoteByIdAsync(999_999);

        // Assert
        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteNoteAsync_WhenNoteExists_ReturnsTrueAndRemovesRow()
    {
        // Arrange
        var note = await _repository.CreateNoteAsync(new Note
        {
            Title = "To delete",
            Content = "Goodbye",
            Importance = NoteImportance.Low
        });

        // Act
        var deleted = await _repository.DeleteNoteAsync(note.Id);
        var afterwards = await _repository.GetNoteByIdAsync(note.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(afterwards);
    }

    [Fact]
    public async Task GetAllNotesAsync_FilteredByCategory_ReturnsOnlyMatching()
    {
        // Arrange
        var work = (await _repository.GetAllCategoriesAsync()).First(c => c.Name == "Work");

        // Make sure there is at least one Work-tagged note in the DB
        await _repository.CreateNoteAsync(new Note
        {
            Title = "Work-only note",
            Content = "Belongs to Work",
            CategoryId = work.Id,
            Importance = NoteImportance.Normal
        });

        // Act
        var notes = await _repository.GetAllNotesAsync(work.Id);

        // Assert
        Assert.NotEmpty(notes);
        Assert.All(notes, n => Assert.Equal(work.Id, n.CategoryId));
    }
}
