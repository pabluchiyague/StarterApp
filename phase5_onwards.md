## Phase 5 — Items UI + dual repository (Day 8-11, ~12 hours)

This is the first phase that makes the app rental-shaped end-to-end. By the end of it the user can browse, view, and create items against either a local DB (used by tests) or the live API (toggled by a single const). Phase 5 also introduces the dual-implementation pattern that Phases 6, 7, and 8 reuse for Rentals and Reviews.

The API endpoints in scope here (full schemas in the API reference PDF):

| Verb | URL | Auth? | Purpose |
|---|---|---|---|
| GET | `/items` | No | Paginated list with filters |
| GET | `/items/{id}` | No | One item with embedded reviews |
| POST | `/items` | Yes (Bearer) | Create — owner becomes the JWT subject |
| PUT | `/items/{id}` | Yes (Bearer, owner only) | Partial update; 403 if not owner |
| GET | `/categories` | No | List of categories with `{id, name, slug, itemCount}` |
| GET | `/items/nearby` | No | Phase 6 — defer until then |

### 5.1 What's already built (Phase 4)

The DTOs and JSON helpers are already in `RentalApp.Database/Models/Api/` from Phase 4. Quick reference:

| File | Purpose |
|---|---|
| `ApiJson.cs` | Centralised `JsonSerializerOptions` (camelCase, ignore-null, RentalStatus converter) |
| `RentalStatusJsonConverter.cs` | `"Out for Rent"` ↔ `RentalStatus.OutForRent` |
| `PagedResponse.cs` | Wire-format envelope **and** the local `PagedResult<T>` record |
| `ErrorResponse.cs` | `{ error, message, details? }` — every non-2xx body |
| `AuthDtos.cs` | `LoginRequest`, `LoginResponse`, `RegisterRequest`, `UserDto` |
| `ItemDtos.cs` | `ItemSummaryDto`, `ItemDetailDto`, `CreateItemRequest`, `UpdateItemRequest`, `NearbyResponse`, `CategoryDto`, `CategoryListResponse` |
| `RentalDtos.cs` | `CreateRentalRequest`, `UpdateRentalStatusRequest`, `RentalSummaryDto`, `RentalDetailDto`, `RentalListResponse` |
| `ReviewDtos.cs` | `CreateReviewRequest`, `ReviewDto`, `ReviewListResponse` |

You don't have to write any DTO code in Phase 5 — they're already there. You will write mappers between DTOs and your domain models.

### 5.2 The DTO ↔ domain mapping rules (read this once)

These rules apply to every API repository in Phases 5, 7, and 8.

| Direction | Field on DTO | Field on domain | Note |
|---|---|---|---|
| API → domain | `category` (slug string) | `Item.Category.Slug` and `Item.CategoryId` | API never returns a foreign-key int alone — match by slug. Use `_categoryCache.First(c => c.Slug == dto.Category).Id` |
| API → domain | `ownerName`, `ownerRating` | `Item.Owner.FirstName + LastName` and a non-mapped average | The API does the join server-side; treat as read-only joined fields |
| API → domain | `distance` (km) | `Item.DistanceKm` (NotMapped) | Phase 6 — only present on `/items/nearby` |
| API → domain | `status` (string) | `Rental.Status` (enum) | Handled automatically by `RentalStatusJsonConverter` — don't roll your own |
| domain → API | `Item.CategoryId` | `categoryId` (int) | Send the int when creating; don't translate to slug |
| domain → API | `Item.Location` (Point) | `latitude`, `longitude` (double pair) | API expects two scalars, not GeoJSON |
| domain → API | `Rental.StartDate` | `startDate` (ISO date string `yyyy-MM-dd`) | `DateTime` round-trips fine via System.Text.Json with the converter; if it doesn't, format manually with `.ToString("yyyy-MM-dd")` |

Define one private mapper class `ItemDtoMapper` per repository to keep these rules in one place. Tests assert the rules — round-tripping a known DTO into a domain object and back must be lossless for the fields we own.

### 5.3 Domain repository contract

`RentalApp.Database/Repositories/IItemRepository.cs`:

```csharp
using RentalApp.Database.Models;
using RentalApp.Models.Api;

namespace RentalApp.Database.Repositories;

public interface IItemRepository
{
    /// <summary>
    /// Paginated list. <paramref name="categorySlug"/> uses API-style slug
    /// strings (e.g. "tools"); local impl translates to CategoryId via
    /// <see cref="Category.Slug"/>.
    /// </summary>
    Task<PagedResult<Item>> GetItemsAsync(
        string? categorySlug = null,
        string? search = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>404 (API) or null record (local) -> returns null.</summary>
    Task<Item?> GetItemByIdAsync(int id);

    /// <summary>Caller must set OwnerId locally; server overrides it for API.</summary>
    Task<Item> CreateItemAsync(Item item);

    /// <summary>
    /// Partial update — pass only the fields to change on
    /// <paramref name="updates"/>. Returns null if not found, throws if 403.
    /// </summary>
    Task<Item?> UpdateItemAsync(int id, UpdateItemRequest updates);

    /// <summary>
    /// "My listings" view. The API doesn't have this endpoint — the
    /// API impl falls back to filtering /items results client-side; the
    /// local impl uses an indexed query on Items.OwnerId.
    /// </summary>
    Task<List<Item>> GetByOwnerAsync(int ownerId);

    Task<List<Category>> GetAllCategoriesAsync();
}
```

`PagedResult<T>` already exists in `RentalApp.Database/Models/Api/PagedResponse.cs`.

### 5.4 LocalItemRepository

DB-backed. Sits in `RentalApp.Database/Repositories/LocalItemRepository.cs`.

Key decisions:
- Always `Include(c => c.Category)` and `Include(o => o.Owner)` — Items list / detail views need owner display name
- Translate `categorySlug` → `categoryId` via a `Categories.FirstOrDefault(c => c.Slug == slug)` lookup. Cache the categories list per request to avoid round-tripping the DB once per item
- Pagination: `Skip((page - 1) * pageSize).Take(pageSize)` plus a separate `Count()` for totals

```csharp
public async Task<PagedResult<Item>> GetItemsAsync(string? categorySlug, string? search, int page, int pageSize)
{
    IQueryable<Item> query = _ctx.Items.Include(i => i.Category).Include(i => i.Owner);

    if (!string.IsNullOrEmpty(categorySlug))
    {
        var category = await _ctx.Categories.FirstOrDefaultAsync(c => c.Slug == categorySlug);
        if (category == null) return new PagedResult<Item>(Array.Empty<Item>(), 0, page, pageSize, 0);
        query = query.Where(i => i.CategoryId == category.Id);
    }

    if (!string.IsNullOrEmpty(search))
    {
        var like = $"%{search}%";
        query = query.Where(i => EF.Functions.ILike(i.Title, like)
                              || EF.Functions.ILike(i.Description ?? "", like));
    }

    var total = await query.CountAsync();
    var items = await query
        .OrderByDescending(i => i.CreatedAt)
        .Skip((page - 1) * pageSize).Take(pageSize)
        .ToListAsync();

    var totalPages = (int)Math.Ceiling(total / (double)pageSize);
    return new PagedResult<Item>(items, total, page, pageSize, totalPages);
}
```

For `UpdateItemAsync`, only assign properties on the model that have non-null values on the request:

```csharp
public async Task<Item?> UpdateItemAsync(int id, UpdateItemRequest u)
{
    var existing = await _ctx.Items.FindAsync(id);
    if (existing == null) return null;
    if (u.Title       is { } t) existing.Title       = t;
    if (u.Description is { } d) existing.Description = d;
    if (u.DailyRate   is { } r) existing.DailyRate   = r;
    if (u.IsAvailable is { } a) existing.IsAvailable = a;
    existing.UpdatedAt = DateTime.UtcNow;
    await _ctx.SaveChangesAsync();
    return existing;
}
```

Tests in `RentalApp.Tests/LocalItemRepositoryTests.cs`:
- `CreateItemAsync_AssignsId`
- `GetItemByIdAsync_ExistingItem_ReturnsWithCategoryAndOwner`
- `GetItemByIdAsync_MissingItem_ReturnsNull`
- `GetItemsAsync_FilteredBySlug_ReturnsOnlyMatching`
- `GetItemsAsync_Pagination_RespectsPageSize`
- `GetItemsAsync_Search_MatchesTitleCaseInsensitive`
- `UpdateItemAsync_PartialUpdate_PreservesUntouchedFields`
- `UpdateItemAsync_MissingItem_ReturnsNull`
- `GetByOwnerAsync_ReturnsOnlyOwnedItems`

Add `[Collection("Database")]` so they share the existing fixture.

### 5.5 ApiItemRepository — concrete URL building

Lives in `RentalApp.Database/Repositories/ApiItemRepository.cs`. Constructor takes an `HttpClient` injected by `AddHttpClient<...>()`. **Do not configure the BaseAddress here** — that's the DI registration's job.

```csharp
public class ApiItemRepository : IItemRepository
{
    private readonly HttpClient _http;
    public ApiItemRepository(HttpClient http) { _http = http; }

    public async Task<PagedResult<Item>> GetItemsAsync(
        string? categorySlug, string? search, int page, int pageSize)
    {
        // Spec: /items?category=<slug>&search=<text>&page=<n>&pageSize=<n>
        // Defaults per the API: page=1, pageSize=20 (max 100). Always send both
        // explicitly so behaviour is deterministic.
        var query = new List<string>();
        if (!string.IsNullOrEmpty(categorySlug)) query.Add($"category={Uri.EscapeDataString(categorySlug)}");
        if (!string.IsNullOrEmpty(search))       query.Add($"search={Uri.EscapeDataString(search)}");
        query.Add($"page={page}");
        query.Add($"pageSize={Math.Clamp(pageSize, 1, 100)}");
        var url = $"items?{string.Join("&", query)}";

        var resp = await _http.GetAsync(url);
        await ThrowApiError(resp);
        var dto = await resp.Content.ReadFromJsonAsync<PagedResponse<ItemSummaryDto>>(ApiJson.Options);

        var items = dto!.Items.Select(MapSummary).ToList();
        return new PagedResult<Item>(items, dto.TotalItems, dto.Page, dto.PageSize, dto.TotalPages);
    }

    public async Task<Item?> GetItemByIdAsync(int id)
    {
        var resp = await _http.GetAsync($"items/{id}");
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        await ThrowApiError(resp);
        var dto = await resp.Content.ReadFromJsonAsync<ItemDetailDto>(ApiJson.Options);
        return MapDetail(dto!);
    }

    public async Task<Item> CreateItemAsync(Item item)
    {
        var body = new CreateItemRequest(
            item.Title,
            item.Description,
            item.DailyRate,
            item.CategoryId,
            // Latitude / Longitude come from Item.Location (Phase 6) or
            // from a CreateItemViewModel that captures them via ILocationService.
            // Use 0/0 placeholders pre-Phase-6.
            Latitude:  item.Location?.Y ?? 0,
            Longitude: item.Location?.X ?? 0);

        var resp = await _http.PostAsJsonAsync("items", body, ApiJson.Options);
        await ThrowApiError(resp);
        var dto = await resp.Content.ReadFromJsonAsync<ItemDetailDto>(ApiJson.Options);
        return MapDetail(dto!);
    }

    public async Task<Item?> UpdateItemAsync(int id, UpdateItemRequest updates)
    {
        var resp = await _http.PutAsJsonAsync($"items/{id}", updates, ApiJson.Options);
        if (resp.StatusCode == HttpStatusCode.NotFound)  return null;
        if (resp.StatusCode == HttpStatusCode.Forbidden) throw new UnauthorizedAccessException("You can only update your own items.");
        await ThrowApiError(resp);
        var dto = await resp.Content.ReadFromJsonAsync<ItemDetailDto>(ApiJson.Options);
        return MapDetail(dto!);
    }

    public async Task<List<Item>> GetByOwnerAsync(int ownerId)
    {
        // API doesn't have /users/{id}/items. Two options:
        //   1. Pull /items pages until we've seen them all and filter in-memory
        //   2. (Cheaper) Pull /users/{id}/profile which lists itemsListed count
        //      and call /items per category — clunky.
        // For Tier 1/2 scope, fall back to (1) with a hard cap.
        var firstPage = await GetItemsAsync(null, null, 1, 100);
        return firstPage.Items.Where(i => i.OwnerId == ownerId).ToList();
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        var resp = await _http.GetFromJsonAsync<CategoryListResponse>("categories", ApiJson.Options);
        return resp!.Categories.Select(c => new Category
        {
            Id = c.Id, Name = c.Name, Slug = c.Slug
        }).ToList();
    }

    private static async Task ThrowApiError(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;

        // Error envelope: { "error": "...", "message": "...", "details"?: {...} }
        ErrorResponse? err = null;
        try { err = await resp.Content.ReadFromJsonAsync<ErrorResponse>(ApiJson.Options); }
        catch { /* swallow — body might be empty */ }

        var msg = err?.Message ?? resp.ReasonPhrase ?? "Request failed";
        throw resp.StatusCode switch
        {
            HttpStatusCode.BadRequest    => new ArgumentException(msg),
            HttpStatusCode.Unauthorized  => new UnauthorizedAccessException(msg),
            HttpStatusCode.Forbidden     => new UnauthorizedAccessException(msg),
            HttpStatusCode.Conflict      => new InvalidOperationException(msg),
            HttpStatusCode.TooManyRequests => new HttpRequestException("Rate limited — try again soon."),
            _                            => new HttpRequestException($"{(int)resp.StatusCode} {msg}"),
        };
    }

    private static Item MapSummary(ItemSummaryDto d) => new()
    {
        Id          = d.Id,
        Title       = d.Title,
        Description = d.Description,
        DailyRate   = d.DailyRate,
        CategoryId  = d.CategoryId,
        Category    = new Category { Id = d.CategoryId, Name = d.Category, Slug = d.Category.ToLowerInvariant() },
        OwnerId     = d.OwnerId,
        Owner       = new User { Id = d.OwnerId, FirstName = d.OwnerName, LastName = string.Empty },
        IsAvailable = d.IsAvailable,
        ImageUrl    = d.ImageUrl,
        CreatedAt   = d.CreatedAt,
        AverageRating = d.AverageRating,
        DistanceKm  = d.Distance,   // null unless populated by /items/nearby
    };

    private static Item MapDetail(ItemDetailDto d)
    {
        var item = MapSummary(d);
        // ItemDetailDto adds reviews + lat/lon; map into the domain item if needed
        return item;
    }
}
```

**Status code → exception map (used by every API repo this phase, and Phases 7, 8):**

| Status | Cause | Throws |
|---|---|---|
| 200/201 | Success | nothing |
| 400 | Validation failed | `ArgumentException(message)` — surface as toast in VM |
| 401 | Token missing/expired | `UnauthorizedAccessException` — `AuthorizationDelegatingHandler` (Phase 9) clears the token; user is bumped to login |
| 403 | Permission denied (e.g. updating someone else's item) | `UnauthorizedAccessException` |
| 404 | Resource not found | repository returns `null`, doesn't throw |
| 409 | Conflict (overlapping rental, duplicate review) | `InvalidOperationException(message)` |
| 429 | Rate limited | `HttpRequestException` — VM should back off |
| 5xx | Server error | `HttpRequestException` — VM should retry once or surface |

### 5.6 Tests for ApiItemRepository — `FakeHttpMessageHandler`

The point of testing the API repo is to verify three things WITHOUT touching the network: the URL we build, the JSON we send, and how we map the response. Use a stub `HttpMessageHandler`:

```csharp
public class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = new();
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> r) { _responder = r; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        Requests.Add(req);
        return Task.FromResult(_responder(req));
    }
}
```

Helper for canned responses:

```csharp
private static HttpResponseMessage Ok<T>(T body) => new(HttpStatusCode.OK)
{
    Content = JsonContent.Create(body, options: ApiJson.Options)
};
```

Tests in `RentalApp.Tests/ApiItemRepositoryTests.cs`:
- `GetItemsAsync_BuildsCorrectQueryString_WithAllFiltersEscaped`
- `GetItemsAsync_OnSuccess_MapsDtosToDomainObjects`
- `GetItemByIdAsync_On404_ReturnsNull`
- `GetItemByIdAsync_On500_ThrowsHttpRequestException`
- `CreateItemAsync_PostsRightBodyToItemsEndpoint`
- `CreateItemAsync_On400_ThrowsArgumentExceptionWithApiMessage`
- `UpdateItemAsync_On403_ThrowsUnauthorizedAccessException`
- `UpdateItemAsync_OnSuccess_ReturnsUpdatedItem`
- `RentalStatusJsonConverter_RoundTrip_OutForRent` (cross-cutting — covers the Phase 7 contract early)

These tests don't need the `DatabaseFixture` — they construct the repo with a `new HttpClient(fakeHandler) { BaseAddress = new("https://test/") }` and assert against `fakeHandler.Requests[0]`.

### 5.7 DI registration with the toggle

Add a constant near the top of `MauiProgram.cs`:

```csharp
public static class FeatureFlags
{
    /// <summary>
    /// false → all repositories use the local Postgres DB (good for offline,
    ///         tests, and incremental development).
    /// true  → all repositories hit the API at set09102-api.b-davison.workers.dev.
    /// Phase 9 wires AuthorizationDelegatingHandler so this toggle becomes
    /// safe to flip.
    /// </summary>
    public const bool UseApi = false;
}
```

Then in `CreateMauiApp()`:

```csharp
if (FeatureFlags.UseApi)
{
    var apiBase = new Uri("https://set09102-api.b-davison.workers.dev/");
    builder.Services.AddHttpClient<IItemRepository, ApiItemRepository>(c => c.BaseAddress = apiBase);
    // .AddHttpMessageHandler<AuthorizationDelegatingHandler>();   // Phase 9
}
else
{
    builder.Services.AddScoped<IItemRepository, LocalItemRepository>();
}
```

The same pattern repeats for `IRentalRepository` (Phase 7) and `IReviewRepository` (Phase 8) — write them once here and add to both branches.

### 5.8 Views and ViewModels

Three pages, three view-models. All depend on `IItemRepository`, none care which implementation is bound.

| Page | VM | Route | Purpose |
|---|---|---|---|
| `ItemsListPage` | `ItemsListViewModel` | `//items` (Shell tab) | Browse with category filter and search box |
| `ItemDetailPage` | `ItemDetailViewModel` | `itemDetail` (push) | View one item + reviews; owner-only Edit/Delete buttons |
| `CreateItemPage` | `CreateItemViewModel` | `createItem` (push) | Form: title, description, dailyRate, category dropdown, GPS fields (Phase 6) |

`ItemsListViewModel` constructor:

```csharp
public ItemsListViewModel(IItemRepository repository, INavigationService nav)
{
    _repository = repository;
    _nav = nav;
    Title = "Browse items";
}

[ObservableProperty] private ObservableCollection<Item> items = new();
[ObservableProperty] private List<Category> categories = new();
[ObservableProperty] private Category? selectedCategory;
[ObservableProperty] private string searchText = string.Empty;
[ObservableProperty] private int page = 1;
[ObservableProperty] private int totalPages;

[RelayCommand]
private async Task LoadAsync()
{
    Categories = await _repository.GetAllCategoriesAsync();
    var result = await _repository.GetItemsAsync(SelectedCategory?.Slug, SearchText, Page, pageSize: 20);
    Items = new ObservableCollection<Item>(result.Items);
    TotalPages = result.TotalPages;
}

partial void OnSelectedCategoryChanged(Category? _) { Page = 1; _ = LoadAsync(); }
```

`ItemDetailViewModel.IsOwner` powers the visibility of edit/delete:

```csharp
public bool IsOwner => _auth.CurrentUser?.Id == Item.OwnerId;
```

XAML binds `IsVisible="{Binding IsOwner}"` on the edit/delete buttons.

### 5.9 Shell wiring

Replace the current "About" flyout in `AppShell.xaml` (or add alongside it) with an Items section:

```xml
<FlyoutItem Title="Browse" Icon="items_icon.png">
    <ShellContent Title="Items"
                  ContentTemplate="{DataTemplate views:ItemsListPage}"
                  Route="items" />
</FlyoutItem>
```

Register the push routes in `AppShell.xaml.cs`:

```csharp
Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
Routing.RegisterRoute(nameof(CreateItemPage), typeof(CreateItemPage));
```

Update `LoginViewModel.LoginAsync` post-login redirect from `"//about"` to `"//items"`.

`MauiProgram.cs` adds:

```csharp
builder.Services.AddTransient<ItemsListViewModel>();
builder.Services.AddTransient<ItemsListPage>();
builder.Services.AddTransient<ItemDetailViewModel>();
builder.Services.AddTransient<ItemDetailPage>();
builder.Services.AddTransient<CreateItemViewModel>();
builder.Services.AddTransient<CreateItemPage>();
```

### 5.10 ViewModel tests

Add `Moq` to `RentalApp.Tests.csproj`:

```xml
<PackageReference Include="Moq" Version="4.20.70" />
```

Tests in `RentalApp.Tests/ItemsListViewModelTests.cs` (no fixture needed — pure mocks):

- `LoadAsync_PopulatesItemsAndCategories`
- `LoadAsync_WhenCategorySelected_PassesSlugToRepository`
- `LoadAsync_OnRepositoryThrow_SetsErrorMessage`

And in `ItemDetailViewModelTests.cs`:

- `IsOwner_WhenCurrentUserIsOwner_ReturnsTrue`
- `IsOwner_WhenAnonymous_ReturnsFalse`
- `DeleteAsync_OnNonOwner_DoesNothing`

### 5.11 Manual verification checkpoint

With `UseApi = false`:

1. Build + deploy to emulator
2. Register a new user → log in → land on the Items list page
3. Categories dropdown shows the five seeded categories
4. Tap "List an item" → fill form → submit → appears in the list
5. Filter by category → only matching items shown
6. Tap an item → detail page; if you're the owner, Edit visible

With `UseApi = true` (only after Phase 9):

7. Same flow against the live API. Watch network tab for actual JSON shapes.

### 5.12 Coverage expectation

After Phase 5 the suite has roughly 25-30 tests:

- 5 from Tutorial 6 (auth fixture sanity, schema sanity)
- 5 from Phase 3 (`AuthenticationServiceTests`)
- ~10 from `LocalItemRepositoryTests`
- ~9 from `ApiItemRepositoryTests`
- ~6 from VM tests

Coverage should be ~55-60%. Phase 7 (state pattern + repository) and Phase 8 (reviews) push it past 80%.

```
git commit -am "Phase 5: Items UI with dual local/API IItemRepository; URL building, error mapping, and tests"
```

---

## Phase 6 — PostGIS spatial search (Day 12-14, ~9 hours) — Merit-required

### 6.1 Switch the Postgres image

`docker-compose.yml`:

```yaml
db:
  image: postgis/postgis:16-3.4   # was postgres:16
```

`.github/workflows/build.yml`:

```yaml
services:
  postgres:
    image: postgis/postgis:16-3.4
```

Rebuild the dev container so it pulls the new image. Drop and recreate `appdb`:

```
psql "postgresql://app_user:app_password@localhost:5432/postgres" \
  -c "DROP DATABASE IF EXISTS appdb;" -c "CREATE DATABASE appdb;"
```

### 6.2 Enable PostGIS in a migration

```
dotnet ef migrations add EnablePostGIS \
  --project RentalApp.Database --startup-project RentalApp.Migrations
```

Edit the generated migration's `Up()`:

```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
```

### 6.3 NetTopologySuite

`RentalApp.Database.csproj`:

```xml
<PackageReference Include="NetTopologySuite" Version="2.5.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="9.0.0" />
```

`AppDbContext.OnConfiguring`:

```csharp
optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());
```

### 6.4 Item.Location

Add to `Item.cs`:

```csharp
using NetTopologySuite.Geometries;
public Point? Location { get; set; }
```

EF + NetTopologySuite map this to `geography(Point, 4326)`. Add a migration `AddItemLocation`.

### 6.5 ILocationService

```csharp
public interface ILocationService
{
    Task<(double lat, double lon)?> GetCurrentLocationAsync();
}
```

`LocationService` calls `Geolocation.Default.GetLocationAsync()`. Register as singleton.

### 6.6 GetNearbyAsync

API contract — `GET /items/nearby?lat=&lon=&radius=&category=`. Radius in km, max 50.

Add to `IItemRepository`:

```csharp
Task<NearbySearchResult> GetNearbyAsync(
    double lat, double lon, double radiusKm = 5, string? categorySlug = null);
```

Local impl: PostGIS `ST_DWithin` via `i.Location.IsWithinDistance(origin, radiusMetres)`.
API impl: HTTP GET, parse `NearbyResponse` DTO, populate each `Item.DistanceKm` from the server-computed `distance` field.

### 6.7 NearbyItemsPage + tests

VM uses `ILocationService` (mocked in tests). Slider for radius, list of nearby items sorted by distance.

Tests:
- `GetNearbyAsync_LocalImpl_ReturnsItemsWithinRadius`
- `GetNearbyAsync_ApiImpl_PassesRightQueryString`
- `NearbyItemsViewModel_LoadsForCurrentLocation_WithMockedGps`

**Checkpoint:** "Find Near Me" works locally. Coverage ~65%. Commit.

---

## Phase 7 — Rental workflow + State Pattern (Day 15-19, ~15 hours) — DISTINCTION HEADLINE

The set-piece bonus. Plan the state machine first, then the model, then the service, then the UI.

### 7.1 The state machine

| From | To | Who can trigger |
|---|---|---|
| Requested | Approved | Owner |
| Requested | Rejected | Owner |
| Approved | OutForRent | System or Owner |
| OutForRent | Returned | Borrower |
| OutForRent | Overdue | System (timer) |
| Overdue | Returned | Borrower |
| Returned | Completed | Owner |

Anything else → `InvalidStateTransitionException`.

### 7.2 IRentalState + state classes

`RentalApp.Database/States/IRentalState.cs`:

```csharp
public interface IRentalState
{
    RentalStatus Status { get; }
    IRentalState Approve();
    IRentalState Reject();
    IRentalState MarkOutForRent();
    IRentalState MarkReturned();
    IRentalState MarkOverdue();
    IRentalState Complete();
}
```

One class per state. `RequestedState` only allows Approve/Reject; everything else throws. `CompletedState` and `RejectedState` are terminal — every method throws.

Connect to the persisted `Rental.Status` enum via a non-mapped property:

```csharp
[NotMapped]
public IRentalState State => Status switch
{
    RentalStatus.Requested  => new RequestedState(),
    RentalStatus.Approved   => new ApprovedState(),
    RentalStatus.OutForRent => new OutForRentState(),
    RentalStatus.Returned   => new ReturnedState(),
    RentalStatus.Completed  => new CompletedState(),
    RentalStatus.Rejected   => new RejectedState(),
    RentalStatus.Overdue    => new OverdueState(),
    _ => throw new InvalidOperationException()
};
```

### 7.3 RentalService and IRentalRepository

`RentalService` orchestrates. Methods:

- `RequestRentalAsync(itemId, borrowerId, start, end)` — guards: dates valid, no overlap, can't rent your own item, item is `IsAvailable`
- `ApproveRentalAsync(rentalId)` — calls `rental.State.Approve()`, persists new Status, sets `ApprovedAt`
- `RejectRentalAsync`, `MarkOutForRentAsync`, `MarkReturnedAsync`, `MarkOverdueAsync`, `CompleteAsync` — same pattern
- `GetIncomingForOwnerAsync(userId, statusFilter?)`, `GetOutgoingForBorrowerAsync(userId, statusFilter?)`

`IRentalRepository` mirrors the API endpoints:

```csharp
public interface IRentalRepository
{
    Task<Rental> CreateAsync(Rental rental);
    Task<Rental?> GetByIdAsync(int id);
    Task<List<Rental>> GetIncomingForOwnerAsync(int ownerId, RentalStatus? statusFilter = null);
    Task<List<Rental>> GetOutgoingForBorrowerAsync(int borrowerId, RentalStatus? statusFilter = null);
    Task<Rental?> UpdateStatusAsync(int rentalId, RentalStatus newStatus);
}
```

API impl uses `PATCH /rentals/{id}/status` with `{ status: "Out for Rent" }`. The `RentalStatusJsonConverter` handles the string-with-spaces serialisation transparently — that's the whole reason it exists.

Double-booking guard:

```csharp
private async Task<bool> HasOverlapAsync(int itemId, DateTime start, DateTime end)
    => await _ctx.Rentals.AnyAsync(r =>
           r.ItemId == itemId &&
           (r.Status == RentalStatus.Approved || r.Status == RentalStatus.OutForRent) &&
           r.StartDate <= end && r.EndDate >= start);
```

### 7.4 UI

`RentalsPage` with two tabs: Incoming (rentals on items I own) and Outgoing (rentals I requested).

`RentalsViewModel` exposes both lists plus `ApproveCommand`, `RejectCommand`, `MarkOutForRentCommand`, `MarkReturnedCommand`, `CompleteCommand`. Each command's `CanExecute` mirrors the API's permission rules so users only see actions they can actually perform.

Status displayed as a coloured badge (green=Approved, blue=Out for Rent, red=Overdue, grey=Completed/Rejected).

### 7.5 Tests — coverage explosion

- One test per valid transition (~7)
- One test per invalid transition (~10, all assert throws)
- `RequestRentalAsync_DateOverlap_ReturnsFailure`
- `RequestRentalAsync_StartAfterEnd_ReturnsFailure`
- `RequestRentalAsync_OwnRental_ReturnsFailure`
- `ApproveRentalAsync_NotOwner_ReturnsFailure`
- `TotalPrice_CalculatedCorrectly` (rate × inclusive day count)
- API repo tests using `FakeHttpMessageHandler` for each endpoint

Should land coverage at 70-75%. Commit.

---

## Phase 8 — Reviews + push coverage to ≥80% (Day 20-21, ~6 hours)

### 8.1 Reviews

`IReviewRepository`:

```csharp
public interface IReviewRepository
{
    Task<Review> CreateAsync(Review review);
    Task<PagedResult<Review>> GetForItemAsync(int itemId, int page = 1, int pageSize = 10);
    Task<PagedResult<Review>> GetForUserAsync(int userId, int page = 1, int pageSize = 10);
}
```

Server enforces these rules; mirror them locally:
- Rental must be `Completed`
- Reviewer must equal `Rental.BorrowerId`
- One review per rental (DB unique constraint already in place from Phase 4)
- Rating 1-5 inclusive

`ReviewsPage` + `ReviewViewModel` for posting; show `AverageRating` on `ItemDetailPage`.

### 8.2 Coverage push

Run `dotnet test --collect:"XPlat Code Coverage"`, then optionally `reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage` for an HTML report.

Look for gaps:
- Untested error paths (mock repository to throw, assert VM displays error)
- Login/Register VM error branches
- State machine edge cases you missed in Phase 7

Aim ≥80%. Codecov dashboard reflects it. Commit.

---

## Phase 9 — Wire up the API plumbing and flip the toggle (Day 22-24, ~9 hours)

By now every domain feature has both a `Local*Repository` and an `Api*Repository` implementing the same interface. Phase 9 makes the API repositories actually work by adding bearer-token plumbing.

### 9.1 Packages

`RentalApp.csproj`:

```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.6" />
```

### 9.2 ITokenStore + SecureTokenStore

Wraps MAUI `SecureStorage.Default.SetAsync/GetAsync/Remove` for three keys: `jwt.token`, `jwt.expiresAt`, `jwt.userId`.

### 9.3 AuthorizationDelegatingHandler

`DelegatingHandler` registered on every typed `HttpClient`. Adds `Authorization: Bearer <token>` to outgoing requests; clears the token on a 401 response so the user is forced back to login.

### 9.4 ApiAuthenticationService

Implements `IAuthenticationService` against `POST /auth/register` and `POST /auth/token`. After successful login, calls `GET /users/me` to populate the current user, persists token via `ITokenStore`.

### 9.5 DI configuration

```csharp
builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
builder.Services.AddTransient<AuthorizationDelegatingHandler>();

if (FeatureFlags.UseApi)
{
    var apiBase = new Uri("https://set09102-api.b-davison.workers.dev/");
    builder.Services.AddHttpClient<IAuthenticationService, ApiAuthenticationService>(c => c.BaseAddress = apiBase)
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
    builder.Services.AddHttpClient<IItemRepository, ApiItemRepository>(c => c.BaseAddress = apiBase)
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
    builder.Services.AddHttpClient<IRentalRepository, ApiRentalRepository>(c => c.BaseAddress = apiBase)
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
    builder.Services.AddHttpClient<IReviewRepository, ApiReviewRepository>(c => c.BaseAddress = apiBase)
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
}
else
{
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<IItemRepository,   LocalItemRepository>();
    builder.Services.AddScoped<IRentalRepository, LocalRentalRepository>();
    builder.Services.AddScoped<IReviewRepository, LocalReviewRepository>();
}
```

### 9.6 Tests

Add tests using `FakeHttpMessageHandler`:

- `ApiAuthenticationService_LoginAsync_PostsToTokenEndpoint_PersistsToken`
- `ApiAuthenticationService_LoginAsync_On401_ReturnsFailure`
- `AuthorizationDelegatingHandler_AddsBearerHeader_WhenTokenStored`
- `AuthorizationDelegatingHandler_ClearsToken_OnUnauthorizedResponse`

Tests **never** hit the live API — `DatabaseFixture` plus mocked `HttpClient` is the integration boundary.

### 9.7 Manual smoke test

Set `FeatureFlags.UseApi = true`, deploy, register a new user against the live API, list items, create one, log out, log back in. Token persists across app restart.

Commit.

---

## Phase 10 — Distinction polish (Day 25-27)

- **SonarCloud** — Tutorial 7 §3. Address code smells. Pass quality gate.
- **Doxygen** — add XML doc comments to every public method; deploy site to GitHub Pages via the docs workflow.
- **Set-piece** — pick one piece of code to over-document for the viva (the State Pattern transitions or the spatial query are the obvious choices).
- **Optional second bonus** — overdue detection (background timer flips OutForRent → Overdue based on EndDate). Cheap, viva-friendly.

---

## Phase 11 — Submission (Day 28-30)

### 11.1 README.md

- Project overview
- Setup instructions (Docker, dependencies, dev container)
- How to run the app (local mode + API mode)
- How to run tests
- API endpoint link: `https://set09102-api.b-davison.workers.dev/`
- Architecture overview

### 11.2 Architecture diagrams

Tools: draw.io, Mermaid, Lucidchart.

- Component diagram (5 marks)
- Database schema diagram (5 marks)
- Sequence diagram for the rental flow (5 marks)
- State diagram for rental states (5 bonus marks)

### 11.3 The report

Per §10 of the brief — quantitative scoring:

- Cover page (2)
- Architecture (20: 5 component, 5 schema, 5 sequence, 5 state)
- Feature checklist with screenshots (20)
- Testing documentation with coverage screenshot (15)
- CI/CD evidence (10)
- Design patterns explanation with code excerpts (15: MVVM, Repository, Service, +5 bonus for State Pattern)
- AI usage (15: 3 marks tools, 9 marks for ≥3 documented interactions, 3 marks reflection)
- References (3)

PDF, ≤20 pages, 12pt, single/1.5 spaced.

### 11.4 AI usage log

Throughout the project — short summaries of significant AI exchanges:

- "Asked Claude to draft the State Pattern transitions; modified `OverdueState.MarkReturned()` because the suggestion didn't reset the overdue timestamp."
- "Used Copilot to scaffold `RentalRepository` CRUD methods; rewrote `HasOverlapAsync` because the suggested version had an off-by-one on the date comparison."
- "Asked Claude how to handle the API's status string format; verified the dictionary in the converter against the API reference PDF."

### 11.5 Submission checklist

- ☐ Repo public
- ☐ All Tier 1 + Tier 2 features
- ☐ At least one Tier 3 bonus (State Pattern)
- ☐ Tests pass, ≥80% coverage
- ☐ CI green on main
- ☐ README complete with API endpoint link
- ☐ Report PDF, ≤20 pages
- ☐ ≥15 commits over project period
- ☐ AI section in report
- ☐ Viva booked

### 11.6 Viva rehearsal

Practise:

- "Walk me through the State Pattern code." (your set-piece)
- "Why a Repository pattern? Why a Service layer?"
- "Why PostGIS over haversine?"
- "Why dual implementations behind the same interface? What problem does it solve?"
- "Show me a piece of AI-assisted code and explain it."
- "Where's your double-booking guard? Walk me through it."
- "Why is `ILocationService` an interface?" (testability)
- "What happens when the JWT expires mid-session?" (`AuthorizationDelegatingHandler` clears the token on 401)

---

## Reference: Tier 1 / 2 / 3 feature checklist

### Tier 1 (Pass)
- [x] Local user authentication — Phase 1-3
- [ ] API user authentication (JWT) — Phase 9
- [ ] Item CRUD — Phases 4-5
- [ ] Rental request basic — Phase 7
- [x] MVVM (already done)
- [x] Repository pattern (already done)

### Tier 2 (Merit)
- [ ] PostGIS nearby search — Phase 6
- [ ] Rental workflow with state transitions — Phase 7
- [ ] Reviews — Phase 8
- [ ] Service layer — Phases 7-9
- [ ] ≥60% test coverage — Phase 7-8

### Tier 3 (Distinction — pick at least one)
- [ ] **State Pattern** — Phase 7 (your headline bonus)
- [ ] MediatR / CQRS Lite — skip unless ahead
- [ ] Overdue detection — Phase 10 (small bonus)
- [ ] Map UI / radius slider — Phase 6 polish
- [ ] SonarCloud — Phase 10
