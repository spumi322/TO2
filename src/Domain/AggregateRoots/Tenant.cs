using Domain.Common;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Domain.AggregateRoots
{
    public class Tenant : AggregateRootBase
    {
        private readonly List<User> _users = new();
        private string _name = string.Empty;

        public Tenant() { }

        /// <summary>
        /// Display name (trimmed). Setting it also sets <see cref="NormalizedName"/>.
        /// </summary>
        [Required]
        public string Name
        {
            get => _name;
            set
            {
                _name = value.Trim();
                NormalizedName = NormalizeName(value);
            }
        }

        /// <summary>
        /// Trimmed, uppercase name; unique, so names differing only in case or spaces are the same organization.
        /// </summary>
        public string NormalizedName { get; private set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Hash of the current invite code; a tenant has at most one code, a new one replaces it.
        /// </summary>
        public string? InviteCodeHash { get; private set; }
        public DateTime? InviteCodeExpiresAt { get; private set; }

        public IReadOnlyList<User> Users => _users;

        public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

        public void SetInviteCode(string codeHash, DateTime expiresAt)
        {
            InviteCodeHash = codeHash;
            InviteCodeExpiresAt = expiresAt;
        }

        public bool IsInviteCodeValid(string codeHash, DateTime now)
        {
            return InviteCodeHash != null && InviteCodeHash == codeHash && now < InviteCodeExpiresAt;
        }
    }
}
