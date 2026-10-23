#nullable enable
using System;
using System.Globalization;
using System.Security.Cryptography;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>管理端文件账号的 PBKDF2-SHA256 密码编码与验证。</summary>
    internal static class GatewayAdminPasswordHasher
    {
        private const string Prefix = "$pbkdf2-sha256$";
        private const int Iterations = 120_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static bool IsHashed(string? value)
            => value?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        public static string Hash(string password)
        {
            ArgumentNullException.ThrowIfNull(password);
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);
            return string.Concat(
                Prefix,
                Iterations.ToString(CultureInfo.InvariantCulture), "$",
                Convert.ToBase64String(salt), "$",
                Convert.ToBase64String(hash));
        }

        public static bool Verify(string encodedOrPlaintext, string suppliedPassword)
        {
            encodedOrPlaintext ??= string.Empty;
            suppliedPassword ??= string.Empty;
            if (!IsHashed(encodedOrPlaintext))
            {
                return FixedTimeEquals(encodedOrPlaintext, suppliedPassword);
            }

            var parts = encodedOrPlaintext.Split('$', StringSplitOptions.None);
            if (parts.Length != 5
                || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
                || iterations <= 0
                || iterations > 1_000_000)
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(parts[3]);
                var expected = Convert.FromBase64String(parts[4]);
                if (salt.Length is < 8 or > 64 || expected.Length is < 16 or > 64)
                {
                    return false;
                }

                var actual = Rfc2898DeriveBytes.Pbkdf2(
                    suppliedPassword,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expected.Length);
                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = System.Text.Encoding.UTF8.GetBytes(left);
            var rightBytes = System.Text.Encoding.UTF8.GetBytes(right);
            if (leftBytes.Length != rightBytes.Length)
            {
                CryptographicOperations.FixedTimeEquals(leftBytes, leftBytes);
                return false;
            }
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
