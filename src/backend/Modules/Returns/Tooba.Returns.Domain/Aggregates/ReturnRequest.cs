using Tooba.BuildingBlocks;
using Tooba.Returns.Domain.Events;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Domain.Aggregates;


/// <summary>
/// درخواست مرجوعی برای یک SellerOrder. aggregate root.
/// </summary>
public sealed class ReturnRequest : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();
    private readonly List<ReturnItem> _items = [];
    private readonly List<RefundAttempt> _refundAttempts = [];

    private ReturnRequest()
    {
    }

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>سفارش فروشنده مرجع.</summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>کاربر درخواست‌دهنده.</summary>
    public Guid RequestedByUserId { get; init; }

    /// <summary>کلید idempotency ایجاد.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>وضعیت چرخه.</summary>
    public ReturnRequestStatus Status { get; private set; }

    /// <summary>دلیل درخواست.</summary>
    public string? Reason { get; init; }

    /// <summary>ارز refund.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>مبلغ refund محاسبه‌شده از snapshot خطوط.</summary>
    public decimal RefundAmount { get; private set; }

    /// <summary>مقصد بازگشت وجه.</summary>
    public RefundDestination RefundDestination { get; private set; }

    /// <summary>شناسه پرداخت snapshot.</summary>
    public Guid? PaymentId { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>خطوط مرجوعی.</summary>
    public IReadOnlyList<ReturnItem> Items => _items;

    /// <summary>تلاش‌های refund.</summary>
    public IReadOnlyList<RefundAttempt> RefundAttempts => _refundAttempts;

    /// <summary>رویدادهای دامنه.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// درخواست مرجوعی را از snapshot خطوط می‌سازد. eligibility در Directory بررسی می‌شود.
    /// </summary>
    public static ReturnRequest Create(
        Guid returnRequestId,
        Func<Guid> newId,
        Guid sellerOrderId,
        Guid checkoutId,
        Guid sellerPartyId,
        Guid requestedByUserId,
        string idempotencyKey,
        string? reason,
        string currency,
        IEnumerable<(Guid OrderLineId, decimal Quantity, decimal UnitPriceSnapshot, Guid? ReservationId)> lines,
        DateTimeOffset now,
        RefundDestination refundDestination = RefundDestination.OriginalPayment)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new InvalidOperationException("returns.idempotency.required");
        }

        if (!Enum.IsDefined(refundDestination))
        {
            throw new InvalidOperationException("returns.refund_destination.invalid");
        }

        var request = new ReturnRequest
        {
            ReturnRequestId = returnRequestId,
            SellerOrderId = sellerOrderId,
            CheckoutId = checkoutId,
            SellerPartyId = sellerPartyId,
            RequestedByUserId = requestedByUserId,
            IdempotencyKey = idempotencyKey.Trim(),
            Status = ReturnRequestStatus.Requested,
            Reason = reason?.Trim(),
            Currency = currency.Trim(),
            RefundDestination = refundDestination,
            CreatedAt = now,
            UpdatedAt = now,
        };
        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("returns.qty.positive");
            }

            request._items.Add(ReturnItem.Create(
                newId(),
                request.ReturnRequestId,
                line.OrderLineId,
                line.Quantity,
                line.UnitPriceSnapshot,
                currency,
                line.ReservationId));
        }

        request.RefundAmount = request._items.Sum(x => x.UnitPriceSnapshot * x.Quantity);
        request._domainEvents.Add(new ReturnRequestedDomainEvent(request.ReturnRequestId, request.SellerOrderId, request.CheckoutId));
        return request;
    }

    /// <summary>خطوط بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedItems(IEnumerable<ReturnItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    /// <summary>تلاش‌های بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedRefundAttempts(IEnumerable<RefundAttempt> attempts)
    {
        _refundAttempts.Clear();
        _refundAttempts.AddRange(attempts);
    }

    /// <summary>درخواست را تأیید می‌کند.</summary>
    public void Approve(Guid paymentId, DateTimeOffset now, RefundDestination? destinationOverride = null)
    {
        EnsureStatus(ReturnRequestStatus.Requested);
        Status = ReturnRequestStatus.Approved;
        PaymentId = paymentId;
        if (destinationOverride is { } dest)
        {
            if (!Enum.IsDefined(dest))
                throw new InvalidOperationException("returns.refund_destination.invalid");
            RefundDestination = dest;
        }

        UpdatedAt = now;
        _domainEvents.Add(new ReturnApprovedDomainEvent(ReturnRequestId, SellerOrderId, CheckoutId, RefundAmount, Currency));
    }

    /// <summary>درخواست را رد می‌کند.</summary>
    public void Reject(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.Requested);
        Status = ReturnRequestStatus.Rejected;
        UpdatedAt = now;
    }

    /// <summary>به RefundProcessing می‌رود.</summary>
    public void MarkRefundProcessing(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.Approved, ReturnRequestStatus.RefundFailed);
        Status = ReturnRequestStatus.RefundProcessing;
        UpdatedAt = now;
    }

    /// <summary>refund را موفق علامت می‌زند.</summary>
    public void MarkRefundSucceeded(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.RefundProcessing);
        Status = ReturnRequestStatus.Completed;
        UpdatedAt = now;
        _domainEvents.Add(new RefundSucceededDomainEvent(ReturnRequestId, SellerOrderId, PaymentId ?? Guid.Empty, RefundAmount, Currency));
    }

    /// <summary>refund را شکست‌خورده علامت می‌زند.</summary>
    public void MarkRefundFailed(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.RefundProcessing);
        Status = ReturnRequestStatus.RefundFailed;
        UpdatedAt = now;
    }

    /// <summary>درخواست را لغو می‌کند.</summary>
    public void Cancel(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.Requested);
        Status = ReturnRequestStatus.Cancelled;
        UpdatedAt = now;
    }

    /// <summary>تلاش refund جدید ثبت می‌کند.</summary>
    public RefundAttempt BeginRefundAttempt(Guid refundAttemptId, Guid paymentId, string idempotencyKey, DateTimeOffset now)
    {
        var attempt = RefundAttempt.CreatePending(refundAttemptId, ReturnRequestId, paymentId, RefundAmount, Currency, idempotencyKey, now);
        _refundAttempts.Add(attempt);
        return attempt;
    }

    private void EnsureStatus(params ReturnRequestStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new InvalidOperationException("fulfillment.status.transition_invalid");
        }
    }
}
