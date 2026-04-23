using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StarterApp.Database.Models;

namespace StarterApp.Database.Data;

public class AppDbContext : DbContext
{
    public AppDbContext()
    { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Prefer the CONNECTION_STRING env var when running inside the dev container
        // (docker-compose sets it to point at the `db` service via the shared network
        // namespace). Fall back to the embedded appsettings.json, which targets the
        // Android emulator's 10.0.2.2 host loopback and is only valid when the MAUI
        // app runs on-device.
        var envConn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConn))
        {
            optionsBuilder.UseNpgsql(envConn);
            return;
        }

        var a = Assembly.GetExecutingAssembly();
        using var stream = a.GetManifestResourceStream("StarterApp.Database.appsettings.json");

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        optionsBuilder.UseNpgsql(
            config.GetConnectionString("DevelopmentConnection")
        );
    }

    // NEW: Define database tables for note-taking app
    public DbSet<Category> Categories { get; set; }
    public DbSet<Note> Notes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            // Unique constraint on category name (can't have duplicate "Work" categories)
            entity.HasIndex(e => e.Name).IsUnique();

            // Ensure proper column types and constraints
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.ColorHex).HasMaxLength(7);
            entity.Property(e => e.Description).HasMaxLength(200);
        });

        // Configure Note entity
        modelBuilder.Entity<Note>(entity =>
        {
            // Index on CategoryId for faster filtering by category
            entity.HasIndex(e => e.CategoryId);

            // Index on CreatedAt for sorting by date
            entity.HasIndex(e => e.CreatedAt);

            // Ensure proper column types
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Content).HasColumnType("text");  // PostgreSQL text type

            // Configure one-to-many relationship
            entity.HasOne(n => n.Category)
                  .WithMany(c => c.Notes)
                  .HasForeignKey(n => n.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);  // When category deleted, set CategoryId to NULL
        });

        // Seed default categories
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Seeds the database with default categories
    /// </summary>
    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Personal", ColorHex = "#4CAF50", Description = "Personal notes and ideas" },
            new Category { Id = 2, Name = "Work", ColorHex = "#2196F3", Description = "Work-related tasks and notes" },
            new Category { Id = 3, Name = "Study", ColorHex = "#FF9800", Description = "Study materials and learning notes" },
            new Category { Id = 4, Name = "Shopping", ColorHex = "#E91E63", Description = "Shopping lists and reminders" }
        );
    }
}
