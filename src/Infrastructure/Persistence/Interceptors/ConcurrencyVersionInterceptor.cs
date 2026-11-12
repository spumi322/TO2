using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that increments Version on every modified entity inheriting from EntityBase.
/// Version is a concurrency token, so EF adds "WHERE Version = @loaded" to UPDATE/DELETE and throws
/// DbUpdateConcurrencyException when another write got there first (UnitOfWork turns it into ConflictException → 409).
/// App-managed, so it works on any EF provider.
/// </summary>
public class ConcurrencyVersionInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        IncrementVersions(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        IncrementVersions(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void IncrementVersions(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<EntityBase>().Where(e => e.State == EntityState.Modified))
        {
            var version = entry.Property(e => e.Version);
            version.CurrentValue = version.OriginalValue + 1;
        }
    }
}
