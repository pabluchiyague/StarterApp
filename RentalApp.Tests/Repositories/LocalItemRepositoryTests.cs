using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Models.Api;

namespace RentalApp.Tests;

public abstract class LocalItemRepositoryTests
{
    protected readonly DatabaseFixture _fixture;
    protected readonly LocalItemRepository _repository;

    protected LocalItemRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.Seed();
        _repository = new LocalItemRepository(_fixture.TestDbContext);
    }

    [Collection("Database")]
    public class CreateItemAsyncTests : LocalItemRepositoryTests
    {
        public CreateItemAsyncTests(DatabaseFixture fixture) : base(fixture)
        {
        }

    [Fact]
    public async Task CreateItemAsync_ValidItem_AssignsId()
    {
        var owner = await CreateOwnerAsync("create");
        var category = _fixture.TestDbContext.Categories.Single(c => c.Slug == "tools");

        var item = await _repository.CreateItemAsync(new Item
        {
            Title = $"Created drill {Guid.NewGuid():N}",
            Description = "Created by repository test",
            DailyRate = 7.50m,
            CategoryId = category.Id,
            OwnerId = owner.Id
        });

        Assert.NotEqual(0, item.Id);
        Assert.Equal(category.Id, item.CategoryId);
        Assert.Equal(owner.Id, item.OwnerId);
    }
    }

    [Collection("Database")]
    public class GetItemByIdAsyncTests : LocalItemRepositoryTests
    {
        public GetItemByIdAsyncTests(DatabaseFixture fixture) : base(fixture)
        {
        }

    [Fact]
    public async Task GetItemByIdAsync_ExistingItem_ReturnsWithCategoryAndOwner()
    {
        var owner = await CreateOwnerAsync("detail");
        var category = _fixture.TestDbContext.Categories.Single(c => c.Slug == "camping");
        var item = await _repository.CreateItemAsync(new Item
        {
            Title = $"Tent {Guid.NewGuid():N}",
            DailyRate = 12m,
            CategoryId = category.Id,
            OwnerId = owner.Id
        });

        var result = await _repository.GetItemByIdAsync(item.Id);

        Assert.NotNull(result);
        Assert.NotNull(result!.Category);
        Assert.NotNull(result.Owner);
        Assert.Equal("camping", result.Category!.Slug);
    }

    [Fact]
    public async Task GetItemByIdAsync_MissingItem_ReturnsNull()
    {
        var result = await _repository.GetItemByIdAsync(-1);

        Assert.Null(result);
    }
    }

    [Collection("Database")]
    public class GetItemsAsyncTests : LocalItemRepositoryTests
    {
        public GetItemsAsyncTests(DatabaseFixture fixture) : base(fixture)
        {
        }

    [Fact]
    public async Task GetItemsAsync_FilteredBySlug_ReturnsOnlyMatching()
    {
        var owner = await CreateOwnerAsync("filter");
        await CreateItemInCategoryAsync(owner.Id, "sports", $"Sports item {Guid.NewGuid():N}");
        await CreateItemInCategoryAsync(owner.Id, "games", $"Games item {Guid.NewGuid():N}");

        var result = await _repository.GetItemsAsync(categorySlug: "sports", pageSize: 100);

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item => Assert.Equal("sports", item.Category!.Slug));
    }

    [Fact]
    public async Task GetItemsAsync_Search_MatchesTitleCaseInsensitive()
    {
        var owner = await CreateOwnerAsync("search");
        var unique = $"ZXQ{Guid.NewGuid():N}";
        await CreateItemInCategoryAsync(owner.Id, "electronics", unique);

        var result = await _repository.GetItemsAsync(search: unique.ToLowerInvariant());

        Assert.Single(result.Items);
        Assert.Equal(unique, result.Items[0].Title);
    }
    }

    [Collection("Database")]
    public class UpdateItemAsyncTests : LocalItemRepositoryTests
    {
        public UpdateItemAsyncTests(DatabaseFixture fixture) : base(fixture)
        {
        }

    [Fact]
    public async Task UpdateItemAsync_PartialUpdate_PreservesUntouchedFields()
    {
        var owner = await CreateOwnerAsync("update");
        var item = await CreateItemInCategoryAsync(owner.Id, "tools", $"Original {Guid.NewGuid():N}");

        var result = await _repository.UpdateItemAsync(item.Id, new UpdateItemRequest
        {
            Title = "Updated title"
        });

        Assert.NotNull(result);
        Assert.Equal("Updated title", result!.Title);
        Assert.Equal(item.DailyRate, result.DailyRate);
    }

    [Fact]
    public async Task UpdateItemAsync_MissingItem_ReturnsNull()
    {
        var result = await _repository.UpdateItemAsync(-1, new UpdateItemRequest
        {
            Title = "Nope"
        });

        Assert.Null(result);
    }
    }

    [Collection("Database")]
    public class GetByOwnerAsyncTests : LocalItemRepositoryTests
    {
        public GetByOwnerAsyncTests(DatabaseFixture fixture) : base(fixture)
        {
        }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsOnlyOwnedItems()
    {
        var owner = await CreateOwnerAsync("owned");
        await CreateItemInCategoryAsync(owner.Id, "tools", $"Owned {Guid.NewGuid():N}");

        var result = await _repository.GetByOwnerAsync(owner.Id);

        Assert.NotEmpty(result);
        Assert.All(result, item => Assert.Equal(owner.Id, item.OwnerId));
    }
    }

    protected async Task<User> CreateOwnerAsync(string label)
    {
        var user = new User
        {
            FirstName = "Item",
            LastName = "Owner",
            Email = $"{label}.{Guid.NewGuid():N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sup3rSecret!"),
            PasswordSalt = string.Empty,
            IsActive = true
        };

        _fixture.TestDbContext.Users.Add(user);
        await _fixture.TestDbContext.SaveChangesAsync();
        return user;
    }

    protected async Task<Item> CreateItemInCategoryAsync(int ownerId, string slug, string title)
    {
        var category = await _fixture.TestDbContext.Categories.SingleAsync(c => c.Slug == slug);
        return await _repository.CreateItemAsync(new Item
        {
            Title = title,
            Description = $"{slug} item",
            DailyRate = 4.25m,
            CategoryId = category.Id,
            OwnerId = ownerId,
            IsAvailable = true
        });
    }
}
