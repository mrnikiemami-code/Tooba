namespace Tooba.Identity.Domain.Enums;

/// <summary>
/// وضعیت اثبات مالکیت شناسه. وجود شناسه با تأیید مالکیت یکی نیست.
/// </summary>
public enum IdentifierVerificationState
{
    /// <summary>
    /// هنوز اثبات کنترل انجام نشده است.
    /// </summary>
    Unverified = 0,

    /// <summary>
    /// مالکیت شناسه اثبات شده است.
    /// </summary>
    Verified = 1,

    /// <summary>
    /// تأیید لغو شده و نباید برای ورود سیاست‌محور استفاده شود.
    /// </summary>
    Revoked = 2,
}
