namespace Application.Contracts;

/// <summary>
/// Service for accessing the current tenant context.
/// Abstracts tenant identification from HTTP context, enabling use in background jobs and testing.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets the current tenant ID from the execution context.
    /// </summary>
    /// <returns>The tenant ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when tenant context is not available</exception>
    long GetCurrentTenantId();

    /// <summary>
    /// Gets the current user's username from the execution context.
    /// </summary>
    /// <returns>The username, or "System" if no user context is available</returns>
    string GetCurrentUserName();
}
