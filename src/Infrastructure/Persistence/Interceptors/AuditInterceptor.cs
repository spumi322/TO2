using Application.Contracts;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically sets audit fields (CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate)
/// on entities inheriting from EntityBase.
/// Replaces the previous "PlaceHolder" approach with real user data from ITenantService.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantService _tenantService;

    public AuditInterceptor(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context == null) return;

        var userName = _tenantService.GetCurrentUserName();
        var now = DateTime.UtcNow;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is EntityBase &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (EntityBase)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedDate = now;
                entity.CreatedBy = userName;
            }

            entity.LastModifiedDate = now;
            entity.LastModifiedBy = userName;
        }
    }
}
