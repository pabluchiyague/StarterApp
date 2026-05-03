using RentalApp.Database.Models;
using RentalApp.Services;

namespace RentalApp.Tests;

/// <summary>
/// Unit tests for AuthenticationService. Each test follows the AAA structure
/// (Arrange / Act / Assert) and is named MethodName_Scenario_ExpectedBehaviour
/// per the xUnit / .NET conventions covered in Tutorial 6.
///
/// All tests run against the shared DatabaseFixture which boots an isolated
/// `testappdb` database from the production migration set. The default roles
/// (Admin, OrdinaryUser, SpecialUser) are seeded by the AddAuthSchema
/// migration; OrdinaryUser is flagged IsDefault=true so RegisterAsync should
/// auto-assign it to new accounts.
/// </summary>
public abstract class AuthenticationServiceTests
{
    protected readonly DatabaseFixture _fixture;
    protected readonly AuthenticationService _service;

    protected AuthenticationServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _service = new AuthenticationService(_fixture.TestDbContext);
    }

    /// <summary>
    /// Generates a fresh email per test so the suite is order-independent
    /// and re-runnable against a non-pristine `testappdb`.
    /// </summary>
    protected static string UniqueEmail(string label) =>
        $"{label}.{Guid.NewGuid():N}@example.com";

    [Collection("Database")]
    public class RegisterAsyncTests : AuthenticationServiceTests
    {
        public RegisterAsyncTests(DatabaseFixture fixture) : base(fixture)
        {
        }

    [Fact]
    public async Task RegisterAsync_NewUser_CreatesUserAndAssignsDefaultRole()
    {
        // Arrange
        var email = UniqueEmail("register");

        // Act
        var result = await _service.RegisterAsync(
            firstName: "Alice",
            lastName: "Anderson",
            email: email,
            password: "Sup3rSecret!");

        // Assert — registration succeeded
        Assert.True(result.IsSuccess, result.Message);

        // Assert — user exists in DB with hashed password
        var savedUser = _fixture.TestDbContext.Users
            .Single(u => u.Email == email);
        Assert.Equal("Alice", savedUser.FirstName);
        Assert.NotEqual("Sup3rSecret!", savedUser.PasswordHash);
        Assert.True(savedUser.IsActive);

        // Assert — the default role (OrdinaryUser, IsDefault=true) was attached
        var assignedRole = _fixture.TestDbContext.UserRoles
            .Where(ur => ur.UserId == savedUser.Id)
            .Select(ur => ur.Role.Name)
            .Single();
        Assert.Equal(RoleConstants.OrdinaryUser, assignedRole);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange — register an email once
        var email = UniqueEmail("register.duplicate");
        var firstAttempt = await _service.RegisterAsync(
            "Dana", "Davis", email, "Sup3rSecret!");
        Assert.True(firstAttempt.IsSuccess);

        // Act — try to register the same email again
        var secondAttempt = await _service.RegisterAsync(
            "Eli", "Evans", email, "AnotherPass!");

        // Assert
        Assert.False(secondAttempt.IsSuccess);
        Assert.Equal("User with this email already exists", secondAttempt.Message);

        // And there's still only one user with that email
        var count = _fixture.TestDbContext.Users.Count(u => u.Email == email);
        Assert.Equal(1, count);
    }
    }

    [Collection("Database")]
    public class LoginAsyncTests : AuthenticationServiceTests
    {
        public LoginAsyncTests(DatabaseFixture fixture) : base(fixture)
        {
        }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange — register a user we can then sign in as
        var email = UniqueEmail("login.valid");
        var password = "Sup3rSecret!";
        await _service.RegisterAsync("Bob", "Brown", email, password);

        // Use a fresh service instance to prove login doesn't depend on
        // state cached during the registration call.
        var loginService = new AuthenticationService(_fixture.TestDbContext);

        // Act
        var result = await loginService.LoginAsync(email, password);

        // Assert
        Assert.True(result.IsSuccess, result.Message);
        Assert.True(loginService.IsAuthenticated);
        Assert.NotNull(loginService.CurrentUser);
        Assert.Equal(email, loginService.CurrentUser!.Email);
        Assert.Contains(RoleConstants.OrdinaryUser, loginService.CurrentUserRoles);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFailure()
    {
        // Arrange — register a user
        var email = UniqueEmail("login.wrongpass");
        await _service.RegisterAsync("Carla", "Cooper", email, "CorrectPassword1!");

        var loginService = new AuthenticationService(_fixture.TestDbContext);

        // Act
        var result = await loginService.LoginAsync(email, "WrongPassword!");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password", result.Message);
        Assert.False(loginService.IsAuthenticated);
        Assert.Null(loginService.CurrentUser);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsFailure()
    {
        // Arrange — no user registered with this email
        var email = UniqueEmail("login.unknown");

        // Act
        var result = await _service.LoginAsync(email, "AnyPassword1!");

        // Assert
        Assert.False(result.IsSuccess);
        // The service deliberately returns the same generic message for both
        // unknown-email and wrong-password to avoid leaking which one is wrong.
        Assert.Equal("Invalid email or password", result.Message);
        Assert.False(_service.IsAuthenticated);
    }
    }
}
