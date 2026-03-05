namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}