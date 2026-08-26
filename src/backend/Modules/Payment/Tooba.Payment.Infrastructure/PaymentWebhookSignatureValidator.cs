using System.Security.Cryptography;
using System.Text;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// اعتبارسنجی HMAC-SHA256 برای webhook/callback پرداخت.
/// </summary>
public static class PaymentWebhookSignatureValidator
{
    /// <summary>
    /// هدر امضای webhook.
    /// </summary>
    public const string SignatureHeaderName = "X-Tooba-Payment-Signature";

    /// <summary>
    /// پیشوند مورد انتظار: sha256=
    /// </summary>
    public const string SignaturePrefix = "sha256=";

    /// <summary>
    /// امضای بدنهٔ خام را با راز پیکربندی‌شده مقایسه می‌کند.
    /// </summary>
    public static bool TryValidate(string secret, ReadOnlySpan<byte> body, string? signatureHeader, out string errorCode)
    {
        errorCode = "payment.webhook.invalid_signature";
        if (string.IsNullOrWhiteSpace(secret))
        {
            errorCode = "payment.gateway.unconfigured";
            return false;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader)
            || !signatureHeader.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var providedHex = signatureHeader[SignaturePrefix.Length..];
        if (providedHex.Length == 0)
        {
            return false;
        }

        var provided = new byte[providedHex.Length / 2];
        for (var i = 0; i < provided.Length; i++)
        {
            if (!byte.TryParse(providedHex.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out provided[i]))
            {
                return false;
            }
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        if (provided.Length != expected.Length
            || !CryptographicOperations.FixedTimeEquals(provided, expected))
        {
            return false;
        }

        errorCode = string.Empty;
        return true;
    }

    /// <summary>
    /// امضای تست/ارسال webhook می‌سازد.
    /// </summary>
    public static string ComputeSignature(string secret, ReadOnlySpan<byte> body)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return SignaturePrefix + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
