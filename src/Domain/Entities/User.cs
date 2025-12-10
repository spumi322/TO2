using Domain.AggregateRoots;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities
{
    public class User : IdentityUser<long>
    {
        private readonly List<RefreshToken> _refreshTokens = new();

        public User() { }

        public long TenantId { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens;
    }
}
