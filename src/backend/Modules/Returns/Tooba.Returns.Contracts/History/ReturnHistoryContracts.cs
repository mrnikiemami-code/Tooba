namespace Tooba.Returns.Contracts.History;

/// <summary>Returned quantity of one order line inside a return request.</summary>
public sealed record ReturnHistoryItem(Guid OrderLineId, decimal Quantity);

/// <summary>One refund attempt of a return request. Statuses are stable strings, not owner enums.</summary>
public sealed record ReturnHistoryRefundAttempt(
    string Status,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>Operational milestones of one return request and its refund attempts.</summary>
public sealed record ReturnHistoryRecord(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid RequestedByUserId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ReturnHistoryItem> Items,
    IReadOnlyList<ReturnHistoryRefundAttempt> RefundAttempts);

/// <summary>Read-only Returns history for seller orders, consumed by admin operational timelines.</summary>
public interface IReturnHistoryReader
{
    /// <summary>Return requests for the given seller orders, newest first.</summary>
    Task<IReadOnlyList<ReturnHistoryRecord>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
}

/// <summary>Stable return status strings exposed through the history contract.</summary>
public static class ReturnHistoryStatuses
{
    /// <summary>Return request was approved.</summary>
    public const string Approved = "Approved";

    /// <summary>Return request refund is processing.</summary>
    public const string RefundProcessing = "RefundProcessing";

    /// <summary>Return request completed.</summary>
    public const string Completed = "Completed";

    /// <summary>Return request refund failed.</summary>
    public const string RefundFailed = "RefundFailed";

    /// <summary>Return request was rejected.</summary>
    public const string Rejected = "Rejected";
}

/// <summary>Stable refund attempt status strings exposed through the history contract.</summary>
public static class ReturnHistoryRefundAttemptStatuses
{
    /// <summary>Refund attempt is pending.</summary>
    public const string Pending = "Pending";

    /// <summary>Refund attempt succeeded.</summary>
    public const string Succeeded = "Succeeded";

    /// <summary>Refund attempt failed.</summary>
    public const string Failed = "Failed";
}
