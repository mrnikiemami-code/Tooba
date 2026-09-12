using Microsoft.EntityFrameworkCore;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد Payment. شمارهٔ پرداخت به‌تنهایی اجازهٔ جهش نیست.
/// </summary>
public sealed class OpenPaymentUseCaseGuard : IPaymentUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// ارکستراسیون پرداخت در schema payment. مبلغ از تصویر سفارش است نه از کلاینت؛ OrderDbContext اینجا باز نمی‌شود.
/// </summary>
public sealed class PaymentDirectory : IPaymentDirectory, IPaymentReconciliationDirectory, IPaymentAdminDirectory, IPaymentExpiryDirectory
{
    private readonly PaymentDbContext _db;
    private readonly IPaymentUseCaseGuard _guard;
    private readonly IPayableCheckoutReader _orders;
    private readonly IPaymentGatewayRegistry _gateways;
    private readonly PaymentGatewayActorContext _actorContext;
    private readonly IPaymentRefundGateway? _refundGateway;
    private readonly ICommerceHoldPolicy? _holdPolicy;

    /// <summary>
    /// دایرکتوری را به schema payment و رجیستری درگاه وصل می‌کند. تصویر Paid سفارش از Outbox می‌آید نه از همین تراکنش.
    /// </summary>
    public PaymentDirectory(
        PaymentDbContext db,
        IPaymentUseCaseGuard guard,
        IPayableCheckoutReader orders,
        IPaymentGatewayRegistry gateways,
        PaymentGatewayActorContext actorContext,
        IPaymentRefundGateway? refundGateway = null,
        ICommerceHoldPolicy? holdPolicy = null)
    {
        _db = db;
        _guard = guard;
        _orders = orders;
        _gateways = gateways;
        _actorContext = actorContext;
        _refundGateway = refundGateway;
        _holdPolicy = holdPolicy;
    }

    /// <inheritdoc />
    public async Task<PaymentInitiationResult> InitiateAsync(InitiatePaymentCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var key = command.IdempotencyKey.Trim();
        var existing = await _db.Payments.FirstOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            await EnsureActorCanSeeAsync(existing, command.ActorUserId, command.BuyerPartyId, cancellationToken);
            var priorAttempts = await _db.Attempts
                .Where(x => x.PaymentId == existing.PaymentId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            foreach (var loaded in priorAttempts)
            {
                existing.AttachLoadedAttempt(loaded);
            }

            _actorContext.ActorUserId = command.ActorUserId;
            var replayGateway = _gateways.Resolve(existing.ProviderCode);
            if (existing.Status != PaymentStatus.Succeeded
                && await HasSucceededPaymentForCheckoutAsync(existing.CheckoutId, cancellationToken))
            {
                throw AlreadySucceeded();
            }

            if (existing.Status == PaymentStatus.Failed)
            {
                var retryInitiation = await replayGateway.InitiateAsync(
                    existing.PaymentId,
                    existing.Amount,
                    existing.Currency,
                    cancellationToken);
                var retryAttempt = existing.RecordInitiation(retryInitiation.ProviderRequestReference, DateTimeOffset.UtcNow);
                AssignUnpaidTimeout(existing, DateTimeOffset.UtcNow);
                _db.Attempts.Add(retryAttempt);
                await _db.SaveChangesAsync(cancellationToken);
                return new PaymentInitiationResult(
                    existing.PaymentId,
                    retryAttempt.AttemptId,
                    existing.Status,
                    existing.ProviderCode,
                    retryAttempt.ProviderRequestReference,
                    ResolveRedirectUrl(replayGateway, existing.PaymentId, retryAttempt.AttemptId, retryAttempt.ProviderRequestReference, retryInitiation.RedirectUrl),
                    existing.Amount,
                    existing.Currency);
            }

            if (existing.Status == PaymentStatus.Succeeded)
            {
                var priorSucceeded = priorAttempts.OrderByDescending(x => x.CreatedAt).First();
                return new PaymentInitiationResult(
                    existing.PaymentId,
                    priorSucceeded.AttemptId,
                    existing.Status,
                    existing.ProviderCode,
                    priorSucceeded.ProviderRequestReference,
                    ResolveRedirectUrl(replayGateway, existing.PaymentId, priorSucceeded.AttemptId, priorSucceeded.ProviderRequestReference, null),
                    existing.Amount,
                    existing.Currency);
            }

            if (await HasSucceededPaymentForCheckoutAsync(existing.CheckoutId, cancellationToken))
            {
                throw AlreadySucceeded();
            }

            var prior = priorAttempts.OrderByDescending(x => x.CreatedAt).First();
            return new PaymentInitiationResult(
                existing.PaymentId,
                prior.AttemptId,
                existing.Status,
                existing.ProviderCode,
                prior.ProviderRequestReference,
                ResolveRedirectUrl(replayGateway, existing.PaymentId, prior.AttemptId, prior.ProviderRequestReference, null),
                existing.Amount,
                existing.Currency);
        }

        if (await HasSucceededPaymentForCheckoutAsync(command.CheckoutId, cancellationToken))
        {
            throw AlreadySucceeded();
        }

        var payable = await _orders.GetPayableAsync(command.CheckoutId, command.ActorUserId, command.BuyerPartyId, cancellationToken)
            ?? throw new InvalidOperationException("checkout قابل پرداخت پیدا نشد.");
        if (payable.Mode != OrderPaymentMode.OnlinePurchase)
        {
            throw new InvalidOperationException("درخواست رزرو در ثبت اولیه پرداخت نمی‌خواهد.");
        }

        var pending = payable.SellerOrders.Where(x => x.PendingPayment).ToArray();
        if (pending.Length == 0)
        {
            throw AlreadySucceeded();
        }

        if (pending.Select(x => x.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1
            || !string.Equals(payable.Currency, pending[0].Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ارز پرداخت باید با تصویر سفارش یکی باشد؛ تبدیل ارز در Payment نیست.");
        }

        var merchandise = pending.Sum(x => x.PayableAmount);
        var shipping = Math.Max(0m, payable.ShippingAmount);
        var amount = merchandise + shipping;
        var gateway = _gateways.Resolve(command.ProviderCode);
        // تخصیص فروشنده فقط مبلغ کالا؛ ارسال به باکت StoreShipping (نه اولین فروشنده).
        var allocationRows = pending
            .OrderBy(x => x.SellerOrderId)
            .Select(x => (PaymentAllocationTargetKind.SellerOrder, x.SellerOrderId, x.PayableAmount))
            .ToList();
        if (shipping > 0m)
        {
            allocationRows.Add((
                PaymentAllocationTargetKind.StoreShipping,
                PaymentAllocation.StoreShippingTargetId,
                shipping));
        }

        var payment = CustomerPayment.Open(
            payable.CheckoutId,
            amount,
            payable.Currency,
            gateway.ProviderCode,
            key,
            allocationRows,
            DateTimeOffset.UtcNow);
        _actorContext.ActorUserId = command.ActorUserId;
        var initiation = await gateway.InitiateAsync(payment.PaymentId, payment.Amount, payment.Currency, cancellationToken);
        var attempt = payment.RecordInitiation(initiation.ProviderRequestReference, DateTimeOffset.UtcNow);
        AssignUnpaidTimeout(payment, DateTimeOffset.UtcNow);
        _db.Payments.Add(payment);
        _db.Allocations.AddRange(payment.Allocations);
        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
        return new PaymentInitiationResult(
            payment.PaymentId,
            attempt.AttemptId,
            payment.Status,
            payment.ProviderCode,
            attempt.ProviderRequestReference,
            ResolveRedirectUrl(gateway, payment.PaymentId, attempt.AttemptId, attempt.ProviderRequestReference, initiation.RedirectUrl),
            payment.Amount,
            payment.Currency);
    }

    /// <inheritdoc />
    public async Task<int> ReconcileStalePendingAsync(
        DateTimeOffset asOf,
        TimeSpan minAge,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var cutoff = asOf - minAge;
        var pending = await _db.Payments.AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Pending && x.UpdatedAt <= cutoff)
            .OrderBy(x => x.UpdatedAt)
            .Take(Math.Max(1, batchSize))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var processed = 0;
        foreach (var payment in pending)
        {
            var attempt = await _db.Attempts.AsNoTracking()
                .Where(x => x.PaymentId == payment.PaymentId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (attempt is null)
            {
                continue;
            }

            await VerifyAsync(
                new VerifyPaymentCommand(
                    payment.PaymentId,
                    attempt.AttemptId,
                    attempt.ProviderRequestReference,
                    false),
                cancellationToken).ConfigureAwait(false);
            processed++;
        }

        return processed;
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> VerifyAsync(VerifyPaymentCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.PaymentId == command.PaymentId, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        var attempt = await _db.Attempts.SingleOrDefaultAsync(
            x => x.AttemptId == command.AttemptId && x.PaymentId == payment.PaymentId,
            cancellationToken)
            ?? throw new InvalidOperationException("تلاش پرداخت پیدا نشد.");
        if (attempt.ProviderRequestReference != command.ProviderRequestReference)
        {
            throw new InvalidOperationException("مرجع درگاه با تلاش ذخیره‌شده یکی نیست.");
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, false);
        }

        var gateway = _gateways.Resolve(payment.ProviderCode);
        var verified = await gateway.VerifyAsync(command.ProviderRequestReference, command.CallbackClaimsSuccess, cancellationToken);
        payment.AttachLoadedAttempt(attempt);
        var allocations = await _db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken);
        payment.AttachLoadedAllocations(allocations);
        if (!verified.VerifiedSuccess || string.IsNullOrWhiteSpace(verified.ProviderTransactionReference))
        {
            // Unknown/timeout/rate-limit: Pending می‌ماند؛ فقط شکست قطعی درگاه Failed می‌شود.
            if (PaymentGatewayOutcomes.IsIndeterminate(verified.FailureCode))
            {
                return new PaymentVerificationResult(payment.PaymentId, payment.Status, false);
            }

            payment.ApplyVerifiedFailure(attempt.AttemptId, verified.FailureCode, DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, false);
        }

        var duplicateTxn = await _db.Attempts.AnyAsync(
            x => x.ProviderTransactionReference == verified.ProviderTransactionReference,
            cancellationToken);
        if (duplicateTxn)
        {
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, false);
        }

        var firstSuccess = payment.ApplyVerifiedSuccess(attempt.AttemptId, verified.ProviderTransactionReference, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, firstSuccess);
    }

    /// <inheritdoc />
    public async Task<PaymentSnapshot?> GetAsync(Guid paymentId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        await EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        var allocations = await _db.Allocations.Where(x => x.PaymentId == paymentId).ToListAsync(cancellationToken);
        var attempts = await _db.Attempts.AsNoTracking()
            .Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return ToSnapshot(payment, allocations, attempts);
    }

    /// <inheritdoc />
    public async Task<PaymentSnapshot?> GetLatestForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var payable = await _orders.GetPayableAsync(checkoutId, actorUserId, buyerPartyId, cancellationToken);
        if (payable is null)
        {
            return null;
        }

        var payment = await _db.Payments.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (payment is null)
        {
            return null;
        }

        var allocations = await _db.Allocations.AsNoTracking()
            .Where(x => x.PaymentId == payment.PaymentId)
            .ToListAsync(cancellationToken);
        var attempts = await _db.Attempts.AsNoTracking()
            .Where(x => x.PaymentId == payment.PaymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return ToSnapshot(payment, allocations, attempts);
    }

    /// <inheritdoc />
    public Task<bool> HasSucceededPaymentForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        _db.Payments.AsNoTracking()
            .AnyAsync(x => x.CheckoutId == checkoutId && x.Status == PaymentStatus.Succeeded, cancellationToken);

    internal static InvalidOperationException AlreadySucceeded() =>
        new("پرداخت این سفارش قبلاً با موفقیت انجام شده است.");

    /// <inheritdoc />
    public async Task RegisterProofAssetAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        await EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new InvalidOperationException("payment.method.not_manual");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("payment.manual.submit.invalid_state");
        }

        var duplicate = await _db.ProofAssets.AnyAsync(x => x.MediaAssetId == mediaAssetId, cancellationToken);
        if (duplicate)
        {
            var owned = await _db.ProofAssets.AnyAsync(
                x => x.MediaAssetId == mediaAssetId && x.PaymentId == paymentId,
                cancellationToken);
            if (!owned)
            {
                throw new InvalidOperationException("payment.proof.foreign");
            }

            return;
        }

        _db.ProofAssets.Add(PaymentProofAsset.Attach(paymentId, mediaAssetId, DateTimeOffset.UtcNow));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SubmitManualEvidenceAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        string transferReference,
        Guid? proofMediaAssetId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        await EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        if (payment.Status == PaymentStatus.Succeeded)
        {
            throw AlreadySucceeded();
        }

        var attempts = await _db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        if (proofMediaAssetId is Guid assetId)
        {
            var owned = await _db.ProofAssets.AnyAsync(
                x => x.PaymentId == paymentId && x.MediaAssetId == assetId,
                cancellationToken);
            if (!owned)
            {
                throw new InvalidOperationException("payment.proof.foreign");
            }
        }

        payment.SubmitManualEvidence(transferReference, proofMediaAssetId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RetryManualAfterRejectionAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        await EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        await RestoreDepositAsync(paymentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaymentOperationalSnapshot?> GetOperationalAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return null;
        }

        return await ToOperationalAsync(payment, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return null;
        }

        return await ToOperationalAsync(payment, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> ReconcileAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("payment.missing");
        var attempt = await _db.Attempts.AsNoTracking()
            .Where(x => x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("payment.attempt.missing");
        return await VerifyAsync(
            new VerifyPaymentCommand(
                payment.PaymentId,
                attempt.AttemptId,
                attempt.ProviderRequestReference,
                false),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> ConfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await _db.Payments
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("payment.missing");
        if (payment.Status == PaymentStatus.Succeeded)
        {
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
        }

        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new InvalidOperationException("payment.method.not_manual");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("payment.confirm.invalid_state");
        }

        var attempt = await _db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("payment.attempt.missing");
        if (string.IsNullOrWhiteSpace(attempt.CustomerTransferReference))
        {
            throw new InvalidOperationException("شماره پیگیری پرداخت الزامی است.");
        }

        payment.AttachLoadedAttempt(attempt);
        var allocations = await _db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        payment.AttachLoadedAllocations(allocations);
        var txn = $"manual-confirm-{payment.PaymentId:N}-{attempt.AttemptId:N}";
        var duplicateTxn = await _db.Attempts.AnyAsync(
            x => x.ProviderTransactionReference == txn,
            cancellationToken).ConfigureAwait(false);
        if (duplicateTxn)
        {
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
        }

        var firstSuccess = payment.ApplyVerifiedSuccess(attempt.AttemptId, txn, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, firstSuccess);
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> RejectDepositAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await _db.Payments
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("payment.missing");
        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new InvalidOperationException("payment.method.not_manual");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("payment.reject.invalid_state");
        }

        var attempt = await _db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("payment.attempt.missing");

        payment.AttachLoadedAttempt(attempt);
        var allocations = await _db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        payment.AttachLoadedAllocations(allocations);
        payment.ApplyVerifiedFailure(attempt.AttemptId, "MANUAL_DEPOSIT_REJECTED", DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> RestoreDepositAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await _db.Payments
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("payment.missing");
        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new InvalidOperationException("payment.restore.not_manual");
        }

        var attempts = await _db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        var allocations = await _db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        payment.AttachLoadedAllocations(allocations);
        var restored = payment.RestoreRejectedManualToPending(DateTimeOffset.UtcNow);
        if (attempts.All(x => x.AttemptId != restored.AttemptId))
        {
            _db.Attempts.Add(restored);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> UnconfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await _db.Payments
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("payment.missing");
        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new InvalidOperationException("payment.unconfirm.not_manual");
        }

        var attempts = await _db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        var allocations = await _db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        payment.AttachLoadedAllocations(allocations);
        var unconfirmed = payment.UnconfirmManualDeposit(DateTimeOffset.UtcNow);
        if (attempts.All(x => x.AttemptId != unconfirmed.AttemptId))
        {
            _db.Attempts.Add(unconfirmed);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
    }

    /// <inheritdoc />
    public async Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await _db.Payments
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (payment.Status is PaymentStatus.Created
            or PaymentStatus.Pending
            or PaymentStatus.Failed
            or PaymentStatus.Expired)
        {
            payment.CloseForOrderCancel(now);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (payment.Status is PaymentStatus.Cancelled or PaymentStatus.Refunded or PaymentStatus.RefundFailed)
        {
            return;
        }

        payment.BeginOrderCancelRefund(now);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (ManualPaymentGateway.IsManual(payment.ProviderCode) || _refundGateway is null)
        {
            return;
        }

        try
        {
            var result = await _refundGateway.RefundAsync(
                payment.PaymentId,
                payment.Amount,
                payment.Currency,
                $"order-cancel-refund:{payment.PaymentId:N}",
                cancellationToken).ConfigureAwait(false);
            if (result.Succeeded)
            {
                payment.MarkRefunded(DateTimeOffset.UtcNow);
            }
            else
            {
                payment.MarkRefundFailed(result.FailureCode, DateTimeOffset.UtcNow);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message == "payment.refund.gateway.unconfigured")
        {
            // صف اقدام ادمین: RefundPending می‌ماند.
        }
        catch (Exception)
        {
            // لغو سفارش به موفقیت Refund وابسته نیست؛ وضعیت RefundPending برای retry امن می‌ماند.
        }
    }

    /// <inheritdoc />
    public async Task RestoreAfterOrderCancelRestoreAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await _db.Payments
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        var attempts = await _db.Attempts
            .Where(x => x.PaymentId == payment.PaymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        payment.RestoreAfterOrderCancelRestore(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<PaymentOperationalSnapshot> ToOperationalAsync(
        CustomerPayment payment,
        CancellationToken cancellationToken)
    {
        var attempts = await _db.Attempts.AsNoTracking()
            .Where(x => x.PaymentId == payment.PaymentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var attempt = attempts.FirstOrDefault();
        var manual = ManualPaymentGateway.IsManual(payment.ProviderCode);
        var manualPending = manual && payment.Status == PaymentStatus.Pending;
        var hasManualRejection = attempts.Any(x =>
            x.Status == PaymentAttemptStatus.VerifiedFailed
            && string.Equals(x.FailureCode, "MANUAL_DEPOSIT_REJECTED", StringComparison.Ordinal));
        var restoreEligible = manual
            && payment.Status == PaymentStatus.Failed
            && hasManualRejection;
        var unconfirmEligible = manual && payment.Status == PaymentStatus.Succeeded;
        var evidenceReady = !string.IsNullOrWhiteSpace(attempt?.CustomerTransferReference);
        return new PaymentOperationalSnapshot(
            payment.PaymentId,
            payment.CheckoutId,
            payment.Status,
            payment.Amount,
            payment.Currency,
            payment.ProviderCode,
            attempt?.ProviderRequestReference,
            attempt?.ProviderTransactionReference,
            payment.CreatedAt,
            payment.UpdatedAt,
            payment.CompletedAt,
            attempt?.FailureCode,
            payment.Status == PaymentStatus.Pending,
            ConfirmDepositEligible: manualPending && evidenceReady,
            RejectDepositEligible: manualPending,
            RestoreDepositEligible: restoreEligible,
            HasManualDepositRejection: hasManualRejection,
            UnconfirmDepositEligible: unconfirmEligible,
            CustomerTransferReference: attempt?.CustomerTransferReference,
            ProofMediaAssetId: attempt?.ProofMediaAssetId,
            EvidenceSubmittedAt: attempt?.EvidenceSubmittedAt);
    }

    private static PaymentSnapshot ToSnapshot(
        CustomerPayment payment,
        IReadOnlyList<PaymentAllocation> allocations,
        IReadOnlyList<PaymentAttempt> attempts)
    {
        var latest = attempts.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        var history = attempts.Select(x => new PaymentManualEvidenceSnapshot(
            x.AttemptId,
            x.Status,
            x.CustomerTransferReference,
            x.ProofMediaAssetId,
            x.EvidenceSubmittedAt,
            x.FailureCode)).ToArray();
        return new PaymentSnapshot(
            payment.PaymentId,
            payment.CheckoutId,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.ProviderCode,
            allocations.Select(x => new PaymentAllocationSnapshot(
                x.SellerOrderId,
                x.AllocatedAmount,
                x.Currency,
                x.TargetKind)).ToArray(),
            latest?.CustomerTransferReference,
            latest?.ProofMediaAssetId,
            latest?.EvidenceSubmittedAt,
            history);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ExpireDueUnpaidAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var limit = Math.Max(1, batchSize);
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var paymentIds = await _db.Database
            .SqlQuery<Guid>(
                $"""
                 SELECT p.payment_id AS "Value"
                 FROM payment.payments AS p
                 WHERE p.status IN ('Created', 'Pending', 'Failed')
                   AND p.unpaid_timeout_at IS NOT NULL
                   AND p.unpaid_timeout_at <= {utcNow}
                 ORDER BY p.unpaid_timeout_at
                 LIMIT {limit}
                 FOR UPDATE SKIP LOCKED
                 """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (paymentIds.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return [];
        }

        var expiredCheckouts = new List<Guid>();
        foreach (var paymentId in paymentIds)
        {
            var payment = await _db.Payments.SingleAsync(x => x.PaymentId == paymentId, cancellationToken)
                .ConfigureAwait(false);
            var attempts = await _db.Attempts
                .Where(x => x.PaymentId == paymentId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var loaded in attempts)
            {
                payment.AttachLoadedAttempt(loaded);
            }

            if (payment.ExpireUnpaidTimeout(utcNow))
            {
                expiredCheckouts.Add(payment.CheckoutId);
            }
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return expiredCheckouts;
    }

    /// <inheritdoc />
    public async Task ReopenExpiredForRetryAsync(
        Guid paymentId,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت پیدا نشد.");
        await EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        if (payment.Status != PaymentStatus.Expired)
        {
            throw new InvalidOperationException("payment.unpaid.retry.invalid_state");
        }

        var attempts = await _db.Attempts.Where(x => x.PaymentId == paymentId).ToListAsync(cancellationToken);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        _actorContext.ActorUserId = actorUserId;
        var gateway = _gateways.Resolve(payment.ProviderCode);
        var initiation = await gateway.InitiateAsync(payment.PaymentId, payment.Amount, payment.Currency, cancellationToken);
        var attempt = payment.RecordInitiation(initiation.ProviderRequestReference, DateTimeOffset.UtcNow);
        AssignUnpaidTimeout(payment, DateTimeOffset.UtcNow);
        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void AssignUnpaidTimeout(CustomerPayment payment, DateTimeOffset now)
    {
        if (_holdPolicy is null)
        {
            return;
        }

        payment.AssignUnpaidTimeout(_holdPolicy.ResolveUnpaidTimeoutAt(payment.ProviderCode, now), now);
    }

    private async Task EnsureActorCanSeeAsync(
        CustomerPayment payment,
        Guid actorUserId,
        Guid? buyerPartyId,
        CancellationToken cancellationToken)
    {
        var payable = await _orders.GetPayableAsync(payment.CheckoutId, actorUserId, buyerPartyId, cancellationToken);
        if (payable is null)
        {
            throw new InvalidOperationException("دسترسی به پرداخت بدون هویت سفارش رد شد.");
        }
    }

    private static string ResolveRedirectUrl(
        IPaymentGateway gateway,
        Guid paymentId,
        Guid attemptId,
        string providerRequestReference,
        string? gatewayRedirect)
    {
        if (gateway is FakePaymentGateway or FakeFailingPaymentGateway)
        {
            return ComposeSandboxRedirect(paymentId, attemptId, providerRequestReference);
        }

        if (!string.IsNullOrWhiteSpace(gatewayRedirect))
        {
            return gatewayRedirect;
        }

        return "/payment/result?paymentId=" + paymentId.ToString("D")
            + "&attemptId=" + attemptId.ToString("D")
            + "&ref=" + Uri.EscapeDataString(providerRequestReference);
    }

    private static string ComposeSandboxRedirect(Guid paymentId, Guid attemptId, string providerRequestReference) =>
        "/payment/sandbox?paymentId=" + paymentId.ToString("D")
        + "&attemptId=" + attemptId.ToString("D")
        + "&ref=" + Uri.EscapeDataString(providerRequestReference);
}
