using System.Security.Cryptography;

namespace Application.Utilities
{
    /// <summary>
    /// Organization invite codes: 24 random chars (~120 bits) shown as XXXX-XXXX-XXXX-XXXX-XXXX-XXXX.
    /// The alphabet leaves out 0/O and 1/I so a typed code is hard to get wrong.
    /// </summary>
    public static class InviteCode
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int Length = 24;
        private const int GroupSize = 4;

        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(1);

        public static string Generate()
        {
            var code = RandomNumberGenerator.GetString(Alphabet, Length);
            return string.Join('-', code.Chunk(GroupSize).Select(group => new string(group)));
        }

        /// <summary>
        /// Uppercase without dashes and spaces, so a code typed in any form gives the same hash.
        /// </summary>
        public static string Normalize(string code)
        {
            return new string(code.Where(c => c != '-' && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        }

        public static string Hash(string code) => SecretHasher.Sha256Hex(Normalize(code));
    }
}
