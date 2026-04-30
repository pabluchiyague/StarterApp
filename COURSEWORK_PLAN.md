# RentalApp Coursework Plan

**Target grade:** Distinction (≥70%)
**Module:** SET09102 Software Engineering
**Coursework:** Peer-to-peer rental marketplace ("Library of Things")
**API base URL:** https://set09102-api.b-davison.workers.dev/

This is the working plan. Each phase has concrete steps and a verification checkpoint. Tick the boxes as you go. Don't skip checkpoints — they catch problems before they pile up.

---

## Where you are right now

- Solution renamed `StarterApp` → `RentalApp` everywhere in source
- Auth services restored: `IAuthenticationService`, `AuthenticationService`, `AuthResult`, `AuthStateChangedEventArgs`
- Auth models restored: `User`, `Role`, `UserRole`, `RoleConstants`
- `AppDbContext` merges auth + notes tables, seeds the three default roles
- Tutorial 6 tests still target `NoteRepository` and live in `RentalApp.Tests`
- The Windows folder is still named `StarterApp` — rename to `RentalApp` whenever you want (close VS Code first, rename in File Explorer, reopen)

## What's NOT done yet

- DI registration for `IAuthenticationService` in `MauiProgram.cs`
- A migration that creates the auth tables in `appdb`
- Login / Register pages and ViewModels
- The `//login` Shell route
- Anything rental-shaped (`Item`, `Rental`, `Review`, PostGIS)
- API integration plumbing (HttpClient, ITokenStore, AuthorizationDelegatingHandler)
- CI workflow file (Tutorial 7)

---

## API Reference Quick Card

The full PDF reference is `SET09102 2025-6 TR2 001_ API Reference _ Moodle - Edinburgh Napier University.pdf` — keep it open while implementing each endpoint. Live Swagger at <https://set09102-api.b-davison.workers.dev/>.

### Conventions

- **Auth header**: `Authorization: Bearer {token}` on every authenticated request
- **Content type**: `application/json`
- **Token expiry**: 7 days — clear and re-login on 401
- **Date format**: ISO 8601, `yyyy-MM-dd` for date-only fields
- **Rate limits**: 100 req/min, 1000 req/hour per user → 429 if exceeded
- **Errors**: `{ error, message, details? }` JSON shape

### Endpoints, grouped by phase that needs them

**Phase 3 — Auth**
- `POST /auth/register` (no auth) — `{ firstName, lastName, email, password }` → 201 user
- `POST /auth/token` (no auth) — `{ email, password }` → 200 `{ token, expiresAt, userId }`
- `GET /users/me` (auth) → 200 user profile (`averageRating`, `itemsListed`, `rentalsCompleted`)
- `GET /users/{id}/profile` (no auth) → public profile + reviews

**Phase 5 — Items**
- `GET /items?category=&search=&page=&pageSize=` (no auth) — paginated list. **`category` is a slug string** (`tools`, `camping`, `sports`, `electronics`, `games`)
- `GET /items/{id}` (no auth) — detail + embedded reviews
- `POST /items` (auth) — `{ title, description, dailyRate, categoryId, latitude, longitude }` → 201
- `PUT /items/{id}` (auth, **owner only**) — partial update
- `GET /categories` (no auth) → `[{ id, name, slug, itemCount }]`

**Phase 6 — PostGIS spatial**
- `GET /items/nearby?lat=&lon=&radius=&category=` (no auth) — `radius` in km (default 5, max 50). Response includes server-computed `distance` (km)

**Phase 7 — Rental workflow**
- `POST /rentals` (auth) — `{ itemId, startDate, endDate }` → 201; **409** on overlap, **400** on validation
- `GET /rentals/incoming?status=` (auth) — rentals on items I own
- `GET /rentals/outgoing?status=` (auth) — rentals I requested
- `GET /rentals/{id}` (auth, owner or borrower)
- `PATCH /rentals/{id}/status` (auth) — `{ status }` where status is one of these strings (note the spaces):

  `Requested` | `Approved` | `Rejected` | `Out for Rent` | `Overdue` | `Returned` | `Completed`

  Permitted transitions (server enforces; mirror in your State Pattern):

  | From | To | Who |
  |---|---|---|
  | Requested | Approved | Owner |
  | Requested | Rejected | Owner |
  | Approved | Out for Rent | System or Owner |
  | Out for Rent | Returned | Borrower |
  | Out for Rent | Overdue | System (auto) |
  | Overdue | Returned | Borrower |
  | Returned | Completed | Owner |

  400 `Invalid state transition` for illegal moves; 403 for permission failures.

**Phase 8 — Reviews**
- `POST /reviews` (auth, **borrower of a Completed rental, no duplicates**) — `{ rentalId, rating: 1-5, comment? }`
- `GET /items/{id}/reviews?page=&pageSize=` (no auth) — averageRating, totalReviews
- `GET /users/{id}/reviews?page=&pageSize=` (no auth)

### Architectural implications (apply from Phase 4 onwards)

1. **DTOs ≠ domain models.** API responses include joined fields (`ownerName`, `borrowerRating`, `distance`, `category` as slug-string) that don't belong on EF entities. Create `RentalApp/Models/Api/` with `ItemDto`, `RentalDto`, `ReviewDto`, `UserDto`, `LoginRequest`, `LoginResponse`, etc. Map DTO ↔ domain at the repository boundary.
2. **Status string ↔ enum converter.** API's `"Out for Rent"` ≠ a C# enum value. Define a custom `JsonConverter<RentalStatus>` so deserialization works transparently.
3. **Category needs both `id` and `slug`.** Local `Category.Slug` column added in Phase 4. Map slug ↔ id at the API repository boundary.
4. **Pagination.** Every list endpoint is paginated. ViewModels expose `Page`, `PageSize`, `TotalPages`, `LoadMoreCommand`. Don't fetch unbounded.
5. **Token storage.** JWT goes in MAUI `SecureStorage`. Wrap as `ITokenStore` for testability.
6. **Authorization handler.** A `DelegatingHandler` on the typed `HttpClient` adds the bearer token. 401 handling (clear token + force re-login) lives there.
7. **Dual-implementation pattern.** Every repository (`IItemRepository`, `IRentalRepository`, `IReviewRepository`) gets two implementations: `Local*Repository` (DB-backed, for tests + offline) and `Api*Repository` (HttpClient-backed, for runtime). DI toggle picks one at startup. This is what the brief calls for and is the central architectural feature.

---

## Phase 0 — Sanity check the rename (15 min)

The sed pass touched 50+ files. Before doing anything else, verify it builds.

1. In the dev container:
   ```
   cd /workspace
   dotnet restore RentalApp.Tests/RentalApp.Tests.csproj
   dotnet build   RentalApp.Tests/RentalApp.Tests.csproj
   ```
2. Tutorial 6's 5 tests will likely fail at runtime because the auth tables don't exist yet — Phase 1 fixes that. We just need a clean compile here.
3. If build fails, the typical issues are: a stray `StarterApp.something` in a `using`, or a `com.companyname.starterapp` in a platform config.

**Checkpoint:** `dotnet build` returns "Build succeeded".

```
git add -A
git commit -m "Rename StarterApp -> RentalApp; restore auth services and User/Role/UserRole models"
git push
```

---

## Phase 1 — Wire up auth + first migration (Day 1, ~3 hours)

### 1.1 Register `IAuthenticationService` in DI

Edit `RentalApp/MauiProgram.cs`. Above the `INoteRepository` line:

```csharp
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
```

Add `using RentalApp.Services;` at the top if missing.

### 1.2 Add the auth migration

```
cd /workspace
dotnet ef migrations add AddAuthSchema \
  --project RentalApp.Database \
  --startup-project RentalApp.Migrations
```

Inspect the generated migration. It should `CreateTable` for `users`, `role`, `user_role`, plus `InsertData` for the three roles. Adjust if anything is off (use `dotnet ef migrations remove` to back out and try again).

### 1.3 Apply

```
dotnet ef database update \
  --project RentalApp.Database \
  --startup-project RentalApp.Migrations
```

Verify in psql:
```
psql "$CONNECTION_STRING" -c '\dt'
psql "$CONNECTION_STRING" -c 'SELECT * FROM role;'
```
You should see `users`, `role`, `user_role` plus the existing `notes`/`categories`, and three rows in `role`.

### 1.4 Re-run Tutorial 6 tests

```
dotnet test RentalApp.Tests/RentalApp.Tests.csproj
```
The fixture drops/recreates `testappdb` so it picks up the new schema. Should still be **5 passed**.

**Checkpoint:** Auth tables exist. Three roles seeded. Tutorial 6 tests still green.

```
git commit -am "Phase 1: AddAuthSchema migration + AuthenticationService DI registration"
git push
```

---

## Phase 2 — Tutorial 7: GitHub Actions CI/CD (Day 2, ~3 hours)

Get the pipeline green before adding more features.

### 2.1 Confirm `CODECOV_TOKEN` is in repo secrets

`https://github.com/pabluchiyague/StarterApp/settings/secrets/actions` — and **regenerate** the token first since the previous one is in chat history.

### 2.2 Create `.github/workflows/build.yml`

```yaml
name: Build & Test

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_USER: app_user
          POSTGRES_PASSWORD: app_password
          POSTGRES_DB: appdb
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0

      - name: Restore
        run: dotnet restore RentalApp.Tests/RentalApp.Tests.csproj

      - name: Build
        run: dotnet build RentalApp.Tests/RentalApp.Tests.csproj --configuration Debug --no-restore

      - name: Test
        env:
          CONNECTION_STRING: Host=localhost;Port=5432;Username=app_user;Password=app_password;Database=appdb
        run: dotnet test RentalApp.Tests/RentalApp.Tests.csproj --configuration Debug --no-build --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v5
        with:
          token: ${{ secrets.CODECOV_TOKEN }}
          files: RentalApp.Tests/TestResults/**/coverage.cobertura.xml
          fail_ci_if_error: false

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: RentalApp.Tests/TestResults/
```

### 2.3 PR + merge

```
git checkout -b feature/cicd
git add .github/workflows/build.yml
git commit -m "Tutorial 7: GitHub Actions build + test workflow"
git push -u origin feature/cicd
```
Open a PR. Watch the workflow. Merge when green.

**Checkpoint:** Green ✓ on `main`. Codecov dashboard populated.

---

## Phase 3 — Login + Register pages (Day 3-4, ~6 hours)

### 3.1 `LoginViewModel`

`RentalApp/ViewModels/LoginViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _auth;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;

    public LoginViewModel(IAuthenticationService auth, INavigationService navigation)
    {
        _auth = auth;
        _navigation = navigation;
        Title = "Sign in";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email and password are required.";
            return;
        }
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            var result = await _auth.LoginAsync(Email, Password);
            if (!result.IsSuccess) { ErrorMessage = result.Message; return; }
            await Shell.Current.GoToAsync("//items");  // post-login → main page (will be //items after Phase 5; for now use //notes)
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync() => await _navigation.NavigateToAsync("register");
}
```

### 3.2 `LoginPage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage x:Class="RentalApp.Views.LoginPage"
             xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             Title="{Binding Title}">
    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="16" VerticalOptions="Center">
            <Label Text="Sign in to RentalApp" FontSize="24" FontAttributes="Bold" HorizontalOptions="Center" />
            <Label Text="{Binding ErrorMessage}" TextColor="Red" IsVisible="{Binding HasError}" />
            <Entry Placeholder="Email"    Text="{Binding Email}"    Keyboard="Email" />
            <Entry Placeholder="Password" Text="{Binding Password}" IsPassword="True" />
            <Button Text="Sign in" Command="{Binding LoginCommand}" IsEnabled="{Binding IsNotBusy}" />
            <Button Text="Create an account" Command="{Binding GoToRegisterCommand}" BackgroundColor="Transparent" />
            <ActivityIndicator IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

`LoginPage.xaml.cs` is the standard pair (sets `BindingContext` from DI). Mirror existing pages.

### 3.3 RegisterPage / RegisterViewModel — same shape

Fields: FirstName, LastName, Email, Password, ConfirmPassword. VM calls `_auth.RegisterAsync(...)`, on success navigates back to login (or auto-login).

### 3.4 Register routes in `AppShell.xaml.cs`

```csharp
Routing.RegisterRoute("login",    typeof(LoginPage));
Routing.RegisterRoute("register", typeof(RegisterPage));
```

Add a `<ShellContent>` for `//login` so `NavigationService.NavigateToRootAsync()` works.

### 3.5 Register pages + VMs in `MauiProgram.cs`

```csharp
builder.Services.AddTransient<LoginViewModel>();
builder.Services.AddTransient<LoginPage>();
builder.Services.AddTransient<RegisterViewModel>();
builder.Services.AddTransient<RegisterPage>();
```

### 3.6 Test it

Build, deploy. Register a user, log in. In psql:
```
psql "$CONNECTION_STRING" -c 'SELECT id, "Email", "FirstName" FROM users;'
psql "$CONNECTION_STRING" -c 'SELECT u."Email", r."Name" FROM users u JOIN user_role ur ON u."Id"=ur."UserId" JOIN role r ON r."Id"=ur."RoleId";'
```

### 3.7 AuthenticationService tests

`RentalApp.Tests/AuthenticationServiceTests.cs`:
- `RegisterAsync_NewUser_CreatesUserAndAssignsDefaultRole`
- `LoginAsync_ValidCredentials_ReturnsSuccess`
- `LoginAsync_WrongPassword_ReturnsFailure`
- `LoginAsync_UnknownEmail_ReturnsFailure`
- `RegisterAsync_DuplicateEmail_ReturnsFailure`

Pattern match `NoteRepositoryTests`. Use the same `DatabaseFixture`.

**Checkpoint:** Login flow works. ≥10 tests in CI, all green.

```
git commit -am "Phase 3: LoginPage / RegisterPage + AuthenticationService tests"
```

---

## Phase 4 — Domain reset: Notes out, Rentals in (Day 5-7, ~10 hours)

### 4.1 Delete note-shaped files

```
rm RentalApp/Views/Note*.xaml RentalApp/Views/Note*.xaml.cs
rm RentalApp/Views/Notes*.xaml RentalApp/Views/Notes*.xaml.cs
rm RentalApp/ViewModels/Note*.cs RentalApp/ViewModels/Notes*.cs
rm RentalApp.Database/Models/Note.cs RentalApp.Database/Models/NoteImportance.cs
rm RentalApp.Database/Repositories/INoteRepository.cs
rm RentalApp.Database/Repositories/NoteRepository.cs
rm RentalApp.Database/Repositories/ApiNoteRepository.cs
rm RentalApp.Tests/NoteRepositoryTests.cs
```

`Category` keeps existing — repurposed.

### 4.2 New domain models in `RentalApp.Database/Models/`

- **Item.cs** — Id, Title, Description, DailyRate (decimal), CategoryId, OwnerId (FK User), CreatedAt, UpdatedAt, IsAvailable. `Location` Point added in Phase 6.
- **Rental.cs** — Id, ItemId, BorrowerId, StartDate, EndDate, Status (RentalStatus enum), TotalPrice (decimal), CreatedAt, UpdatedAt, ApprovedAt?
- **Review.cs** — Id, RentalId, ReviewerId, Rating (int 1-5), Comment, CreatedAt
- **RentalStatus.cs** — `enum { Requested, Approved, Rejected, OutForRent, Overdue, Returned, Completed }`
- Update **Category.cs** — add `Slug` (string, lowercase identifier matching API), drop the `Notes` navigation list, add `Items` navigation list

### 4.3 RentalStatusJsonConverter

`RentalApp/Models/Api/RentalStatusJsonConverter.cs`:

```csharp
public class RentalStatusJsonConverter : JsonConverter<RentalStatus>
{
    private static readonly Dictionary<string, RentalStatus> FromString = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Requested"]    = RentalStatus.Requested,
        ["Approved"]     = RentalStatus.Approved,
        ["Rejected"]     = RentalStatus.Rejected,
        ["Out for Rent"] = RentalStatus.OutForRent,
        ["Overdue"]      = RentalStatus.Overdue,
        ["Returned"]     = RentalStatus.Returned,
        ["Completed"]    = RentalStatus.Completed,
    };
    private static readonly Dictionary<RentalStatus, string> ToString = FromString.ToDictionary(kv => kv.Value, kv => kv.Key);

    public override RentalStatus Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o) => FromString[reader.GetString()!];
    public override void Write(Utf8JsonWriter writer, RentalStatus value, JsonSerializerOptions o) => writer.WriteStringValue(ToString[value]);
}
```

### 4.4 ApiJson options helper

`RentalApp/Models/Api/ApiJson.cs`:

```csharp
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new RentalStatusJsonConverter() }
    };
}
```

### 4.5 Update `AppDbContext.cs`

- Drop `DbSet<Note>` and the Note `OnModelCreating` config
- Add `DbSet<Item>`, `DbSet<Rental>`, `DbSet<Review>`
- Configure FKs and indices: `Item.OwnerId` → `users.Id`, `Rental.ItemId` → `items.Id`, etc.
- Update `SeedCategories` to rental categories with slugs:

```csharp
new Category { Id = 1, Name = "Tools",       Slug = "tools",       ColorHex = "#F44336", Description = "Power tools, hand tools" },
new Category { Id = 2, Name = "Camping",     Slug = "camping",     ColorHex = "#4CAF50", Description = "Tents, stoves, sleeping bags" },
new Category { Id = 3, Name = "Sports",      Slug = "sports",      ColorHex = "#2196F3", Description = "Bikes, skis, sports gear" },
new Category { Id = 4, Name = "Electronics", Slug = "electronics", ColorHex = "#9C27B0", Description = "Cameras, projectors, audio" },
new Category { Id = 5, Name = "Games",       Slug = "games",       ColorHex = "#FF9800", Description = "Board games, party games" }
```

### 4.6 Squash and re-run migrations (cleanest path)

```
rm -rf RentalApp.Database/Migrations
psql "$CONNECTION_STRING" -c "DROP DATABASE IF EXISTS appdb;" -c "CREATE DATABASE appdb;"

dotnet ef migrations add InitialRentalSchema \
  --project RentalApp.Database --startup-project RentalApp.Migrations

dotnet ef database update \
  --project RentalApp.Database --startup-project RentalApp.Migrations
```

(If you'd rather keep history, add `DropNotes` then `AddRentalSchema` migrations sequentially.)

### 4.7 Update `DatabaseFixture` and write a placeholder rental test

Adjust `Seed()` to insert a test user, a category (already seeded), and one item. Drop the obsolete `NoteRepositoryTests.cs`.

For now, write a single sanity test like `Categories_AreSeeded_ReturnsFive` so the suite isn't empty.

**Checkpoint:** Build green. Tests green (with the placeholder). Schema in psql shows `users`, `role`, `user_role`, `categories`, `items`, `rentals`, `reviews`. Coverage will dip — that's fine, we're rebuilding.

```
git commit -am "Phase 4: Domain reset; Item/Rental/Review models; fresh InitialRentalSchema"
```

---

## Phase 5 — Items UI + dual repository (Day 8-10, ~9 hours)

### 5.1 API DTOs

In `RentalApp/Models/Api/`:

- `ItemSummaryDto` — id, title, description, dailyRate, categoryId, category (slug), ownerId, ownerName, ownerRating, isAvailable, averageRating, imageUrl, createdAt
- `ItemDetailDto` — adds latitude, longitude, totalReviews, embedded reviews list
- `CreateItemRequest` — title, description, dailyRate, categoryId, latitude, longitude
- `UpdateItemRequest` — optional title, description, dailyRate, isAvailable
- `PagedResponse<T>` — items, totalItems, page, pageSize, totalPages
- `CategoryDto` — id, name, slug, itemCount

### 5.2 `IItemRepository`

`RentalApp.Database/Repositories/IItemRepository.cs`:

```csharp
public interface IItemRepository
{
    Task<PagedResult<Item>> GetItemsAsync(string? categorySlug = null, string? search = null, int page = 1, int pageSize = 20);
    Task<Item?> GetItemByIdAsync(int id);
    Task<Item> CreateItemAsync(Item item);
    Task<Item?> UpdateItemAsync(int id, Item updates);
    Task<List<Item>> GetByOwnerAsync(int ownerId);
    // GetNearbyAsync added in Phase 6
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalItems, int Page, int PageSize, int TotalPages);
```

### 5.3 `LocalItemRepository` + tests

DB-backed. Translate `categorySlug` → `categoryId` by joining on `Category.Slug`. Returns `Item` with `Category` and `Owner` navigations loaded.

Tests in `RentalApp.Tests/LocalItemRepositoryTests.cs`:
- `CreateItemAsync_AssignsId`
- `GetItemByIdAsync_ExistingItem_ReturnsWithCategoryAndOwner`
- `GetItemByIdAsync_MissingItem_ReturnsNull`
- `GetItemsAsync_FilteredBySlug_ReturnsOnlyMatching`
- `GetItemsAsync_Pagination_RespectsPageSize`
- `UpdateItemAsync_PartialUpdate_PreservesUntouchedFields`

### 5.4 `ApiItemRepository`

`RentalApp.Database/Repositories/ApiItemRepository.cs`:

```csharp
public class ApiItemRepository : IItemRepository
{
    private readonly HttpClient _http;
    public ApiItemRepository(HttpClient http) { _http = http; }

    public async Task<PagedResult<Item>> GetItemsAsync(string? categorySlug, string? search, int page, int pageSize)
    {
        var qs = new List<string>();
        if (!string.IsNullOrEmpty(categorySlug)) qs.Add($"category={Uri.EscapeDataString(categorySlug)}");
        if (!string.IsNullOrEmpty(search))       qs.Add($"search={Uri.EscapeDataString(search)}");
        qs.Add($"page={page}");
        qs.Add($"pageSize={pageSize}");
        var url = $"items?{string.Join("&", qs)}";

        var resp = await _http.GetFromJsonAsync<PagedResponse<ItemSummaryDto>>(url, ApiJson.Options);
        return new PagedResult<Item>(
            resp!.Items.Select(MapToDomain).ToList(),
            resp.TotalItems, resp.Page, resp.PageSize, resp.TotalPages);
    }

    public async Task<Item?> GetItemByIdAsync(int id)
    {
        var resp = await _http.GetAsync($"items/{id}");
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<ItemDetailDto>(ApiJson.Options);
        return MapDetailToDomain(dto!);
    }

    public async Task<Item> CreateItemAsync(Item item)
    {
        var req = new CreateItemRequest(item.Title, item.Description, item.DailyRate,
                                        item.CategoryId, item.Location?.Y ?? 0, item.Location?.X ?? 0);
        var resp = await _http.PostAsJsonAsync("items", req, ApiJson.Options);
        resp.EnsureSuccessStatusCode();
        var created = await resp.Content.ReadFromJsonAsync<ItemDetailDto>(ApiJson.Options);
        return MapDetailToDomain(created!);
    }

    // ... UpdateItemAsync uses PUT /items/{id}
    // ... GetByOwnerAsync — API doesn't have this; either filter client-side from /items or skip
    // ... mapping helpers DTO ↔ domain
}
```

Tests use a `FakeHttpMessageHandler`:

```csharp
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> r) { _responder = r; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        => Task.FromResult(_responder(req));
}
```

Verify URL/query, request body shape, and response mapping.

### 5.5 DI toggle

In `MauiProgram.cs` near the top:

```csharp
const bool useApi = false;  // flip to true once Phase 9 wires the bearer token
```

```csharp
if (useApi)
{
    builder.Services.AddHttpClient<IItemRepository, ApiItemRepository>(c =>
        c.BaseAddress = new Uri("https://set09102-api.b-davison.workers.dev/"));
    // .AddHttpMessageHandler<AuthorizationDelegatingHandler>();   // wired in Phase 9
}
else
{
    builder.Services.AddScoped<IItemRepository, LocalItemRepository>();
}
```

### 5.6 Views and ViewModels

- `ItemsListPage` + `ItemsListViewModel` — browse with category dropdown
- `ItemDetailPage` + `ItemDetailViewModel`
- `CreateItemPage` + `CreateItemViewModel`

### 5.7 Wire into Shell

```csharp
Routing.RegisterRoute("itemDetail", typeof(ItemDetailPage));
Routing.RegisterRoute("createItem", typeof(CreateItemPage));
```

Update `AppShell.xaml` so Items is the main tab. In `LoginViewModel`, post-login redirect: `await Shell.Current.GoToAsync("//items");`.

### 5.8 ViewModel tests

Mock `IItemRepository` and `IAuthenticationService`. Add `Moq` to test csproj:
```xml
<PackageReference Include="Moq" Version="4.20.70" />
```

Tests:
- `LoadAsync_PopulatesItemsList`
- `IsOwner_WhenCurrentUserIsOwner_ReturnsTrue`
- `IsOwner_WhenAnonymous_ReturnsFalse`

**Checkpoint:** With `useApi=false`, browse/view/create items end-to-end against local DB. Tests green. Coverage ~50-55%.

```
git commit -am "Phase 5: Items UI + dual local/API IItemRepository; VM and repo tests"
```

---

## Phase 6 — PostGIS spatial search (Day 11-13, ~9 hours)

### 6.1 Switch to PostGIS image

`docker-compose.yml`: change `image: postgres:16` → `image: postgis/postgis:16-3.4`.

`.github/workflows/build.yml`: same change in the service container.

Rebuild dev container. Drop and recreate appdb.

### 6.2 Enable PostGIS in migrations

```
dotnet ef migrations add EnablePostGIS \
  --project RentalApp.Database --startup-project RentalApp.Migrations
```

Edit the migration's `Up()` to start with:
```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
```

### 6.3 Add NetTopologySuite

`RentalApp.Database.csproj`:
```xml
<PackageReference Include="NetTopologySuite" Version="2.5.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="9.0.0" />
```

In `AppDbContext.OnConfiguring`:
```csharp
optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());
```

### 6.4 Add `Location` to `Item`

```csharp
using NetTopologySuite.Geometries;
public Point? Location { get; set; }

[NotMapped]
public double? DistanceKm { get; set; }   // populated by GetNearbyAsync
```

Add migration `AddItemLocation`, apply.

### 6.5 `ILocationService` + `LocationService`

`RentalApp/Services/ILocationService.cs`:
```csharp
public interface ILocationService
{
    Task<(double lat, double lon)?> GetCurrentLocationAsync();
}
```

`LocationService.cs` uses MAUI `Geolocation.Default.GetLocationAsync()`. Critical: it's an interface so tests can mock it.

```csharp
builder.Services.AddSingleton<ILocationService, LocationService>();
```

### 6.6 Add `GetNearbyAsync` to `IItemRepository`

```csharp
Task<NearbySearchResult> GetNearbyAsync(double lat, double lon, double radiusKm = 5, string? categorySlug = null);
```

`NearbySearchResult` — `Items`, `SearchLocation` (lat, lon), `Radius`, `TotalResults`.

**LocalItemRepository.GetNearbyAsync**:
```csharp
public async Task<NearbySearchResult> GetNearbyAsync(double lat, double lon, double radiusKm, string? categorySlug)
{
    var origin = new Point(lon, lat) { SRID = 4326 };
    var radiusMetres = radiusKm * 1000;

    var query = _context.Items
        .Include(i => i.Category)
        .Where(i => i.Location != null && i.Location.IsWithinDistance(origin, radiusMetres));

    if (!string.IsNullOrEmpty(categorySlug))
        query = query.Where(i => i.Category!.Slug == categorySlug);

    var results = await query
        .Select(i => new { Item = i, DistanceMetres = i.Location!.Distance(origin) })
        .OrderBy(x => x.DistanceMetres)
        .ToListAsync();

    var items = results.Select(r => { r.Item.DistanceKm = r.DistanceMetres / 1000; return r.Item; }).ToList();
    return new NearbySearchResult(items, lat, lon, radiusKm, items.Count);
}
```

**ApiItemRepository.GetNearbyAsync** — `GET /items/nearby?lat=&lon=&radius=&category=`, parse `NearbyResponse` DTO, populate `DistanceKm` from each item's `distance` field.

### 6.7 `NearbyItemsPage` + VM

VM constructor: `ILocationService` + `IItemRepository`. On page appear, `var loc = await _location.GetCurrentLocationAsync();` → call `GetNearbyAsync`. Slider for radius.

### 6.8 Tests

- `GetNearbyAsync_ItemsWithinRadius_AreReturned` (insert items at known coords, query, assert)
- `NearbyItemsViewModel_LoadsItemsForCurrentLocation` (mock `ILocationService` to return Edinburgh's coordinates)

**Checkpoint:** "Find Near Me" works locally. Coverage ~60%.

```
git commit -am "Phase 6: PostGIS + ILocationService + NearbyItemsPage; spatial query tests"
```

---

## Phase 7 — Rental workflow + State Pattern (Day 14-18, ~15 hours) — DISTINCTION-CRITICAL

### 7.1 Design

Allowed transitions (mirror the API; see Quick Card above). Invalid transitions throw `InvalidStateTransitionException`.

### 7.2 Create the state classes

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

One class per state: `RequestedState`, `ApprovedState`, `RejectedState`, `OutForRentState`, `OverdueState`, `ReturnedState`, `CompletedState`.

`InvalidStateTransitionException` with message like `"Cannot transition from Approved to Rejected"`.

### 7.3 Connect to `Rental`

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

### 7.4 `RentalService`

Methods:
- `RequestRentalAsync(itemId, borrowerId, start, end)` — guards: dates valid, no overlap, item exists, can't rent your own
- `ApproveRentalAsync(rentalId)` — calls `rental.State.Approve()`, updates Status, persists
- `RejectRentalAsync`, `MarkOutForRentAsync`, `MarkReturnedAsync`, `MarkOverdueAsync`, `CompleteAsync`
- Authorization checks against `IAuthenticationService.CurrentUser` (reject if non-owner tries to Approve, etc.)

Double-booking guard:
```csharp
private async Task<bool> HasOverlapAsync(int itemId, DateTime start, DateTime end)
{
    return await _rentals.AnyAsync(r =>
        r.ItemId == itemId &&
        (r.Status == RentalStatus.Approved || r.Status == RentalStatus.OutForRent) &&
        r.StartDate <= end && r.EndDate >= start);
}
```

### 7.5 UI + dual repository

`IRentalRepository`:
```csharp
Task<Rental> CreateAsync(Rental rental);
Task<Rental?> GetByIdAsync(int id);
Task<List<Rental>> GetIncomingForOwnerAsync(int ownerId, RentalStatus? statusFilter = null);
Task<List<Rental>> GetOutgoingForBorrowerAsync(int borrowerId, RentalStatus? statusFilter = null);
Task<Rental?> UpdateStatusAsync(int rentalId, RentalStatus newStatus);
```

`LocalRentalRepository` — DB-backed, runs the State Pattern transitions in `UpdateStatusAsync`.

`ApiRentalRepository` — calls `POST /rentals`, `GET /rentals/incoming?status=`, `GET /rentals/outgoing?status=`, `PATCH /rentals/{id}/status`. Uses `RentalStatusJsonConverter` so the enum maps to `"Out for Rent"` etc.

UI:
- `RentalsPage` with two tabs: Incoming and Outgoing
- `RentalsViewModel` exposes both lists, plus `ApproveCommand`, `RejectCommand`, `MarkOutForRentCommand`, `MarkReturnedCommand`, `CompleteCommand`
- Status displayed as colored badge (green=Approved, blue=Out for Rent, red=Overdue, grey=Completed/Rejected)
- Permission UI: only show owner-side commands if `_auth.CurrentUser.Id == rental.OwnerId`; only borrower-side commands otherwise

### 7.6 Tests (drives coverage up)

- One test per valid transition (~7 tests)
- One test per invalid transition (~10 tests, all assert throws)
- `RequestRentalAsync_DateOverlap_ReturnsFailure`
- `RequestRentalAsync_StartAfterEnd_ReturnsFailure`
- `RequestRentalAsync_OwnRental_ReturnsFailure`
- `ApproveRentalAsync_NotOwner_ReturnsFailure`
- `TotalPrice_CalculatedCorrectly` (rate × days)

You should be at **70%+ coverage** after this phase.

**Checkpoint:** Full rental workflow operational. State Pattern code is clean, well-commented, viva-ready.

```
git commit -am "Phase 7: Rental workflow with State Pattern; comprehensive transition tests"
```

---

## Phase 8 — Reviews + push coverage to ≥80% (Day 19-20, ~6 hours)

### 8.1 Reviews

`IReviewRepository`:
```csharp
Task<Review> CreateAsync(Review review);
Task<PagedResult<Review>> GetForItemAsync(int itemId, int page = 1, int pageSize = 10);
Task<PagedResult<Review>> GetForUserAsync(int userId, int page = 1, int pageSize = 10);
```

`LocalReviewRepository` — DB-backed. Enforce: rental must be Completed; reviewer must be borrower; one review per rental (DB unique constraint on RentalId).

`ApiReviewRepository` — `POST /reviews`, `GET /items/{id}/reviews`, `GET /users/{id}/reviews`.

`ReviewsPage` + `ReviewViewModel` — submit form (after rental Completed), display list. Show average rating on `ItemDetailPage`.

### 8.2 Coverage push to ≥80%

Run `dotnet test --collect:"XPlat Code Coverage"`, look at `RentalApp.Tests/TestResults/*/coverage.cobertura.xml` (or use ReportGenerator to make HTML). Find the gaps:
- Untested error paths in services (mock the repository to throw, assert handling)
- Untested ViewModels (LoginViewModel error branch, RegisterViewModel validation)
- Edge cases on `IRentalState` you might've missed

**Checkpoint:** Coverage ≥80%, GHA still green, Codecov reflects it.

```
git commit -am "Phase 8: Reviews + tests to ≥80% coverage"
```

---

## Phase 9 — Wire up the API plumbing and flip the toggle (Day 21-23, ~9 hours)

By now every feature has both `Local*Repository` (DB-backed, used by tests) and `Api*Repository` (HttpClient-backed, written but not yet usable because no JWT).

### 9.1 Packages

`RentalApp.csproj`:
```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.6" />
```

### 9.2 `ITokenStore` + `SecureTokenStore`

`RentalApp/Services/ITokenStore.cs`:
```csharp
public interface ITokenStore
{
    Task<string?> GetTokenAsync();
    Task<DateTime?> GetExpiryAsync();
    Task<int?> GetUserIdAsync();
    Task SetAsync(string token, DateTime expiresAt, int userId);
    Task ClearAsync();
}
```

`SecureTokenStore.cs` — wraps `SecureStorage.Default.SetAsync/GetAsync/Remove` with three keys (`jwt.token`, `jwt.expiresAt`, `jwt.userId`).

### 9.3 `AuthorizationDelegatingHandler`

```csharp
public class AuthorizationDelegatingHandler : DelegatingHandler
{
    private readonly ITokenStore _tokens;
    public AuthorizationDelegatingHandler(ITokenStore tokens) { _tokens = tokens; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokens.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            await _tokens.ClearAsync();   // expired/invalid → force re-login
        return response;
    }
}
```

### 9.4 `ApiAuthenticationService`

Implements `IAuthenticationService` against `POST /auth/register` and `POST /auth/token`. After login, persist token. After register, immediately call login.

Key calls:
```csharp
// LoginAsync
var resp = await _http.PostAsJsonAsync("auth/token", new LoginRequest(email, password), ApiJson.Options);
if (!resp.IsSuccessStatusCode) return new AuthenticationResult(false, await ExtractMessageAsync(resp));
var body = await resp.Content.ReadFromJsonAsync<LoginResponse>(ApiJson.Options);
await _tokens.SetAsync(body!.Token, body.ExpiresAt, body.UserId);
var me = await _http.GetFromJsonAsync<UserDto>("users/me", ApiJson.Options);
_currentUser = MapToDomain(me!);
```

### 9.5 DI configuration

```csharp
const bool useApi = true;   // flip the whole app

builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
builder.Services.AddTransient<AuthorizationDelegatingHandler>();

if (useApi)
{
    var apiBase = new Uri("https://set09102-api.b-davison.workers.dev/");
    builder.Services.AddHttpClient<IAuthenticationService, ApiAuthenticationService>(c => c.BaseAddress = apiBase)
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
    builder.Services.AddHttpClient<IItemRepository,        ApiItemRepository>(c => c.BaseAddress = apiBase)
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
    builder.Services.AddHttpClient<IRentalRepository,      ApiRentalRepository>(c => c.BaseAddress = apiBase)
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
    builder.Services.AddHttpClient<IReviewRepository,      ApiReviewRepository>(c => c.BaseAddress = apiBase)
        .AddHttpMessageHandler<AuthorizationDelegatingHandler>();
}
else
{
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<IItemRepository,        LocalItemRepository>();
    builder.Services.AddScoped<IRentalRepository,      LocalRentalRepository>();
    builder.Services.AddScoped<IReviewRepository,      LocalReviewRepository>();
}
```

### 9.6 Tests with mocked HttpClient

Add tests using `FakeHttpMessageHandler`:
- `ApiAuthenticationService.LoginAsync_PostsToTokenEndpoint_PersistsToken`
- `ApiAuthenticationService.LoginAsync_On401_ReturnsFailure`
- `AuthorizationDelegatingHandler_AddsBearerHeader_WhenTokenStored`
- `AuthorizationDelegatingHandler_ClearsToken_OnUnauthorizedResponse`
- `ApiItemRepository.GetItemsAsync_BuildsCorrectQueryString`
- `RentalStatusJsonConverter_RoundTrip_OutForRent` ↔ `RentalStatus.OutForRent`

Tests **always** use the local repositories for integration tests — that's what `DatabaseFixture` is for. Don't hit the live API in CI.

### 9.7 End-to-end smoke test against the real API (manual)

1. Set `useApi = true`
2. Deploy to emulator
3. Register a new user → verify token in SecureStorage (use `await SecureStorage.Default.GetAsync("jwt.token")` from a debug button if needed)
4. List items — fetched from API
5. Create an item — POSTed and shows in subsequent fetches
6. Log out — token cleared
7. Log back in — token persists across app restart

**Checkpoint:** App in API mode performs the full happy path. Test suite green at ≥80% with `useApi=false` (which is what tests use).

```
git commit -am "Phase 9: ITokenStore, AuthorizationDelegatingHandler, ApiAuthenticationService; DI toggle live"
```

---

## Phase 10 — Distinction polish (Day 24-26)

### 10.1 SonarCloud

Tutorial 7 §3. Account → organisation → project → token. Add to workflow. Address code smells. Pass quality gate.

### 10.2 Doxygen

Add the Documentation workflow per Tutorial 7. Add XML doc comments to every public method (auto-generate skeletons via Cursor/Copilot, then flesh out).

### 10.3 Pick one set-piece for the viva

Code you over-document — most likely the State Pattern transition logic OR the spatial query. Add detailed XML comments + a paragraph comment block walking through the algorithm. This is what you'll be asked to "explain in detail" during the viva.

### 10.4 (Optional) Overdue detection

Cheap additional bonus: a background check (timer) that scans `OutForRent` rentals where `EndDate < UtcNow.Date`, flips them to `Overdue`. Small, easy, adds story.

**Checkpoint:** Sonar quality gate green. Doxygen site deployed to GitHub Pages. ≥80% coverage. ≥15 commits.

---

## Phase 11 — Submission (Day 27-30)

### 11.1 README.md

Per the brief:
- Project overview
- Setup instructions (Docker, DB, dependencies)
- How to run the app (dev mode + API mode)
- How to run tests
- API endpoint link: `https://set09102-api.b-davison.workers.dev/`
- Architecture overview

### 11.2 Architecture diagrams

- Component diagram (5 marks) — MAUI App → ViewModels → Services → Repositories (Local | API) → DB / API
- Database schema (5 marks) — generate from psql or hand-draw
- Sequence diagram for "rental flow" (5 marks) — Borrower requests → Owner approves → Out for Rent → Returned → Completed → Review
- State diagram for rental states (5 bonus marks) — direct render of your State Pattern

Tools: draw.io, Mermaid in markdown, Lucidchart — pick what you're comfortable with.

### 11.3 Write the report

Per §10 of the brief. **Quantitative scoring** — just tick every box:
- Cover page (2)
- Architecture (20: 5 component, 5 schema, 5 sequence, 5 state)
- Feature checklist with screenshots (20)
- Testing documentation with coverage screenshot (15)
- CI/CD evidence (10)
- Design patterns explanation with code excerpts (15: MVVM, Repository, Service, +5 bonus for State Pattern)
- AI usage (15: 3 marks tools, 9 marks for ≥3 documented interactions, 3 marks reflection)
- References (3)

Max 20 pages PDF, 12pt single/1.5 spaced.

### 11.4 AI usage log

Keep a running document throughout the project. Examples:
- "Asked Claude to draft the State Pattern transitions; modified `OverdueState.MarkReturned()` because the suggestion didn't reset the overdue timestamp."
- "Used Copilot to scaffold `RentalRepository` CRUD methods; rewrote `HasOverlapAsync` because the suggested version had an off-by-one on date comparison."
- "Asked ChatGPT to explain ST_DWithin; verified against PostGIS docs before using."

### 11.5 Submission checklist

- ☐ Repo public, named appropriately
- ☐ All Tier 1 + Tier 2 features
- ☐ At least one Tier 3 bonus (State Pattern)
- ☐ Tests pass, ≥80% coverage
- ☐ CI green on `main`
- ☐ README complete with API endpoint link
- ☐ Report PDF, ≤20 pages
- ☐ ≥15 commits over project period
- ☐ AI section in report
- ☐ Viva booked

### 11.6 Viva rehearsal

Practice answers for:
- "Walk me through the State Pattern code." (your set-piece)
- "Why a Repository pattern? Why a Service layer?"
- "Why PostGIS over haversine?"
- "Why dual implementations (Local + API) behind the same interface? What problem does it solve?"
- "Show me a piece of AI-assisted code and explain it."
- "Where's your double-booking guard? Walk me through it."
- "Why is `ILocationService` an interface?" (testability — mocked in `NearbyItemsViewModel` tests)
- "What happens when the JWT expires mid-session?" (`AuthorizationDelegatingHandler` clears the token on 401)

---

## Reference: Tier 1 / 2 / 3 feature checklist

### Tier 1 (Pass)
- [x] Local user authentication — done in Phase 1-3
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
- [ ] MediatR / CQRS Lite — skip unless ahead of schedule
- [ ] Overdue detection — Phase 10 (small bonus)
- [ ] Map UI / radius slider — Phase 6 polish
- [ ] SonarCloud — Phase 10

---

## Day-zero next actions

1. Open VS Code in the dev container
2. `cd /workspace`
3. `dotnet build RentalApp.Tests/RentalApp.Tests.csproj` — verify rename is clean
4. Run Phase 1 (DI registration + AddAuthSchema migration)
5. Open a feature branch, push, PR for Tutorial 7 (Phase 2) — get CI green
6. Merge → start Phase 3

When you hit a wall, message with:
- What you were trying to do
- What you typed
- What you saw
- (For build errors) the first 30 lines of the failure
