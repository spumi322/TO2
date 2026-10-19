using System.Security.Cryptography;
using System.Text;

namespace Application.Utilities
{
    /// <summary>
    /// Hashes random secrets that are stored hashed (refresh tokens, invite codes).
    /// Plain SHA-256 is enough because the secrets are long and random; guessable secrets like passwords need a slow, salted hash (Identity does that).
    /// No salt, so the same value always gives the same hash and can be looked up by it.
    /// </summary>
    public static class SecretHasher
    {
        /// <summary>
        /// SHA-256 of the UTF-8 bytes, uppercase hex (64 chars).
        /// </summary>
        public static string Sha256Hex(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        }
    }
}
