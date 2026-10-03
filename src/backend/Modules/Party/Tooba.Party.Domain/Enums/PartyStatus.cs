namespace Tooba.Party.Domain.Enums;

/// <summary>
/// وضعیت چرخهٔ عمر Party در منبع حقیقت کسب‌وکار. مجوز SpiceDB نیست.
/// </summary>
public enum PartyStatus
{
    /// <summary>
    /// Party برای پیوند و عضویت قابل استفاده است.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Party از جریان کسب‌وکار کنار گذاشته شده؛ حذف سخت هویت ورود نیست.
    /// </summary>
    Disabled = 1,
}
