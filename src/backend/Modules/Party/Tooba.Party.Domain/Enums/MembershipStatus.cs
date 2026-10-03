namespace Tooba.Party.Domain.Enums;

/// <summary>
/// وضعیت عضویت. وجود عضویت به‌تنهایی همهٔ مجوزها را نمی‌دهد.
/// </summary>
public enum MembershipStatus
{
    /// <summary>
    /// پیوند کسب‌وکار برقرار است و می‌تواند به SpiceDB تصویر شود.
    /// </summary>
    Active = 0,

    /// <summary>
    /// پیوند پایان یافته؛ مجوز را SpiceDB schema تعیین می‌کند نه این مقدار به‌تنهایی.
    /// </summary>
    Ended = 1,
}
