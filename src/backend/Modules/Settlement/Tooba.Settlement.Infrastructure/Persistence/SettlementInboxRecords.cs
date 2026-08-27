namespace Tooba.Settlement.Infrastructure.Persistence;

/// <summary>
/// dedup inbox برای payment.succeeded در schema settlement.
/// </summary>
public sealed class SettlementPaymentInboxRecord
{
    /// <summary>شناسه رویداد integration.</summary>
    public Guid EventId { get; init; }

    /// <summary>پرداخت مرجع.</summary>
    public Guid PaymentId { get; init; }

    /// <summary>زمان پردازش.</summary>
    public DateTimeOffset ProcessedAt { get; init; }
}

/// <summary>
/// dedup inbox برای refund.succeeded در schema settlement.
/// </summary>
public sealed class SettlementRefundInboxRecord
{
    /// <summary>شناسه رویداد integration.</summary>
    public Guid EventId { get; init; }

    /// <summary>درخواست مرجوعی مرجع.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>زمان پردازش.</summary>
    public DateTimeOffset ProcessedAt { get; init; }
}
