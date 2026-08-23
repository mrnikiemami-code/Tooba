using System.Security.Cryptography;

namespace Tooba.Cart.Infrastructure;

/// <summary>
/// تولید و هش راز مهمان. راز خام در پایگاه ذخیره نمی‌شود.
/// </summary>
public static class CartCredentialHasher
{
    /// <summary>
    /// راز پرمخاطرهٔ ۳۲ بایتی به‌صورت hex می‌سازد.
    /// </summary>
    public static string CreateSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    /// <summary>
    /// هش SHA-256 پایدار برای ذخیره می‌سازد.
    /// </summary>
    public static string Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret.Trim()))).ToLowerInvariant();
    }

    /// <summary>
    /// مقایسهٔ زمان‌ثابت هش با راز ارائه‌شده.
    /// </summary>
    public static bool Matches(string secret, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var computed = Hash(secret);
        var left = System.Text.Encoding.UTF8.GetBytes(computed);
        var right = System.Text.Encoding.UTF8.GetBytes(storedHash.Trim().ToLowerInvariant());
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
