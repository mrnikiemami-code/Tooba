using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// نتیجهٔ ارزیابی eligibility مرجوعی (منبع حقیقت یکتا؛ settlement دخیل نیست).
/// </summary>
public sealed record ReturnEligibilityResult(
    Guid SellerOrderId,
    Guid CheckoutId,
    bool Eligible,
    string ReasonCode,
    DateTimeOffset? EligibleUntil,
    DateTimeOffset? LastDeliveredAt,
    IReadOnlyList<ReturnLineEligibility> Lines);
