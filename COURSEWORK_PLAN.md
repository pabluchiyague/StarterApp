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

