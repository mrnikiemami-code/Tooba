namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// Admin return/refund work-queue row — a read model over the same Return aggregate, with no second
/// lifecycle. Transport-neutral: every value is already projected by
/// <see cref="AdminReturnQueueFilters"/> or resolved from a foreign Contracts lookup.
/// <para>
/// TB-275: the row also carries the case's ONE work stage (<see cref="WorkCategory"/>), its typed blocker, the exact
/// stored refund attempt the workspace may act on, and one entry per requested line, so the queue never renders a
/// summed quantity or an invented action.
/// </para>
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
    IReadOnlyList<string> AvailableActionCodes,
    IReadOnlyList<AdminReturnWorkQueueLine>? Lines = null,
    string WorkCategory = ReturnWorkCategoryCodes.Closed,
    string? WorkBlockerCode = null,
    bool NeedsOperator = false,
    Guid? PendingRefundAttemptId = null,
    Guid? FailedRefundAttemptId = null);
