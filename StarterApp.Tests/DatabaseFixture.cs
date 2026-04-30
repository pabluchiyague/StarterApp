using Microsoft.EntityFrameworkCore;
using StarterApp.Database.Data;
using StarterApp.Database.Models;

namespace StarterApp.Tests;

/// <summary>
/// xUnit fixture that boots an isolated PostgreSQL database (testappdb)
/// for the lifetime of a test class. The fixture:
///   - Switches the CONNECTION_STRING env var to point at the test DB
///   - Drops and recreates the DB via the production migration set
///   - Optionally seeds a known Note for tests that need pre-existing data
/// IClassFixture&lt;DatabaseFixture&gt; on a test class causes xUnit to
/// instantiate this once per class and inject it into the constructor.
/// </summary>
public class DatabaseFixture
{
    internal AppDbContext TestDbContext { get; }

    public DatabaseFixture()
    {
        // Point AppDbContext at a separate test database. The production
        // AppDbContext.OnConfiguring already reads CONNECTION_STRING, so we
        // just override the env var for this test process.
        var testConn = "Host=localhost;Username=app_user;Password=app_password;Database=testappdb";
        Environment.SetEnvironmentVariable("CONNECTION_STRING", testConn);

        TestDbContext = new AppDbContext();

        // Repeatable clean state on every run
        TestDbContext.Database.EnsureDeleted();
        TestDbContext.Database.Migrate();
    }

    /// <summary>
    /// Adds a single seed note. Idempotent: safe to call from every test
    /// class constructor. The default categories (Personal, Work, Study,
    /// Shopping, Health) are already inserted by EF migrations via
    /// AppDbContext.SeedData / SeedHealthCategory.
    /// </summary>
    internal void Seed()
    {
        if (TestDbContext.Notes.Any()) return;

        var work = TestDbContext.Categories.First(c => c.Name == "Work");

        var note = new Note
        {
            Title = "Seed note",
            Content = "Seeded by the fixture",
            CategoryId = work.Id,
            Importance = NoteImportance.Normal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        TestDbContext.Notes.Add(note);
        TestDbContext.SaveChanges();
    }
}
