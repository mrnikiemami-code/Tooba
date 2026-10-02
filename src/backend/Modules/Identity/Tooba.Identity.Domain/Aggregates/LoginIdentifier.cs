using Tooba.Identity.Contracts;
using Tooba.Identity.Domain.Enums;

namespace Tooba.Identity.Domain.Aggregates;

/// <summary>
/// شناسهٔ ورود متعلق به یک User. ستون ثابت روی خود User نیست.
/// </summary>
public sealed class LoginIdentifier
{
    /// <summary>
    /// شناسهٔ پایدار ردیف شناسه.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// کلید User مالک؛ FK بین‌ماژولی به Party نیست.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// گونهٔ typed برای lookup.
    /// </summary>
    public LoginIdentifierKind Kind { get; init; }

    /// <summary>
    /// مقدار نمایش برای کاربر.
    /// </summary>
    public string DisplayValue { get; init; } = string.Empty;

    /// <summary>
    /// کلید یکتایی در دامنهٔ هویت همان پایگاه Tenant/Marketplace.
    /// </summary>
    public string NormalizedValue { get; init; } = string.Empty;

    /// <summary>
    /// وضعیت اثبات مالکیت.
    /// </summary>
    public IdentifierVerificationState VerificationState { get; set; }

    /// <summary>
    /// نشانهٔ ترجیح UX؛ جایگزین یکتایی نیست.
    /// </summary>
    public bool IsPreferred { get; set; }

    /// <summary>
    /// زمان ایجاد به‌وقت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان تأیید در صورت Verified.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; set; }
}
