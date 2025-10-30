using Domain.AggregateRoots;
using Domain.Common;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class User : IdentityUser<long>
    {
        private readonly List<RefreshToken> _refreshTokens = new();

        private User() { }

        [Required]
        public string Name { get; set; }

        public long TenantId { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens; 
    }
}
