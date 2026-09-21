using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Microsoft.Extensions.Logging;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Contracts.Refunds;

namespace Tooba.Returns.Infrastructure.Directories;

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
/// مقصد Wallet: اعتبار ledger یک‌بار؛ PSP refund gateway صدا زده نمی‌شود (no double-credit).
/// </summary>
public sealed class ReturnDirectory : IReturnDirectory
{
    private readonly ReturnsDbContext _db;
    private readonly IReturnUseCaseGuard _guard;
    private readonly IOrderReturnReader _orders;
    private readonly IReturnEligibilityEvaluator _eligibility;
    private readonly IPaymentReturnReader _payments;
    private readonly IPaymentRefundGateway _refundGateway;
    private readonly IWalletRefundCreditPort _wallets;
    private readonly IReturnInventoryGateway _inventory;
    private readonly ReturnsInstrumentation _telemetry;
    private readonly ILogger<ReturnDirectory> _logger;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>
    /// دایرکتوری را به schema returns و درز Order/Payment/Wallet و evaluator وصل می‌کند.
    /// </summary>
    public ReturnDirectory(
        ReturnsDbContext db,
        IReturnUseCaseGuard guard,
        IOrderReturnReader orders,
        IReturnEligibilityEvaluator eligibility,
        IPaymentReturnReader payments,
        IPaymentRefundGateway refundGateway,
        IWalletRefundCreditPort wallets,
        IReturnInventoryGateway inventory,
        ReturnsInstrumentation telemetry,
        ILogger<ReturnDirectory> logger,
        IClock clock,
        IIdGenerator ids)
    {
        _db = db;
        _guard = guard;
        _orders = orders;
        _eligibility = eligibility;
        _payments = payments;
        _refundGateway = refundGateway;
        _wallets = wallets;
        _inventory = inventory;
        _telemetry = telemetry;
        _logger = logger;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    public Task<ReturnSnapshot> CreateAsync(CreateReturnCommand command, CancellationToken cancellationToken) =>
        CreateInternalAsync(command, requireOwner: true, cancellationToken);

    /// <inheritdoc />
    public Task<ReturnSnapshot> CreateAdminInitiatedAsync(CreateReturnCommand command, CancellationToken cancellationToken) =>
        CreateInternalAsync(command, requireOwner: false, cancellationToken);

    /// <inheritdoc />
    public Task<ReturnEligibilityResult> EvaluateEligibilityAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken) =>
        _eligibility.EvaluateAsync(sellerOrderId, cancellationToken);

    private async Task<ReturnSnapshot> CreateInternalAsync(
        CreateReturnCommand command,
        bool requireOwner,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var existing = await _db.ReturnRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await MapSnapshotAsync(existing, cancellationToken);
        }

        var orderContext = await _orders.GetReturnContextAsync(command.SellerOrderId, cancellationToken)
            ?? throw new InvalidOperationException("returns." + ReturnEligibilityReasonCodes.OrderMissing);
        if (requireOwner && orderContext.PlacedByUserId != command.ActorUserId)
        {
            throw new InvalidOperationException("returns.actor.not_owner");
        }

        var eligibility = await _eligibility.EvaluateAsync(command.SellerOrderId, cancellationToken);
        if (!eligibility.Eligible)
        {
            throw new InvalidOperationException("returns." + eligibility.ReasonCode);
        }

        var remainingByLine = eligibility.Lines.ToDictionary(x => x.OrderLineId, x => x.RemainingReturnableQuantity);
        var lineSnapshots = new List<(Guid OrderLineId, decimal Quantity, decimal UnitPriceSnapshot, Guid? ReservationId)>();
        foreach (var item in command.Items)
        {
            var orderLine = orderContext.Lines.SingleOrDefault(x => x.OrderLineId == item.OrderLineId)
                ?? throw new InvalidOperationException("returns.order_line.not_found");
            remainingByLine.TryGetValue(item.OrderLineId, out var remaining);
            if (item.Quantity <= 0 || item.Quantity > remaining)
            {
                throw new InvalidOperationException("returns.qty.exceeds_remaining");
            }

            lineSnapshots.Add((item.OrderLineId, item.Quantity, orderLine.UnitPriceSnapshot, orderLine.ReservationId));
        }

        var now = _clock.UtcNow;
        var requestedBy = orderContext.PlacedByUserId;
        var request = ReturnRequest.Create(
            _ids.NewId(),
            () => _ids.NewId(),
            orderContext.SellerOrderId,
            orderContext.CheckoutId,
            orderContext.SellerPartyId,
            requestedBy,
            command.IdempotencyKey,
            command.Reason,
            orderContext.Currency,
            lineSnapshots,
            now,
            command.RefundDestination);
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
    public async Task<IReadOnlyList<ReturnSnapshot>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        if (sellerOrderIds.Count == 0)
        {
            return [];
        }

        var requests = await _db.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        return await MapManyAsync(requests, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReturnStatusOverlayRow>> ListStatusOverlayAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.ReturnRequests.AsNoTracking()
            .Select(r => new { r.SellerOrderId, r.Status })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new ReturnStatusOverlayRow(r.SellerOrderId, r.Status)).ToList();
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot> ApproveAsync(ApproveReturnCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
        var payment = await ResolvePaymentAsync(request, cancellationToken)
            ?? throw new InvalidOperationException("returns.payment.not_found");
        if (!string.Equals(payment.Status, "Succeeded", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("returns.payment.not_succeeded");
        }

        request.Approve(payment.PaymentId, _clock.UtcNow, command.RefundDestination);
        request.MarkRefundProcessing(_clock.UtcNow);
        var attempt = request.BeginRefundAttempt(_ids.NewId(), payment.PaymentId, $"refund-{request.ReturnRequestId:N}", _clock.UtcNow);
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
        request.Reject(_clock.UtcNow);
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
            throw new InvalidOperationException("returns.retry.invalid_status");
        }

        var paymentId = request.PaymentId
            ?? throw new InvalidOperationException("returns.payment.reference_missing");
        request.MarkRefundProcessing(_clock.UtcNow);
        var attempt = request.BeginRefundAttempt(
            _ids.NewId(),
            paymentId,
            $"refund-retry-{request.ReturnRequestId:N}-{request.RefundAttempts.Count + 1}",
            _clock.UtcNow);
        _db.RefundAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordRetry();

        await ExecuteRefundAsync(request, attempt, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }

    private async Task ExecuteRefundAsync(ReturnRequest request, RefundAttempt attempt, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        try
        {
            if (request.RefundDestination == RefundDestination.Wallet)
            {
                // انتخاب معماری: اعتبار کیف پول جایگزین PSP gateway است؛ gateway صدا زده نمی‌شود تا double-credit نشود.
                await _wallets.CreditRefundAsync(
                    request.RequestedByUserId,
                    attempt.Amount,
                    attempt.Currency,
                    request.ReturnRequestId,
                    $"wallet-refund-credit:{request.ReturnRequestId:D}",
                    cancellationToken);
                attempt.MarkSucceeded($"wallet-refund:{request.ReturnRequestId:D}", now);
                request.MarkRefundSucceeded(now);
                _telemetry.RecordRefundSucceeded();
                foreach (var item in request.Items.Where(x => x.ReservationId is not null))
                {
                    await _inventory.RestockConsumedReservationAsync(
                        item.ReservationId!.Value,
                        item.Quantity,
                        $"return-restock-{request.ReturnRequestId:N}-{item.ReturnItemId:N}",
                        cancellationToken);
                }

                return;
            }

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
                    await _inventory.RestockConsumedReservationAsync(
                        item.ReservationId!.Value,
                        item.Quantity,
                        $"return-restock-{request.ReturnRequestId:N}-{item.ReturnItemId:N}",
                        cancellationToken);
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

    private async Task<PaymentReturnSnapshot?> ResolvePaymentAsync(ReturnRequest request, CancellationToken cancellationToken) =>
        request.PaymentId is { } existing
            ? await _payments.GetAsync(existing, request.RequestedByUserId, null, cancellationToken)
            : await _payments.GetLatestForCheckoutAsync(request.CheckoutId, request.RequestedByUserId, null, cancellationToken);

    private async Task<ReturnRequest> LoadMutableAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var request = await _db.ReturnRequests.SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken)
            ?? throw new InvalidOperationException("returns.request.not_found");
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
            request.RefundDestination,
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
