using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tooba.Fulfillment.Application;
using Tooba.Order.Application;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد Returns.
/// </summary>
public sealed class OpenReturnUseCaseGuard : IReturnUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// ارکستراسیون مرجوعی در schema returns.
/// </summary>
public sealed class ReturnDirectory : IReturnDirectory
{
    private static readonly TimeSpan ReturnWindow = TimeSpan.FromDays(30);

    private readonly ReturnsDbContext _db;
    private readonly IReturnUseCaseGuard _guard;
    private readonly IOrderReturnReader _orders;
    private readonly IFulfillmentReturnReader _fulfillment;
    private readonly IPaymentDirectory _payments;
    private readonly IPaymentRefundGateway _refundGateway;
    private readonly IReturnInventoryGateway _inventory;
    private readonly ReturnsInstrumentation _telemetry;
    private readonly ILogger<ReturnDirectory> _logger;

    /// <summary>
    /// دایرکتوری را به schema returns و درز Order/Fulfillment/Payment وصل می‌کند.
    /// </summary>
    public ReturnDirectory(
        ReturnsDbContext db,
        IReturnUseCaseGuard guard,
        IOrderReturnReader orders,
        IFulfillmentReturnReader fulfillment,
        IPaymentDirectory payments,
        IPaymentRefundGateway refundGateway,
        IReturnInventoryGateway inventory,
        ReturnsInstrumentation telemetry,
        ILogger<ReturnDirectory> logger)
    {
        _db = db;
        _guard = guard;
        _orders = orders;
        _fulfillment = fulfillment;
        _payments = payments;
        _refundGateway = refundGateway;
        _inventory = inventory;
        _telemetry = telemetry;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot> CreateAsync(CreateReturnCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var existing = await _db.ReturnRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await MapSnapshotAsync(existing, cancellationToken);
        }

        var orderContext = await _orders.GetReturnContextAsync(command.SellerOrderId, cancellationToken)
            ?? throw new InvalidOperationException("سفارش برای مرجوعی پیدا نشد.");
        if (orderContext.PlacedByUserId != command.ActorUserId)
        {
            throw new InvalidOperationException("درخواست‌دهنده مالک سفارش نیست.");
        }

        if (!orderContext.IsPaid)
        {
            throw new InvalidOperationException("مرجوعی فقط برای سفارش Paid مجاز است.");
        }

        var fulfillment = await _fulfillment.GetEligibilityAsync(command.SellerOrderId, cancellationToken)
            ?? throw new InvalidOperationException("اطلاعات fulfillment برای مرجوعی پیدا نشد.");

        if (fulfillment.LastDeliveredAt is null)
        {
            throw new InvalidOperationException("هنوز تحویلی ثبت نشده است.");
        }

        var now = DateTimeOffset.UtcNow;
        if (now - fulfillment.LastDeliveredAt.Value > ReturnWindow)
        {
            throw new InvalidOperationException("مهلت ۳۰ روزهٔ مرجوعی گذشته است.");
        }

        var alreadyReturned = await GetAlreadyReturnedQuantitiesAsync(command.SellerOrderId, cancellationToken);
        var lineSnapshots = new List<(Guid OrderLineId, int Quantity, decimal UnitPriceSnapshot, Guid? ReservationId)>();
        foreach (var item in command.Items)
        {
            var orderLine = orderContext.Lines.SingleOrDefault(x => x.OrderLineId == item.OrderLineId)
                ?? throw new InvalidOperationException("خط سفارش پیدا نشد.");
            fulfillment.DeliveredQuantities.TryGetValue(item.OrderLineId, out var delivered);
            alreadyReturned.TryGetValue(item.OrderLineId, out var returned);
            var remaining = delivered - returned;
            if (item.Quantity <= 0 || item.Quantity > remaining)
            {
                throw new InvalidOperationException("تعداد مرجوعی از باقیماندهٔ تحویل‌شده بیشتر است.");
            }

            lineSnapshots.Add((item.OrderLineId, item.Quantity, orderLine.UnitPriceSnapshot, orderLine.ReservationId));
        }

        var request = ReturnRequest.Create(
            orderContext.SellerOrderId,
            orderContext.CheckoutId,
            orderContext.SellerPartyId,
            command.ActorUserId,
            command.IdempotencyKey,
            command.Reason,
            orderContext.Currency,
            lineSnapshots,
            now);
        _db.ReturnRequests.Add(request);
        _db.ReturnItems.AddRange(request.Items);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordCreated();
        return await MapSnapshotAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var request = await _db.ReturnRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken);
        return request is null ? null : await MapSnapshotAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReturnSnapshot>> ListForCustomerAsync(
        Guid customerUserId,
        CancellationToken cancellationToken)
    {
        var requests = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.RequestedByUserId == customerUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        return await MapManyAsync(requests, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReturnSnapshot>> ListForSellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var requests = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        return await MapManyAsync(requests, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReturnSnapshot>> ListAllAsync(CancellationToken cancellationToken)
    {
        var requests = await _db.ReturnRequests.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        return await MapManyAsync(requests, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot> ApproveAsync(ApproveReturnCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
        var payment = await ResolvePaymentAsync(request, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت موفق برای refund پیدا نشد.");
        if (payment.Status != PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("refund فقط برای پرداخت Succeeded مجاز است.");
        }

        request.Approve(payment.PaymentId, DateTimeOffset.UtcNow);
        request.MarkRefundProcessing(DateTimeOffset.UtcNow);
        var attempt = request.BeginRefundAttempt(payment.PaymentId, $"refund-{request.ReturnRequestId:N}", DateTimeOffset.UtcNow);
        _db.RefundAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordApproved();

        await ExecuteRefundAsync(request, attempt, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot> RejectAsync(RejectReturnCommand command, CancellationToken cancellationToken)
    {
        _ = command.Reason;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
        request.Reject(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordRejected();
        return await MapSnapshotAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot> RetryRefundAsync(RetryRefundCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
        if (request.Status != ReturnRequestStatus.RefundFailed)
        {
            throw new InvalidOperationException("retry فقط برای RefundFailed مجاز است.");
        }

        var paymentId = request.PaymentId
            ?? throw new InvalidOperationException("پرداخت مرجع پیدا نشد.");
        request.MarkRefundProcessing(DateTimeOffset.UtcNow);
        var attempt = request.BeginRefundAttempt(
            paymentId,
            $"refund-retry-{request.ReturnRequestId:N}-{request.RefundAttempts.Count + 1}",
            DateTimeOffset.UtcNow);
        _db.RefundAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordRetry();

        await ExecuteRefundAsync(request, attempt, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }

    private async Task ExecuteRefundAsync(ReturnRequest request, RefundAttempt attempt, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            var result = await _refundGateway.RefundAsync(
                attempt.PaymentId,
                attempt.Amount,
                attempt.Currency,
                attempt.IdempotencyKey,
                cancellationToken);
            if (result.Succeeded)
            {
                attempt.MarkSucceeded(result.ProviderReference ?? $"refund-{attempt.RefundAttemptId:N}", now);
                request.MarkRefundSucceeded(now);
                _telemetry.RecordRefundSucceeded();
                foreach (var item in request.Items.Where(x => x.ReservationId is not null))
                {
                    await _inventory.RestockConsumedReservationAsync(item.ReservationId!.Value, item.Quantity, cancellationToken);
                }
            }
            else
            {
                attempt.MarkFailed(result.FailureCode ?? "REFUND_FAILED", now);
                request.MarkRefundFailed(now);
                _telemetry.RecordRefundFailed();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refund gateway failed for return {ReturnRequestId}", request.ReturnRequestId);
            attempt.MarkFailed("GATEWAY_ERROR", now);
            request.MarkRefundFailed(now);
            _telemetry.RecordRefundFailed();
        }
    }

    private async Task<PaymentSnapshot?> ResolvePaymentAsync(ReturnRequest request, CancellationToken cancellationToken) =>
        request.PaymentId is { } existing
            ? await _payments.GetAsync(existing, request.RequestedByUserId, null, cancellationToken)
            : await _payments.GetLatestForCheckoutAsync(request.CheckoutId, request.RequestedByUserId, null, cancellationToken);

    private async Task<Dictionary<Guid, int>> GetAlreadyReturnedQuantitiesAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var activeStatuses = new[]
        {
            ReturnRequestStatus.Requested,
            ReturnRequestStatus.Approved,
            ReturnRequestStatus.RefundProcessing,
            ReturnRequestStatus.Completed,
        };
        var requestIds = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId && activeStatuses.Contains(x.Status))
            .Select(x => x.ReturnRequestId)
            .ToListAsync(cancellationToken);
        if (requestIds.Count == 0)
        {
            return [];
        }

        var items = await _db.ReturnItems.AsNoTracking()
            .Where(x => requestIds.Contains(x.ReturnRequestId))
            .ToListAsync(cancellationToken);
        return items
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
    }

    private async Task<ReturnRequest> LoadMutableAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var request = await _db.ReturnRequests.SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken)
            ?? throw new InvalidOperationException("درخواست مرجوعی پیدا نشد.");
        var items = await _db.ReturnItems.Where(x => x.ReturnRequestId == returnRequestId).ToListAsync(cancellationToken);
        var attempts = await _db.RefundAttempts.Where(x => x.ReturnRequestId == returnRequestId).ToListAsync(cancellationToken);
        request.AttachLoadedItems(items);
        request.AttachLoadedRefundAttempts(attempts);
        return request;
    }

    private async Task<IReadOnlyList<ReturnSnapshot>> MapManyAsync(
        IReadOnlyList<ReturnRequest> requests,
        CancellationToken cancellationToken)
    {
        var results = new List<ReturnSnapshot>(requests.Count);
        foreach (var request in requests)
        {
            results.Add(await MapSnapshotAsync(request, cancellationToken));
        }

        return results;
    }

    private async Task<ReturnSnapshot> MapSnapshotAsync(ReturnRequest request, CancellationToken cancellationToken)
    {
        var items = await _db.ReturnItems.AsNoTracking()
            .Where(x => x.ReturnRequestId == request.ReturnRequestId)
            .ToListAsync(cancellationToken);
        var attempts = await _db.RefundAttempts.AsNoTracking()
            .Where(x => x.ReturnRequestId == request.ReturnRequestId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return new ReturnSnapshot(
            request.ReturnRequestId,
            request.SellerOrderId,
            request.CheckoutId,
            request.SellerPartyId,
            request.RequestedByUserId,
            request.Status,
            request.Reason,
            request.Currency,
            request.RefundAmount,
            request.PaymentId,
            request.CreatedAt,
            request.UpdatedAt,
            items.Select(x => new ReturnItemSnapshot(
                x.ReturnItemId,
                x.OrderLineId,
                x.Quantity,
                x.UnitPriceSnapshot,
                x.Currency,
                x.ReservationId)).ToArray(),
            attempts.Select(x => new RefundAttemptSnapshot(
                x.RefundAttemptId,
                x.PaymentId,
                x.Amount,
                x.Currency,
                x.Status,
                x.IdempotencyKey,
                x.ProviderReference,
                x.FailureCode,
                x.CreatedAt,
                x.CompletedAt)).ToArray());
    }
}
