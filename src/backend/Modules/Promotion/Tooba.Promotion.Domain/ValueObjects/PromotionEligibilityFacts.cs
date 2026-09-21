

namespace Tooba.Promotion.Domain.ValueObjects;

/// <summary>
/// واقعیت‌های صلاحیت که از قرارداد/تصویر Checkout می‌آید نه از JOIN به schemaهای بیگانه.
/// </summary>
public sealed record PromotionEligibilityFacts(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid? CategoryId,
    Guid SellerPartyId,
    string Market,
    string SalesChannel,
    string Currency,
    Guid? CustomerPartyId,
    Guid? OrganizationPartyId,
    decimal Quantity,
    decimal BaseTaxExclusiveAmount,
    string? CouponCode);
