using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RaceDayAPI.Services
{
    public interface IAuthService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);

        /// <summary>True when the stored hash is in an old format and should be replaced after a successful login.</summary>
        bool NeedsRehash(string storedHash);
    }

    /// <summary>
    /// PBKDF2-SHA256 with a random 16-byte salt per password, stored as "v1.{iterations}.{salt}.{hash}".
    /// Accounts created in Part 2 hold an unsalted SHA-256 value. Those still verify, and are upgraded
    /// to the current format on the user's next successful login.
    /// </summary>
    public class AuthService : IAuthService
    {
        private const string Version = "v1";
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            return string.Join('.',
                Version,
                Iterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            if (TryParseCurrent(storedHash, out var iterations, out var salt, out var expected))
            {
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }

            // Legacy format from Part 2: unsalted SHA-256, base64.
            var legacy = TryDecodeBase64(storedHash);
            if (legacy == null) return false;
            var computed = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return CryptographicOperations.FixedTimeEquals(computed, legacy);
        }

        public bool NeedsRehash(string storedHash) => !TryParseCurrent(storedHash, out _, out _, out _);

        private static bool TryParseCurrent(string stored, out int iterations, out byte[] salt, out byte[] key)
        {
            iterations = 0;
            salt = Array.Empty<byte>();
            key = Array.Empty<byte>();

            var parts = stored.Split('.');
            if (parts.Length != 4 || parts[0] != Version) return false;
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations < 1)
                return false;

            var decodedSalt = TryDecodeBase64(parts[2]);
            var decodedKey = TryDecodeBase64(parts[3]);
            if (decodedSalt == null || decodedKey == null || decodedSalt.Length == 0 || decodedKey.Length == 0)
                return false;

            salt = decodedSalt;
            key = decodedKey;
            return true;
        }

        private static byte[]? TryDecodeBase64(string value)
        {
            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
