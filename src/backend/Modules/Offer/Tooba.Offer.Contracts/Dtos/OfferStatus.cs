namespace Tooba.Offer.Domain;

/// <summary>
/// وضعیت تجاری Offer. موجودی، اعتبار قیمت، و انتشار Catalog را نشان نمی‌دهد.
/// Owned public contract type (Tooba.Offer.Contracts assembly).
/// </summary>
public enum OfferStatus
{
    /// <summary>پیش‌نویس listing.</summary>
    Draft = 0,

    /// <summary>Offer برای کانال فعال است.</summary>
    Active = 1,

    /// <summary>تعلیق تجاری فروشنده/کانال.</summary>
    Suspended = 2,

    /// <summary>بایگانی listing.</summary>
    Archived = 3,
}
