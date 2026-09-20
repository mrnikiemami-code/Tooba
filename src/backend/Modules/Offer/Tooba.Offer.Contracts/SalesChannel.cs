namespace Tooba.Offer.Domain;

/// <summary>
/// کانال فروش پایدار. رشتهٔ UI آزاد نیست.
/// Owned public contract type (Tooba.Offer.Contracts assembly) for cross-module Domain/Application use.
/// </summary>
public enum SalesChannel
{
    /// <summary>فروش مستقیم فروشگاه.</summary>
    Direct = 0,

    /// <summary>کانال Marketplace چندفروشنده.</summary>
    Marketplace = 1,

    /// <summary>کانال نمایندگی.</summary>
    Agency = 2,

    /// <summary>کانال سازمانی.</summary>
    Corporate = 3,

    /// <summary>کانال همکاری در فروش.</summary>
    Affiliate = 4,

    /// <summary>کانال API یکپارچه.</summary>
    Api = 5,
}
