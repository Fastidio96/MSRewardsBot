using System;
using System.Security.Cryptography;
using System.Text;

namespace MSRewardsBot.Server.Helpers
{
    internal class AuthUtils
    {
        private const string PEPPER = "ezCaCGyBwygMVgtZ44ppztHHbBSBpLjHw";
        private const int ITERACTION = 100000;

        /// <summary>
        /// Used for register
        /// </summary>
        internal static string HashPassword(string password)
        {
            int saltSize = 16; // 128 bit
            int keySize = 32;  // 256 bit

            // Generate random salt
            byte[] salt = RandomNumberGenerator.GetBytes(saltSize);

            byte[] passwordBytes;
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(PEPPER)))
            {
                passwordBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }

            // Derive key (hash)
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                ITERACTION,
                HashAlgorithmName.SHA256,
                keySize
            );

            // Combine: iterations.salt.hash (Base64)
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Used for login
        /// </summary>
        internal static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string[] parts = storedHash.Split('.', 3);
            if (parts.Length < 2)
            {
                return false;
            }

            byte[] salt;
            byte[] stored;
            try
            {
                salt = Convert.FromBase64String(parts[0]);
                stored = Convert.FromBase64String(parts[1]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (salt.Length == 0 || stored.Length == 0)
            {
                return false;
            }

            byte[] passwordBytes;
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(PEPPER)))
            {
                passwordBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }

            byte[] computed = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                ITERACTION,
                HashAlgorithmName.SHA256,
                stored.Length
            );

            return CryptographicOperations.FixedTimeEquals(stored, computed);
        }
    }
}
