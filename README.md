# RentalApp

RentalApp is a .NET MAUI peer-to-peer rental marketplace for the SET09102
Software Engineering coursework. Users can register, log in, browse items,
create listings, request rentals, manage incoming and outgoing rental requests,
leave reviews, and view public user profiles.

The app uses the live coursework API by default:

```text
https://set09102-api.b-davison.workers.dev/
```

It also keeps local PostgreSQL/PostGIS repository implementations for tests and
offline development.

## Features

- API registration and login with JWT storage in MAUI SecureStorage.
- Browse, search, filter, view, create, and update rental item listings.
- Owner name and owner average rating shown on items.
- Public user profile page using `GET /users/{id}/profile`.
- Reviews shown for items and users.
- Incoming rental request management for item owners.
- Outgoing rental tracking for borrowers.
- Rental status workflow: `Requested`, `Approved`, `Rejected`, `Out for Rent`,
  `Overdue`, `Returned`, and `Completed`.
- Nearby item search using GPS in API mode and PostGIS in local mode.
- GitHub Actions build, test, and Codecov upload workflow.

## Architecture

The solution is split into three main projects:

```text
RentalApp              MAUI app, pages, view models, services, navigation
RentalApp.Database     domain models, repositories, services, EF Core, migrations
RentalApp.Tests        xUnit tests for repositories, services, API clients, states
```

Main patterns used:

- **MVVM**: XAML pages bind to ViewModels and commands.
- **Repository Pattern**: ViewModels and services depend on repository
  interfaces, with API and local implementations.
- **Service Layer**: rental, review, auth, location, and navigation behaviour
  is kept out of XAML pages.
- **State Pattern**: rental status transitions are represented by state classes.

Runtime mode is controlled in `RentalApp/MauiProgram.cs`:

```csharp
public static bool UseApi { get; set; } = true;
```

When `UseApi` is `true`, the app uses the shared coursework API. When `false`,
it uses the local EF Core/PostGIS repositories.

## Prerequisites

- .NET SDK 9.0
- Docker Desktop
- Android emulator or physical Android device for the MAUI app
- Optional: PostgreSQL client tools such as `psql`

## Database Setup

Start the PostGIS database container from the repository root:

```powershell
docker compose up -d db
docker compose ps
```

Check that PostGIS is available:

```powershell
docker compose exec db psql -U app_user -d appdb -c "SELECT postgis_full_version();"
```

The local database is exposed on port `5433`:

```text
Host=localhost;Port=5433;Username=app_user;Password=app_password;Database=appdb
```

Run migrations if you are using local mode:

```powershell
docker compose run --rm migrate
```

## Build

Build the shared database/test code:

```powershell
dotnet build RentalApp.Tests/RentalApp.Tests.csproj --configuration Debug
```

Build the Android app:

```powershell
dotnet build RentalApp/RentalApp.csproj --configuration Debug --framework net9.0-android
```

## Tests

Run the test suite:

```powershell
dotnet test RentalApp.Tests/RentalApp.Tests.csproj
```

The tests use xUnit. Integration-style local repository tests use
`DatabaseFixture`; API repository tests use mocked HTTP handlers rather than
calling the live API.

## Test Coverage

Generate Cobertura coverage:

```powershell
dotnet test RentalApp.Tests/RentalApp.Tests.csproj --collect:"XPlat Code Coverage"
```

Show the latest line coverage percentage in PowerShell:

```powershell
$coverage = [xml](Get-Content (Get-ChildItem RentalApp.Tests/TestResults -Recurse -Filter coverage.cobertura.xml | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName)
[math]::Round([double]$coverage.coverage.'line-rate' * 100, 2)
```

Generate an HTML report:

```powershell
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator "-reports:RentalApp.Tests/TestResults/**/coverage.cobertura.xml" "-targetdir:coverage-report" "-reporttypes:Html"
```

Open:

```text
coverage-report/index.html
```

## Running On Android

Build the Android target, then deploy from Visual Studio or use the generated
APK from:

```text
RentalApp/bin/Debug/net9.0-android/android-x64/
```

For local database mode from an Android emulator, use `10.0.2.2` instead of
`localhost` when connecting back to the host machine.

## Coursework Guidance Folder

The `coursework guidance/` folder is for local notes, PDFs, plans, and guides.
It should not be uploaded to GitHub. Keep this entry in `.gitignore`:

```text
/coursework guidance/
```

## CI/CD

GitHub Actions runs on `main` pushes and pull requests. The workflow:

- restores .NET 9 dependencies
- starts a PostGIS service container
- builds the test project
- runs xUnit tests
- collects Cobertura coverage
- uploads coverage to Codecov
- uploads test results as an artifact

Workflow file:

```text
.github/workflows/build.yml
```
