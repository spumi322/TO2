using Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

/// <summary>
/// HTTP context-based implementation of ITenantService.
/// Retrieves tenant information from JWT claims in the current HTTP request.
/// </summary>
public class HttpContextTenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public long GetCurrentTenantId()
    {
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("tenantId")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || !long.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Tenant context not found. Ensure the user is authenticated and has a valid tenantId claim.");
        }

        return tenantId;
    }

    /// <inheritdoc/>
    public string GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
    }
}
