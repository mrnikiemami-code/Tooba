using Tooba.Identity.Domain.Enums;

namespace Tooba.Identity.Domain.Aggregates;

/// <summary>
/// ثبت نام‌نویسی عامل MFA بدون پیاده‌سازی UI.
/// </summary>
public sealed class MfaFactorEnrollment
{
    /// <summary>
    /// کلید ردیف نام‌نویسی.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User مالک عامل.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// گونهٔ عامل آینده.
    /// </summary>
    public MfaFactorKind FactorKind { get; init; }

    /// <summary>
    /// آیا عامل فعال است.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
