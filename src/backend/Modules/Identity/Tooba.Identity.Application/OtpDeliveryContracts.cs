using Tooba.Identity.Domain;

namespace Tooba.Identity.Application;

/// <summary>
/// نتیجهٔ تحویل OTP بدون افشای راز یا جزئیات vendor.
/// </summary>
public enum OtpDeliveryOutcomeKind
{
    /// <summary>تحویل موفق.</summary>
    Succeeded = 1,

    /// <summary>provider موقتاً در دسترس نیست.</summary>
    Unavailable = 2,

    /// <summary>provider نرخ را محدود کرده است.</summary>
    RateLimited = 3,

    /// <summary>مقصد نامعتبر.</summary>
    InvalidDestination = 4,

    /// <summary>پیکربندی Production ناقص است.</summary>
    Misconfigured = 5,
}

/// <summary>
/// پیام تحویل OTP provider-agnostic.
/// </summary>
public sealed record OtpDeliveryMessage(OtpPurpose Purpose, string Destination, string OneTimeCode);

/// <summary>
/// نتیجهٔ provider با correlation اختیاری.
/// </summary>
public sealed record OtpDeliveryOutcome(OtpDeliveryOutcomeKind Kind, string? CorrelationId = null);

/// <summary>
/// مرز تحویل OTP تولید؛ domain به vendor خاص گره نمی‌خورد.
/// </summary>
public interface IOtpDeliveryProvider
{
    /// <summary>
    /// OTP را به مقصد می‌فرستد. کد OTP نباید لاگ شود.
    /// </summary>
    Task<OtpDeliveryOutcome> DeliverAsync(OtpDeliveryMessage message, CancellationToken cancellationToken);
}
