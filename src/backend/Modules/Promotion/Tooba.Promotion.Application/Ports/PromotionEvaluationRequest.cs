using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Application.Ports;

/// <summary>
/// ورودی ارزیابی. مبلغ پایه از Pricing می‌آید نه از سبد به‌عنوان حقیقت.
/// </summary>
public sealed record PromotionEvaluationRequest(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid? CategoryId,
    Guid SellerPartyId,
    string Market,
    string SalesChannel,
    string Currency,
    decimal Quantity,
    decimal BaseTaxExclusiveAmount,
    Guid? CustomerPartyId,
    Guid? OrganizationPartyId,
    string? CouponCode,
    DateTimeOffset At,
    QuantityRoundingMode RoundingMode = QuantityRoundingMode.Nearest);
