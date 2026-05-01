using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Tests;

/// <summary>
/// xUnit fixture that boots an isolated PostgreSQL database (testappdb)
/// for the lifetime of a test run. The fixture:
///   - Switches the CONNECTION_STRING env var to point at the test DB
///   - Drops and recreates the DB via the production migration set
///   - Optionally seeds a known User + Item for tests that need
///     pre-existing data
///
/// All tests in the suite share this fixture via <c>[Collection("Database")]</c>
/// — see <see cref="DatabaseCollection"/>.
/// </summary>
public class DatabaseFixture
{
    internal AppDbContext TestDbContext { get; }

    public DatabaseFixture()
    {
        var testConn = "Host=localhost;Username=app_user;Password=app_password;Database=testappdb";
        Environment.SetEnvironmentVariable("CONNECTION_STRING", testConn);

        TestDbContext = new AppDbContext();

        // Repeatable clean state on every run.
        TestDbContext.Database.EnsureDeleted();
        TestDbContext.Database.Migrate();
    }

    /// <summary>
    /// Adds one seed user and one seed item under the "Tools" category.
    /// Idempotent — safe to call from every test class constructor.
    /// Default categories and roles are inserted by EF migrations via
    /// <c>AppDbContext.SeedRoles</c> / <c>SeedCategories</c>.
    /// </summary>
    internal void Seed()
    {
        if (TestDbContext.Items.Any()) return;

        // Seed user (used as the owner of the seed item)
        var owner = new User
        {
            FirstName    = "Seed",
            LastName     = "Owner",
            Email        = "seed.owner@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sup3rSecret!"),
            PasswordSalt = string.Empty,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
            IsActive     = true,
        };
        TestDbContext.Users.Add(owner);
        TestDbContext.SaveChanges();

        // Seed item under the "Tools" category (already inserted by migration)
        var tools = TestDbContext.Categories.First(c => c.Slug == "tools");

        var item = new Item
        {
            Title       = "Seed drill",
            Description = "Cordless drill — seeded by the fixture",
            DailyRate   = 5.00m,
            CategoryId  = tools.Id,
            OwnerId     = owner.Id,
            IsAvailable = true,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow,
        };
        TestDbContext.Items.Add(item);
        TestDbContext.SaveChanges();
    }
}
