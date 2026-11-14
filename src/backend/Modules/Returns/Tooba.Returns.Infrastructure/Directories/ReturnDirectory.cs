using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Microsoft.Extensions.Logging;
using Tooba.Order.Contracts.Returns;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Contracts.Refunds;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// ارکستراسیون مرجوعی در schema returns.
/// مقصد Wallet: اعتبار ledger یک‌بار؛ PSP refund gateway صدا زده نمی‌شود (no double-credit).
/// </summary>
public sealed partial class ReturnDirectory : IReturnDirectory
{
    private readonly ReturnsDbContext _db;
    private readonly IReturnUseCaseGuard _guard;
    private readonly IOrderReturnReader _orders;
    private readonly IReturnEligibilityEvaluator _eligibility;
    private readonly IPaymentReturnReader _payments;
    private readonly IPaymentRefundGateway _refundGateway;
    private readonly IWalletRefundCreditPort _wallets;
    private readonly IReturnInventoryGateway _inventory;
    private readonly IFulfillmentCourierReturnSource _courierSource;
    private readonly IOrderReturnObligationClosure _obligationClosure;
    private readonly IFulfillmentAdminOperations _fulfillment;
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
        IIdGenerator ids,
        IFulfillmentCourierReturnSource? courierSource = null,
        IOrderReturnObligationClosure? obligationClosure = null,
        IFulfillmentAdminOperations? fulfillment = null)
    {
        _db = db;
        _guard = guard;
        _orders = orders;
        _eligibility = eligibility;
        _payments = payments;
        _refundGateway = refundGateway;
        _wallets = wallets;
        _inventory = inventory;
        _courierSource = courierSource!;
        _obligationClosure = obligationClosure!;
        _fulfillment = fulfillment!;
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
            ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);
        if (requireOwner && orderContext.PlacedByUserId != command.ActorUserId)
        {
            throw new ContractOperationException(ReturnsErrorCodes.NotOwner);
        }

        var eligibility = await _eligibility.EvaluateAsync(command.SellerOrderId, cancellationToken);
        if (!eligibility.Eligible)
        {
            throw new ContractOperationException(ReturnEligibilityReasonCodes.ToErrorCode(eligibility.ReasonCode));
        }

        var remainingByLine = eligibility.Lines.ToDictionary(x => x.OrderLineId, x => x.RemainingReturnableQuantity);
        var lineSnapshots = new List<(Guid OrderLineId, decimal Quantity, decimal UnitPriceSnapshot, Guid? ReservationId)>();
        foreach (var item in command.Items)
        {
            var orderLine = orderContext.Lines.SingleOrDefault(x => x.OrderLineId == item.OrderLineId)
                ?? throw new ContractOperationException(ReturnsErrorCodes.LineMissing);
            remainingByLine.TryGetValue(item.OrderLineId, out var remaining);
            if (item.Quantity <= 0 || item.Quantity > remaining)
            {
                throw new ContractOperationException(ReturnsErrorCodes.QuantityExceeded);
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

        if (request.Origin == ReturnOrigin.CourierOrigin
            && request.ClosurePhase < CourierOriginClosurePhase.RefundReleased
            && request.Status is ReturnRequestStatus.Approved
                or ReturnRequestStatus.RefundProcessing
                or ReturnRequestStatus.RefundFailed
                or ReturnRequestStatus.Completed)
        {
            // TB-265: a previous approval already recorded its durable phases (approval + money attempt). A refused
            // closure or a lost money answer must be RESUMED, never re-approved or duplicated: the same recorded
            // obligation/branch work runs again idempotently and only then does the SAME attempt proceed.
            await EnsureCourierOriginClosureAsync(request, command.ActorUserId, cancellationToken);
            if (request.Status == ReturnRequestStatus.RefundFailed)
            {
                // A failed money attempt is resumed through the existing retry entry, not a second refund attempt.
                await _db.SaveChangesAsync(cancellationToken);
                return await MapSnapshotAsync(request, cancellationToken);
            }

            if (request.Status == ReturnRequestStatus.Completed)
            {
                // TB-265-R1: money already succeeded and the obligation is closed, so ONLY the unfinished physical
                // disposition is retried here. No second money attempt is created.
                await SettlePhysicalDispositionAsync(request, cancellationToken);
                MarkRefundReleasedWhenFullyDisposed(request);
                await _db.SaveChangesAsync(cancellationToken);
                return await MapSnapshotAsync(request, cancellationToken);
            }

            var pending = request.RefundAttempts
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault(x => x.Status == RefundAttemptStatus.Pending)
                ?? throw new ContractOperationException(ReturnsErrorCodes.RefundRetryInvalidState);
            await ExecuteRefundAsync(request, pending, cancellationToken);
            if (request.Status == ReturnRequestStatus.Completed)
            {
                MarkRefundReleasedWhenFullyDisposed(request);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        var payment = await ResolvePaymentAsync(request, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
        if (!string.Equals(payment.Status, "Succeeded", StringComparison.Ordinal))
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundAlreadyStarted);
        }

        request.Approve(payment.PaymentId, _clock.UtcNow, command.RefundDestination);
        request.MarkRefundProcessing(_clock.UtcNow);
        var attempt = request.BeginRefundAttempt(_ids.NewId(), payment.PaymentId, $"refund-{request.ReturnRequestId:N}", _clock.UtcNow);
        _db.RefundAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordApproved();

        if (request.Origin == ReturnOrigin.CourierOrigin)
        {
            // TB-265: obligation + branch closure BEFORE money. A refused closure keeps the approval pending and
            // never lets a refund run over live replacement stock.
            await EnsureCourierOriginClosureAsync(request, command.ActorUserId, cancellationToken);
        }

        await ExecuteRefundAsync(request, attempt, cancellationToken);
        if (request.Origin == ReturnOrigin.CourierOrigin && request.Status == ReturnRequestStatus.Completed)
        {
            MarkRefundReleasedWhenFullyDisposed(request);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot> RejectAsync(RejectReturnCommand command, CancellationToken cancellationToken)
    {
        _ = command.Reason;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);

        // TB-265-R1: only a case that was never approved can be rejected. A case already refunding/approved keeps
        // its obligation and its claim; releasing its branch here would free capacity a live (or completed) case
        // still owns and would let the same units fund a second branch.
        if (request.Status == ReturnRequestStatus.Requested)
        {
            request.Reject(_clock.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            _telemetry.RecordRejected();

            // Releasing the source branch AFTER the durable reject is the only lawful release of this claim.
            await ReleaseCourierOriginClaimAsync(
                request, nameof(ReturnRequestStatus.Rejected), command.ActorUserId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        if (request.Status is ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled)
        {
            // Exact terminal replay: finish any still-pending claim release, and never touch money or stock.
            await ReleaseCourierOriginClaimAsync(
                request, request.Status.ToString(), command.ActorUserId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        throw new ContractOperationException("return.reject.invalid_state");
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot> RetryRefundAsync(RetryReturnRefundCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);

        if (request.Origin == ReturnOrigin.CourierOrigin
            && request.Status == ReturnRequestStatus.Completed
            && request.ClosurePhase < CourierOriginClosurePhase.RefundReleased)
        {
            // TB-265-R1: the money already succeeded; retry ONLY the unfinished physical disposition with the same
            // per-item identity. No new refund attempt and no new idempotency key is minted here.
            await SettlePhysicalDispositionAsync(request, cancellationToken);
            MarkRefundReleasedWhenFullyDisposed(request);
            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        if (request.Status != ReturnRequestStatus.RefundFailed)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundRetryInvalidState);
        }

        var paymentId = request.PaymentId
            ?? throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
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
                await _db.SaveChangesAsync(cancellationToken);
                await TrySettlePhysicalDispositionAsync(request, cancellationToken);
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
                await _db.SaveChangesAsync(cancellationToken);
                await TrySettlePhysicalDispositionAsync(request, cancellationToken);
            }
            else
            {
                // A proven terminal refusal is a real failed money attempt.
                attempt.MarkFailed(result.FailureCode ?? "REFUND_FAILED", now);
                request.MarkRefundFailed(now);
                _telemetry.RecordRefundFailed();
            }
        }
        catch (Exception ex)
        {
            // TB-265-R1: an UNKNOWN/transport outcome is not a failure. The durable attempt and its idempotency key
            // are retained, the case stays RefundProcessing, and the existing resume path reconciles the SAME
            // attempt instead of minting a second refund key over a possibly completed refund.
            _logger.LogWarning(ex, "Refund outcome unknown for return {ReturnRequestId}", request.ReturnRequestId);
        }
    }

    /// <summary>
    /// TB-265-R1: the money phase is only recorded as finished when every received line also has its physical
    /// disposition. A failed/pending restock keeps the case <c>Completed</c> with <c>StockSettled</c>, so the
    /// disposition-only retry stays reachable and no second refund attempt is ever minted for it.
    /// </summary>
    private void MarkRefundReleasedWhenFullyDisposed(ReturnRequest request)
    {
        if (request.Items.Any(x => x.Disposition == ReturnItemDisposition.None))
        {
            return;
        }

        request.MarkRefundReleased(_clock.UtcNow);
    }

    /// <summary>
    /// TB-265-R1: the physical disposition is a separate owner phase from money. Its failure can never change a
    /// successful refund into a failed one; the unfinished item stays explicitly pending for a disposition-only retry.
    /// </summary>
    private async Task TrySettlePhysicalDispositionAsync(ReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await SettlePhysicalDispositionAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Physical disposition pending for return {ReturnRequestId}", request.ReturnRequestId);
        }
    }

    private async Task<PaymentReturnSnapshot?> ResolvePaymentAsync(ReturnRequest request, CancellationToken cancellationToken) =>
        request.PaymentId is { } existing
            ? await _payments.GetAsync(existing, request.RequestedByUserId, null, cancellationToken)
            : await _payments.GetLatestForCheckoutAsync(request.CheckoutId, request.RequestedByUserId, null, cancellationToken);

    private async Task<ReturnRequest> LoadMutableAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var request = await _db.ReturnRequests.SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);
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
                x.ReservationId,
                x.Condition,
                x.Disposition,
                x.SourceShipmentId,
                x.SourceReceiptId)).ToArray(),
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
                x.CompletedAt)).ToArray(),
            request.Origin,
            request.SourceFulfillmentId,
            request.SourceShipmentId,
            request.SourceReceiptId,
            request.SourceClaimId,
            request.SourceOccurrenceKey,
            request.ClosurePhase,
            request.ObligationClosedAt,
            request.StockSettledAt,
            request.ReleasedHeldQuantity);
    }
}
