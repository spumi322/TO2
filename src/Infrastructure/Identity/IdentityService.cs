using Application.Contracts;
using Application.DTOs.Auth;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<User> _userManager;

        public IdentityService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        public async Task<AuthUser?> FindByIdAsync(long userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user == null ? null : ToAuthUser(user);
        }

        public async Task<AuthUser?> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
                return null;

            return ToAuthUser(user);
        }

        public async Task<AuthUser> CreateUserAsync(string userName, string email, string password, long tenantId)
        {
            var user = new User
            {
                UserName = userName,
                Email = email,
                TenantId = tenantId,
                EmailConfirmed = true // Auto-confirm for development
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"User creation failed: {errors}");
            }

            return ToAuthUser(user);
        }

        private static AuthUser ToAuthUser(User user) => new(user.Id, user.UserName!, user.Email!, user.TenantId);
    }
}
