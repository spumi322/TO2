using Application.DTOs.Auth;

namespace Application.Contracts
{
    /// <summary>
    /// Invite codes for joining the current user's organization.
    /// </summary>
    public interface ITenantInviteService
    {
        /// <summary>
        /// New invite code for the current tenant, valid for one day; replaces the previous code.
        /// </summary>
        Task<InviteCodeResponse> CreateInviteCodeAsync();
    }
}
