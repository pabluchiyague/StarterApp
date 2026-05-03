using RentalApp.Database.Models;

namespace RentalApp.Tests;

/// <summary>
/// Cheap smoke tests that confirm the rental schema migrated and seeded
/// correctly. They replace the now-deleted <c>NoteRepositoryTests</c>
/// during Phase 4 — full Item / Rental / Review repository tests arrive
/// alongside <c>LocalItemRepository</c> in Phase 5.
/// </summary>
[Collection("Database")]
public class SchemaSanityTests
{
    private readonly DatabaseFixture _fixture;

    public SchemaSanityTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Categories_AreSeededByMigration_FiveRowsWithSlugs()
    {
        var categories = _fixture.TestDbContext.Categories
            .OrderBy(c => c.Id)
            .ToList();

        Assert.Equal(5, categories.Count);
        Assert.Equal(new[] { "tools", "camping", "sports", "electronics", "games" },
                     categories.Select(c => c.Slug).ToArray());
    }

    [Fact]
    public void Roles_AreSeededByMigration_OrdinaryUserIsDefault()
    {
        var defaultRole = _fixture.TestDbContext.Roles.Single(r => r.IsDefault);
        Assert.Equal(RoleConstants.OrdinaryUser, defaultRole.Name);
    }

    [Fact]
    public void Items_TableExists_AndAcceptsInsertedRow()
    {
        _fixture.Seed();

        var seeded = _fixture.TestDbContext.Items
            .OrderBy(i => i.Id)
            .First();

        Assert.NotEqual(0, seeded.Id);
        Assert.Equal("Seed drill", seeded.Title);
        Assert.True(seeded.IsAvailable);
    }
}
