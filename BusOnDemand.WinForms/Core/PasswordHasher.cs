using System;
using System.Security.Cryptography;

namespace BusOnDemand.WinForms.Core
{
    public static class PasswordHasher
    {
        // Format: PBKDF2$iterations$base64Salt$base64Hash (HMAC-SHA1, compatible with Rfc2898DeriveBytes on .NET Framework 4.7.2).
        public static bool Verify(string password, string stored)
        {
            if (String.IsNullOrEmpty(password) || String.IsNullOrEmpty(stored)) return false;
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2") return false;
            int iterations;
            if (!Int32.TryParse(parts[1], out iterations) || iterations < 10000) return false;
            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
                {
                    byte[] actual = derive.GetBytes(expected.Length);
                    if (actual.Length != expected.Length) return false;
                    int difference = 0;
                    for (int i = 0; i < actual.Length; i++) difference |= actual[i] ^ expected[i];
                    return difference == 0;
                }
            }
            catch { return false; }
        }

        public static string Create(string password, int iterations = 120000)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return "PBKDF2$" + iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(derive.GetBytes(32));
            }
        }
    }
}
