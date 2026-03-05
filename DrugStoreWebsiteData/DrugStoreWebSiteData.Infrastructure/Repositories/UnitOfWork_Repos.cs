using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DrugStoreWebSite.Infrastructure.Repositories;

public class UnitOfWork_Repos : IUnitOfWork
{
    private readonly DrugStoreDbContext _context;
    private bool _disposed = false; // To follow the dispose pattern
    private readonly ILogger<UnitOfWork_Repos> _logger;

    public UnitOfWork_Repos(
        DrugStoreDbContext context,
        ILogger<UnitOfWork_Repos> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result =  await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Changes saved successfully to the database.");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while saving changes to the database.");
            throw; // Re-throw the exception after logging it
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);  // Prevent finalizer from running
    }
}