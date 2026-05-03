namespace RentalApp.Tests;

/// <summary>
/// Defines an xUnit test collection backed by <see cref="DatabaseFixture"/>.
/// Test classes that decorate themselves with <c>[Collection("Database")]</c>
/// share a single fixture instance — meaning <c>testappdb</c> is dropped and
/// recreated only once per test run, not once per class. This also serialises
/// test execution across the collection, which is required because all tests
/// hit the same physical database.
///
/// Without this, xUnit's default behaviour parallelises across test classes,
/// causing two <see cref="DatabaseFixture"/> instances to race
/// <c>EnsureDeleted</c> + <c>Migrate</c> on the same database. Symptom is a
/// <c>3D000: database "testappdb" does not exist</c> Postgres error in
/// whichever fixture loses the race.
/// </summary>
[CollectionDefinition("Database", DisableParallelization = true)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // Intentionally empty — this class is only the marker that ties test
    // classes to the shared DatabaseFixture instance.
}
