using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RentalApp.Database.Models;

namespace RentalApp.Database.Data;

public class AppDbContext : DbContext
{
    public AppDbContext()
    { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // If DbContextOptions were already supplied (DI / tests), respect them.
        if (optionsBuilder.IsConfigured) return;

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
        using var stream = a.GetManifestResourceStream("RentalApp.Database.appsettings.json");

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        optionsBuilder.UseNpgsql(
            config.GetConnectionString("DevelopmentConnection")
        );
    }

    // Auth tables
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    // Note-taking tables (will be replaced by rental tables in a later phase)
    public DbSet<Category> Categories { get; set; }
    public DbSet<Note> Notes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Auth: User ----
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PasswordSalt).HasMaxLength(255);
        });

        // ---- Auth: Role ----
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // ---- Auth: UserRole junction ----
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId);

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId);
        });

        // ---- Notes: Category ----
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.ColorHex).HasMaxLength(7);
            entity.Property(e => e.Description).HasMaxLength(200);
        });

        // ---- Notes: Note ----
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.HasOne(n => n.Category)
                  .WithMany(c => c.Notes)
                  .HasForeignKey(n => n.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ---- Seeds ----
        SeedRoles(modelBuilder);
        SeedCategories(modelBuilder);
    }

    /// <summary>
    /// Seeds the three default roles required by the auth services.
    /// "OrdinaryUser" is flagged IsDefault so RegisterAsync auto-assigns it
    /// to new accounts.
    /// </summary>
    private void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = RoleConstants.Admin,        Description = "System administrator", IsDefault = false },
            new Role { Id = 2, Name = RoleConstants.OrdinaryUser, Description = "Standard end-user",    IsDefault = true  },
            new Role { Id = 3, Name = RoleConstants.SpecialUser,  Description = "Privileged end-user",  IsDefault = false }
        );
    }

    /// <summary>
    /// Seeds default note categories. These will go away when the domain
    /// is reset to rentals (Phase 2 of the coursework plan).
    /// </summary>
    private void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Personal", ColorHex = "#4CAF50", Description = "Personal notes and ideas" },
            new Category { Id = 2, Name = "Work",     ColorHex = "#2196F3", Description = "Work-related tasks and notes" },
            new Category { Id = 3, Name = "Study",    ColorHex = "#FF9800", Description = "Study materials and learning notes" },
            new Category { Id = 4, Name = "Shopping", ColorHex = "#E91E63", Description = "Shopping lists and reminders" }
        );
    }
}
