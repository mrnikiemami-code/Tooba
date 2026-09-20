using Tooba.BuildingBlocks;

namespace Tooba.Pricing.Domain;

/// <summary>
/// وضعیت رکورد قیمت نوشته‌شده. موجودی، انتشار Catalog، و نرخ FX را نشان نمی‌دهد.
/// </summary>
public enum PriceStatus
{
    /// <summary>
    /// پیش‌نویس قیمت نوشته‌شده. هنوز برای انتخاب پایه فعال نیست.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// قیمت پایهٔ فعال در بازهٔ اعتبار. به‌تنهایی قابل‌خرید بودن Offer نیست.
    /// </summary>
    Active = 1,

    /// <summary>
    /// قیمت از انتخاب خارج شده است. حذف Product یا Offer نیست.
    /// </summary>
    Retired = 2,
}

/// <summary>
/// گونهٔ محدودکنندهٔ قیمت. پایه برای فروش عادی؛ کمپین مرچندایزینگ برای قیمت تبلیغاتی همان Offer.
/// </summary>
public enum PriceQualifierKind
{
    /// <summary>
    /// قیمت پایهٔ کانال/بازار بدون مشتری یا قرارداد.
    /// </summary>
    Base = 0,

    /// <summary>
    /// قیمت نوشته‌شدهٔ محدود به یک MerchandisingCampaign مشخص (QualifierKey = CampaignId).
    /// </summary>
    MerchandisingCampaign = 1,
}
