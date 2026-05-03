namespace RentalApp.Database.Repositories;

/// <summary>
/// This marks repository interfaces that abstract persistence for one domain
/// model type. Specific repositories add methods that match each aggregate's
/// real queries and API endpoints.
/// </summary>
public interface IRepository<T>
    where T : class
{
}
