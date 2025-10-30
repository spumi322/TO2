using Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically sets TenantId on entities during save operations.
/// Uses the modern SaveChangesInterceptor pattern (EF Core 5.0+) for clean separation of concerns.
/// </summary>
public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantService _tenantService;

    public TenantSaveChangesInterceptor(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetTenantId(DbContext? context)
    {
        if (context == null) return;

        var tenantId = _tenantService.GetCurrentTenantId();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added &&
                       e.Entity.GetType().GetProperty("TenantId") != null);

        foreach (var entry in entries)
        {
            var tenantIdProperty = entry.Property("TenantId");

            // Only set if TenantId is null or 0 (not already explicitly set)
            if (tenantIdProperty.CurrentValue == null ||
                (long)tenantIdProperty.CurrentValue == 0)
            {
                tenantIdProperty.CurrentValue = tenantId;
            }
        }
    }
}
