namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// Admin return/refund work-queue row — a read model over the same Return aggregate, with no second
/// lifecycle. Transport-neutral: every value is already projected by
/// <see cref="AdminReturnQueueFilters"/> or resolved from a foreign Contracts lookup.
/// </summary>
public sealed record AdminReturnWorkQueueRow(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    string ReturnReference,
    string OrderReference,
    string CustomerDisplayName,
    string SellerDisplayName,
    string ProductLabel,
    decimal QuantityRequested,
    string UnitLabel,
    string ReturnStatus,
    string RefundStatus,
    string EligibilitySummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> AvailableActionCodes);
