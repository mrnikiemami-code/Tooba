namespace Tooba.Order.Application.Admin.InventoryRecovery.Models;

/// <summary>خط نیازمند بازیابی رزرو.</summary>
public sealed record OrderInventoryRecoveryLineNeed(
    Guid OrderLineId,
    Guid SellerOrderId,
    Guid StockItemId,
    decimal RemainingQuantity,
    Guid PreviousReservationId,
    string PreviousStatus);

/// <summary>ارزیابی کلاس بازیابی موجودی.</summary>
public sealed record OrderInventoryRecoveryAssessment(
    Guid CheckoutId,
    string OrderNumbers,
    string ClassCode,
    bool NeedsRecovery,
    string ReasonFa,
    string OutcomeHint,
    IReadOnlyList<OrderInventoryRecoveryLineNeed> Lines);

/// <summary>نتیجهٔ بازیابی.</summary>
public sealed record OrderInventoryRecoveryResult(
    string Outcome,
    string ClassCode,
    bool NeedsRecovery,
    string MessageFa,
    string OrderNumbers);

/// <summary>سطر کاندید audit.</summary>
public sealed record OrderInventoryRecoveryAuditRow(
    Guid CheckoutId,
    string OrderNumbers,
    string ClassCode,
    string OutcomeHint,
    bool NeedsRecovery,
    string ReasonFa);

/// <summary>صفحهٔ audit بازیابی.</summary>
public sealed record OrderInventoryRecoveryAuditPage(
    IReadOnlyDictionary<string, int> CountsByClass,
    IReadOnlyList<OrderInventoryRecoveryAuditRow> Candidates);
