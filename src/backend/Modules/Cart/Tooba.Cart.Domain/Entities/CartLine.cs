using Tooba.Offer.Contracts.Dtos;
using Tooba.BuildingBlocks;

namespace Tooba.Cart.Domain.Entities;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CartLine
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private CartLine()
    {
    }

    /// <summary>
    /// شناسهٔ پایدار خط.
    /// </summary>
    public Guid LineId { get; init; }

    /// <summary>
    /// سبد مالک خط.
    /// </summary>
    public Guid CartId { get; init; }

    /// <summary>
    /// Offer فروشنده؛ موجودی و قیمت جداگانه از قرارداد خوانده می‌شوند.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// گونهٔ Catalog کپی‌شده از Lookup؛ FK به schema کاتالوگ نیست.
    /// </summary>
    public Guid CatalogVariantId { get; init; }

    /// <summary>
    /// فروشندهٔ Offer؛ چندفروشنده در یک سبد مجاز است.
    /// </summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>
    /// تعداد صحیح مثبت. اعشار نیست.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// رزرو موجودی متعلق به این خط؛ جداول Inventory اینجا join نمی‌شوند.
    /// </summary>
    public Guid? ReservationId { get; private set; }

    /// <summary>
    /// مبلغ نقل‌قول نمایشی. حقیقت تسویه یا مالیات محاسبه‌شده نیست.
    /// </summary>
    public decimal? QuotedAmount { get; private set; }

    /// <summary>
    /// ارز نقل‌قول؛ Locale نیست.
    /// </summary>
    public string? QuotedCurrency { get; private set; }

    /// <summary>
    /// آیا مبلغ نقل‌قول بدون مالیات نوشته شده است.
    /// </summary>
    public bool QuotedTaxExclusive { get; private set; }

    /// <summary>
    /// شناسهٔ قیمت انتخاب‌شده در زمان نقل‌قول.
    /// </summary>
    public Guid? PriceId { get; private set; }

    /// <summary>
    /// زمینهٔ کمپین مرچندایزینگ (اختیاری)؛ مرجع واجدشرایطی است نه مبلغ کلاینت.
    /// </summary>
    public Guid? MerchandisingCampaignId { get; private set; }

    /// <summary>
    /// زمان UTC گرفتن نقل‌قول نمایشی.
    /// </summary>
    public DateTimeOffset QuotedAt { get; private set; }

    /// <summary>
    /// خط جدید با تعداد و نقل‌قول می‌سازد.
    /// </summary>
    public static CartLine Open(
        Guid lineId,
        Guid cartId,
        Guid offerId,
        Guid catalogVariantId,
        Guid sellerPartyId,
        decimal quantity,
        Guid? reservationId,
        decimal quotedAmount,
        string quotedCurrency,
        bool taxExclusive,
        Guid priceId,
        DateTimeOffset quotedAt,
        Guid? merchandisingCampaignId = null)
    {
        EnsureQuantity(quantity);
        return new CartLine
        {
            LineId = lineId,
            CartId = cartId,
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            SellerPartyId = sellerPartyId,
            Quantity = quantity,
            ReservationId = reservationId,
            QuotedAmount = quotedAmount,
            QuotedCurrency = quotedCurrency,
            QuotedTaxExclusive = taxExclusive,
            PriceId = priceId,
            MerchandisingCampaignId = merchandisingCampaignId,
            QuotedAt = quotedAt,
        };
    }

    /// <summary>
    /// تعداد و رزرو و نقل‌قول را پس از هم‌ترازسازی موجودی عوض می‌کند.
    /// </summary>
    public void ReplaceHold(
        decimal quantity,
        Guid? reservationId,
        decimal quotedAmount,
        string quotedCurrency,
        bool taxExclusive,
        Guid priceId,
        DateTimeOffset quotedAt,
        Guid? merchandisingCampaignId = null)
    {
        EnsureQuantity(quantity);
        Quantity = quantity;
        ReservationId = reservationId;
        QuotedAmount = quotedAmount;
        QuotedCurrency = quotedCurrency;
        QuotedTaxExclusive = taxExclusive;
        PriceId = priceId;
        MerchandisingCampaignId = merchandisingCampaignId;
        QuotedAt = quotedAt;
    }

    /// <summary>
    /// زمینهٔ کمپین را پس از ادغام/بازنویسی خط تنظیم می‌کند.
    /// </summary>
    public void SetMerchandisingCampaignId(Guid? merchandisingCampaignId) =>
        MerchandisingCampaignId = merchandisingCampaignId;

    /// <summary>
    /// رزرو را پس از آزادسازی از خط جدا می‌کند.
    /// </summary>
    public void ClearReservation() => ReservationId = null;

    /// <summary>
    /// تعداد باید مثبت و حداکثر ۹۹ باشد (اعشار طبق سیاست مقدار مجاز است).
    /// </summary>
    public static void EnsureQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد خط سبد باید مثبت باشد.");
        }

        if (quantity > 99)
        {
            throw new InvalidOperationException("تعداد خط سبد از سقف foundation بیشتر است.");
        }
    }
}
