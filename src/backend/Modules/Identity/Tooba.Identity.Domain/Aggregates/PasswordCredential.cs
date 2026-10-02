namespace Tooba.Identity.Domain.Aggregates;

/// <summary>
/// فرادادهٔ هش رمز. plaintext و خود هش نباید لاگ شوند.
/// </summary>
public sealed class PasswordCredential
{
    /// <summary>
    /// کلید User.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// خروجی hasher استاندارد ASP.NET؛ الگوریتم سفارشی نیست.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// نسخهٔ قالب هش برای ارتقای بعدی.
    /// </summary>
    public int HasherFormatVersion { get; set; }

    /// <summary>
    /// زمان آخرین تغییر اعتبار.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
