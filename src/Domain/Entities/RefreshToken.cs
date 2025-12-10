using Domain.Common;

namespace Domain.Entities
{
    public class RefreshToken : EntityBase
    {
        public RefreshToken() { }

        public required string Token { get; set; }
        public DateTime Expires { get; set; }
        public DateTime? Revoked { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
        public bool IsActive => !IsRevoked && !IsExpired;

        public long UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
