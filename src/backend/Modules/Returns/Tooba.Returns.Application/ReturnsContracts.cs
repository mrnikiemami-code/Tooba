using Tooba.BuildingBlocks;
using Tooba.Returns.Domain;

namespace Tooba.Returns.Application;

/// <summary>
/// درز موجودی برای restock؛ Returns مستقیم Inventory DbContext باز نمی‌کند.
/// </summary>
public interface IReturnInventoryGateway
{
    /// <summary>
    /// رزرو مصرف‌شده را پس از refund موفق restock می‌کند. پیاده‌سازی فعلی می‌تواند no-op log باشد.
    /// </summary>
    Task RestockConsumedReservationAsync(Guid reservationId, int quantity, CancellationToken cancellationToken);
}

/// <summary>
/// خط مرجوعی در فرمان.
/// </summary>
public sealed record ReturnLineCommand(Guid OrderLineId, int Quantity);

/// <summary>
/// فرمان ایجاد درخواست مرجوعی.
/// </summary>
public sealed record CreateReturnCommand(
    Guid SellerOrderId,
    Guid ActorUserId,
    string IdempotencyKey,
    string? Reason,
    IReadOnlyList<ReturnLineCommand> Items);

/// <summary>
/// فرمان تأیید مرجوعی.
/// </summary>
public sealed record ApproveReturnCommand(Guid ReturnRequestId, Guid ActorUserId);

/// <summary>
/// فرمان رد مرجوعی.
/// </summary>
public sealed record RejectReturnCommand(Guid ReturnRequestId, Guid ActorUserId, string? Reason);

/// <summary>
/// فرمان retry refund (admin).
/// </summary>
public sealed record RetryRefundCommand(Guid ReturnRequestId, Guid ActorUserId);

/// <summary>
/// snapshot خواندنی درخواست مرجوعی.
/// </summary>
public sealed record ReturnSnapshot(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    Guid RequestedByUserId,
    ReturnRequestStatus Status,
    string? Reason,
    string Currency,
    decimal RefundAmount,
    Guid? PaymentId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ReturnItemSnapshot> Items,
    IReadOnlyList<RefundAttemptSnapshot> RefundAttempts);

/// <summary>
/// snapshot خط مرجوعی.
/// </summary>
public sealed record ReturnItemSnapshot(
    Guid ReturnItemId,
    Guid OrderLineId,
    int Quantity,
    decimal UnitPriceSnapshot,
    string Currency,
    Guid? ReservationId);

/// <summary>
/// snapshot تلاش refund.
/// </summary>
public sealed record RefundAttemptSnapshot(
    Guid RefundAttemptId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    RefundAttemptStatus Status,
    string IdempotencyKey,
    string? ProviderReference,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>
/// ارکستراسیون مرجوعی.
/// </summary>
public interface IReturnDirectory
{
    /// <summary>درخواست مرجوعی می‌سازد.</summary>
    Task<ReturnSnapshot> CreateAsync(CreateReturnCommand command, CancellationToken cancellationToken);

    /// <summary>درخواست را می‌خواند.</summary>
    Task<ReturnSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken);

    /// <summary>فهرست درخواست‌های یک مشتری.</summary>
    Task<IReadOnlyList<ReturnSnapshot>> ListForCustomerAsync(Guid customerUserId, CancellationToken cancellationToken);

    /// <summary>فهرست درخواست‌های یک فروشنده.</summary>
    Task<IReadOnlyList<ReturnSnapshot>> ListForSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>فهرست همه درخواست‌ها برای admin.</summary>
    Task<IReadOnlyList<ReturnSnapshot>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>درخواست را تأیید و refund را آغاز می‌کند.</summary>
    Task<ReturnSnapshot> ApproveAsync(ApproveReturnCommand command, CancellationToken cancellationToken);

    /// <summary>درخواست را رد می‌کند.</summary>
    Task<ReturnSnapshot> RejectAsync(RejectReturnCommand command, CancellationToken cancellationToken);

    /// <summary>refund شکست‌خورده را دوباره تلاش می‌کند (admin).</summary>
    Task<ReturnSnapshot> RetryRefundAsync(RetryRefundCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// نگهبان use-case مرجوعی.
/// </summary>
public interface IReturnUseCaseGuard
{
    /// <summary>اجازهٔ mutate را بررسی می‌کند.</summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// رویداد Outbox return.requested.v1
/// </summary>
public sealed class ReturnRequestedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "return.requested.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; set; }
}

/// <summary>
/// رویداد Outbox return.approved.v1
/// </summary>
public sealed class ReturnApprovedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "return.approved.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; set; }

    /// <summary>مبلغ refund.</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>ارز.</summary>
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// رویداد Outbox refund.succeeded.v1
/// </summary>
public sealed class RefundSucceededIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "refund.succeeded.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>پرداخت مرجع.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>مبلغ refund.</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>ارز.</summary>
    public string Currency { get; set; } = string.Empty;
}
