using Application.DTOs.Auth;

namespace Application.Contracts
{
    /// <summary>
    /// User accounts and passwords. Implemented in Infrastructure with ASP.NET Identity.
    /// </summary>
    public interface IIdentityService
    {
        Task<bool> EmailExistsAsync(string email);

        Task<AuthUser?> FindByIdAsync(long userId);

        /// <summary>
        /// Returns the user when the email exists and the password matches, otherwise null.
        /// </summary>
        Task<AuthUser?> ValidateCredentialsAsync(string email, string password);

        /// <summary>
        /// Throws ValidationException when Identity rejects the user (password rules, duplicate name).
        /// </summary>
        Task<AuthUser> CreateUserAsync(string userName, string email, string password, long tenantId);
    }
}
