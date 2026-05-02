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
        // Respect any options the DI container or test harness has supplied.
        if (optionsBuilder.IsConfigured) return;

        // Prefer the CONNECTION_STRING env var (dev container, CI service
        // container, or DatabaseFixture override). Fall back to the embedded
        // appsettings.json which targets the Android emulator's 10.0.2.2
        // host loopback for on-device runs.
        var envConn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConn))
        {
            optionsBuilder.UseNpgsql(envConn, options => options.UseNetTopologySuite());
            return;
        }

        var a = Assembly.GetExecutingAssembly();
        using var stream = a.GetManifestResourceStream("RentalApp.Database.appsettings.json");
        if (stream == null)
        {
            throw new InvalidOperationException("Embedded database appsettings.json was not found.");
        }

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        optionsBuilder.UseNpgsql(
            config.GetConnectionString("DevelopmentConnection"),
            options => options.UseNetTopologySuite()
        );
    }

    // ------------ Auth ------------
    public DbSet<User>     Users     { get; set; }
    public DbSet<Role>     Roles     { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    // ------------ Domain ----------
    public DbSet<Category> Categories { get; set; }
    public DbSet<Item>     Items      { get; set; }
    public DbSet<Rental>   Rentals    { get; set; }
    public DbSet<Review>   Reviews    { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("postgis");

        // ---- User ----
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PasswordSalt).HasMaxLength(255);
        });

        // ---- Role ----
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // ---- UserRole ----
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            entity.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        });

        // ---- Category ----
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Slug).HasMaxLength(50);
            entity.Property(e => e.ColorHex).HasMaxLength(7);
            entity.Property(e => e.Description).HasMaxLength(200);
        });

        // ---- Item ----
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Location)
                  .HasColumnType("geography(Point,4326)")
                  .HasColumnName("location");
            entity.HasIndex(e => e.Location).HasMethod("GIST");

            entity.HasOne(i => i.Category)
                  .WithMany(c => c.Items)
                  .HasForeignKey(i => i.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Owner)
                  .WithMany()
                  .HasForeignKey(i => i.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Rental ----
        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasIndex(e => e.ItemId);
            entity.HasIndex(e => e.BorrowerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.ItemId, e.Status });

            entity.HasOne(r => r.Item)
                  .WithMany(i => i.Rentals)
                  .HasForeignKey(r => r.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Borrower)
                  .WithMany()
                  .HasForeignKey(r => r.BorrowerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Review ----
        modelBuilder.Entity<Review>(entity =>
        {
            // One review per rental — enforces the API's no-duplicate rule
            entity.HasIndex(e => e.RentalId).IsUnique();
            entity.HasIndex(e => e.ReviewerId);
            entity.Property(e => e.Comment).HasMaxLength(500);

            entity.HasOne(rv => rv.Rental)
                  .WithOne(r => r.Review)
                  .HasForeignKey<Review>(rv => rv.RentalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rv => rv.Reviewer)
                  .WithMany()
                  .HasForeignKey(rv => rv.ReviewerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Seeds ----
        SeedRoles(modelBuilder);
        SeedCategories(modelBuilder);
    }

    /// <summary>
    /// Seeds the three default roles. <c>OrdinaryUser</c> is flagged
    /// IsDefault=true so RegisterAsync auto-assigns it to new accounts.
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
    /// Seeds rental categories with their API-aligned slugs. Slugs match
    /// the values the API accepts in <c>GET /items?category=tools</c>.
    /// </summary>
    private void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Tools",       Slug = "tools",       ColorHex = "#F44336", Description = "Power tools, hand tools" },
            new Category { Id = 2, Name = "Camping",     Slug = "camping",     ColorHex = "#4CAF50", Description = "Tents, stoves, sleeping bags" },
            new Category { Id = 3, Name = "Sports",      Slug = "sports",      ColorHex = "#2196F3", Description = "Bikes, skis, sports gear" },
            new Category { Id = 4, Name = "Electronics", Slug = "electronics", ColorHex = "#9C27B0", Description = "Cameras, projectors, audio" },
            new Category { Id = 5, Name = "Games",       Slug = "games",       ColorHex = "#FF9800", Description = "Board games, party games" }
        );
    }
}
