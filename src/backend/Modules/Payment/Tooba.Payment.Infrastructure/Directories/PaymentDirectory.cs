using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Contracts.Checkout;
using Tooba.Payment.Contracts.Errors;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Infrastructure.Directories.Shared;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Providers;

namespace Tooba.Payment.Infrastructure.Directories;

/// <summary>
/// ارکستراسیون پرداخت در schema payment. مبلغ از تصویر سفارش است نه از کلاینت؛ OrderDbContext اینجا باز نمی‌شود.
/// این دایرکتوری فقط <see cref="IPaymentDirectory"/> را پیاده می‌کند؛ بازرسی/مدیریت، reconciliation و انقضای پرداخت‌نشده
/// در دایرکتوری‌های متمرکز جداگانه‌اند.
/// </summary>
public sealed class PaymentDirectory : IPaymentDirectory
{
    /// <summary>TB240-R3: نام واقعی ایندکس یکتای کلید idempotency روی <c>payment.payments</c>.</summary>
    private const string PaymentIdempotencyIndexName = "ix_payments_idempotency_key";

    /// <summary>TB240-R3: نام واقعی جدول پرداخت برای تطبیق دقیق نقض یکتایی.</summary>
    private const string PaymentTableName = "payments";

    private readonly PaymentDbContext _db;
    private readonly IPaymentUseCaseGuard _guard;
    private readonly PaymentActorAccess _actorAccess;
    private readonly IPayableCheckoutReader _orders;
    private readonly IPaymentGatewayRegistry _gateways;
    private readonly PaymentGatewayActorContext _actorContext;
    private readonly PaymentUnpaidTimeoutAssigner _timeoutAssigner;
    private readonly Func<IPaymentAdminDirectory>? _adminDirectory;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>
    /// دایرکتوری را به schema payment و رجیستری درگاه وصل می‌کند. تصویر Paid سفارش از Outbox می‌آید نه از همین تراکنش.
    /// <paramref name="adminDirectory"/> به‌صورت deferred تزریق می‌شود تا چرخهٔ ساخت PaymentDirectory↔PaymentAdminDirectory شکسته شود.
    /// </summary>
    public PaymentDirectory(
        PaymentDbContext db,
        IPaymentUseCaseGuard guard,
        IPayableCheckoutReader orders,
        IPaymentGatewayRegistry gateways,
        PaymentGatewayActorContext actorContext,
        IClock clock,
        IIdGenerator ids,
        Func<IPaymentAdminDirectory>? adminDirectory = null,
        ICommerceHoldPolicy? holdPolicy = null)
    {
        _db = db;
        _guard = guard;
        _actorAccess = new PaymentActorAccess(orders);
        _orders = orders;
        _gateways = gateways;
        _actorContext = actorContext;
        _timeoutAssigner = new PaymentUnpaidTimeoutAssigner(holdPolicy);
        _adminDirectory = adminDirectory;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    public Task<PaymentInitiationResult> InitiateAsync(InitiatePaymentCommand command, CancellationToken cancellationToken) =>
        InitiateCoreAsync(command, cancellationToken, idempotencyRaceResolved: false);

    /// <summary>
    /// TB240-R3: <c>idempotencyRaceResolved</c> is true only when this call is the single re-resolution of a lost
    /// same-key insert race. Such a call may NEVER insert again, so a second conflict is a typed conflict, not a loop.
    /// </summary>
    private async Task<PaymentInitiationResult> InitiateCoreAsync(
        InitiatePaymentCommand command,
        CancellationToken cancellationToken,
        bool idempotencyRaceResolved)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var key = command.IdempotencyKey.Trim();
        var existing = await _db.Payments.FirstOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            await _actorAccess.EnsureActorCanSeeAsync(existing, command.ActorUserId, command.BuyerPartyId, cancellationToken);
            var priorAttempts = await _db.Attempts
                .Where(x => x.PaymentId == existing.PaymentId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            foreach (var loaded in priorAttempts)
            {
                existing.AttachLoadedAttempt(loaded);
            }

            _actorContext.ActorUserId = command.ActorUserId;
            // Fail closed BEFORE any replay, retry, provider call, or return: a reused key must describe
            // the very same payment operation. The requested provider is canonicalized through the gateway
            // registry so an identical-method replay (including the catalog default alias resolved upstream)
            // still matches the persisted concrete provider, while a cross-method or cross-checkout reuse is
            // rejected instead of silently replaying the previous payment or charging the previous provider.
            var requestedProviderCode = _gateways.Resolve(command.ProviderCode).ProviderCode;
            if (existing.CheckoutId != command.CheckoutId
                || !string.Equals(requestedProviderCode, existing.ProviderCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new ContractOperationException(PaymentErrorCodes.IdempotencyConflict);
            }

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
                var retryAttempt = existing.RecordInitiation(_ids.NewId(), retryInitiation.ProviderRequestReference, _clock.UtcNow);
                _timeoutAssigner.Assign(existing, _clock.UtcNow);
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
            ?? throw new ContractOperationException("payment.checkout.not_payable");
        if (payable.Mode != OrderPaymentMode.OnlinePurchase)
        {
            throw new ContractOperationException("payment.initiate.reservation_forbidden");
        }

        var pending = payable.SellerOrders.Where(x => x.PendingPayment).ToArray();
        if (pending.Length == 0)
        {
            throw AlreadySucceeded();
        }

        if (pending.Select(x => x.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1
            || !string.Equals(payable.Currency, pending[0].Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new ContractOperationException("payment.currency.mismatch");
        }

        var merchandise = pending.Sum(x => x.PayableAmount);
        var shipping = Math.Max(0m, payable.ShippingAmount);
        var gateway = _gateways.Resolve(command.ProviderCode);
        // تخصیص فروشنده فقط مبلغ کالا؛ ارسال هرگز به حساب فروشنده نمی‌رود.
        var allocationRows = pending
            .OrderBy(x => x.SellerOrderId)
            .Select(x => (PaymentAllocationTargetKind.SellerOrder, x.SellerOrderId, x.PayableAmount))
            .ToList();
        allocationRows.AddRange(ResolveShippingAllocationRows(payable.ShippingGroups, shipping));
        var amount = merchandise + allocationRows
            .Where(x => x.Item1 is PaymentAllocationTargetKind.StoreShipping
                or PaymentAllocationTargetKind.StoreShippingGroup)
            .Sum(x => x.Item3);

        var paymentId = _ids.NewId();
        var allocationRowsWithIds = allocationRows
            .Select(r => (r.Item1, r.Item2, r.Item3, _ids.NewId()))
            .ToArray();
        var payment = CustomerPayment.Open(
            paymentId,
            payable.CheckoutId,
            amount,
            payable.Currency,
            gateway.ProviderCode,
            key,
            allocationRowsWithIds,
            _clock.UtcNow);
        _actorContext.ActorUserId = command.ActorUserId;
        var initiation = await gateway.InitiateAsync(payment.PaymentId, payment.Amount, payment.Currency, cancellationToken);
        var attempt = payment.RecordInitiation(_ids.NewId(), initiation.ProviderRequestReference, _clock.UtcNow);
        _timeoutAssigner.Assign(payment, _clock.UtcNow);
        _db.Payments.Add(payment);
        _db.Allocations.AddRange(payment.Allocations);
        _db.Attempts.Add(attempt);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsIdempotencyKeyConflict(ex))
        {
            // TB240-R2/R3: TWO simultaneous requests with the SAME key both passed the lookup above. The unique
            // index on idempotency_key is the real arbiter, so the loser must never surface a raw
            // DbUpdateException as platform.unexpected and must never leave two payment side effects.
            // Detach exactly this attempt's graph, then resolve the winner: the caller that won the insert is
            // the ONLY owner of that intent.
            Detach(payment, attempt);

            // TB240-R3: the winner is described by the EXISTING same-key replay path, so operation identity
            // (checkout + canonical provider), the actor-access check, already-succeeded and failed-retry rules
            // are exactly the lawful replay rules — never a new "first row" policy invented for the race.
            var winnerVisible = await _db.Payments.AsNoTracking()
                .AnyAsync(x => x.IdempotencyKey == key, cancellationToken);
            if (!winnerVisible)
            {
                // The winner's row is not visible yet (its transaction is still open). Fail closed with the
                // typed conflict; an explicit later same-key retry replays the original payment.
                throw new ContractOperationException(PaymentErrorCodes.IdempotencyConflict);
            }

            if (idempotencyRaceResolved)
            {
                // One resolution attempt only: never loop on the same key.
                throw new ContractOperationException(PaymentErrorCodes.IdempotencyConflict);
            }

            return await InitiateCoreAsync(command, cancellationToken, idempotencyRaceResolved: true);
        }
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

    /// <summary>
    /// ردیف‌های تخصیص ارسال را قطعی می‌کند. سفارش چندگروهی: یک ردیف به‌ازای هر گروه پایدار با هدف
    /// <c>ShippingGroupId</c>؛ سفارش قدیمی تک‌روشی: همان باکت sentinel قبلی. ناسازگاری مبلغ/شناسه
    /// fail-closed است و هیچ‌گاه گروه تکراری یا گم‌شده بی‌صدا ساخته نمی‌شود.
    /// </summary>
    /// <summary>
    /// TB240-R2/R3: is this failure the EXISTING unique index on <c>payment.payments.idempotency_key</c>?
    /// Exact provider identity only (SQLSTATE 23505 + the real table and index name), so no other uniqueness
    /// violation anywhere in the module can ever be mistaken for an idempotent replay.
    /// </summary>
    private static bool IsIdempotencyKeyConflict(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is Npgsql.PostgresException { SqlState: "23505" } postgres)
            {
                return string.Equals(postgres.ConstraintName, PaymentIdempotencyIndexName, StringComparison.Ordinal)
                    && (postgres.TableName is null
                        || string.Equals(postgres.TableName, PaymentTableName, StringComparison.Ordinal));
            }
        }

        return false;
    }

    /// <summary>
    /// Detaches exactly this attempt's staged graphs so the shared context stays usable after the losing
    /// concurrent insert (the winner's row is then read fresh, never adopted into the tracker).
    /// </summary>
    private void Detach(CustomerPayment payment, PaymentAttempt attempt)
    {
        _db.Entry(attempt).State = EntityState.Detached;
        foreach (var allocation in payment.Allocations)
        {
            _db.Entry(allocation).State = EntityState.Detached;
        }

        _db.Entry(payment).State = EntityState.Detached;
    }

    private static IReadOnlyList<(PaymentAllocationTargetKind TargetKind, Guid TargetId, decimal Amount)>
        ResolveShippingAllocationRows(
            IReadOnlyList<PayableShippingGroupSnapshot>? groups,
            decimal shipping)
    {
        if (groups is null || groups.Count == 0)
        {
            return shipping > 0m
                ? [(PaymentAllocationTargetKind.StoreShipping, PaymentAllocation.StoreShippingTargetId, shipping)]
                : [];
        }

        var seen = new HashSet<Guid>();
        var rows = new List<(PaymentAllocationTargetKind, Guid, decimal)>(groups.Count);
        var total = 0m;
        foreach (var group in groups)
        {
            if (group.ShippingGroupId == Guid.Empty)
            {
                throw new ContractOperationException("payment.shipping_group.identity_required");
            }

            if (!seen.Add(group.ShippingGroupId))
            {
                throw new ContractOperationException("payment.shipping_group.duplicate");
            }

            if (group.Amount <= 0m)
            {
                throw new ContractOperationException("payment.shipping_group.amount_positive");
            }

            total += group.Amount;
            rows.Add((PaymentAllocationTargetKind.StoreShippingGroup, group.ShippingGroupId, group.Amount));
        }

        if (total != shipping)
        {
            // The aggregate payable and the per-group snapshot must agree exactly; otherwise the
            // shipping charge could be counted twice or lost.
            throw new ContractOperationException("payment.shipping_group.sum_mismatch");
        }

        return rows;
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> VerifyAsync(VerifyPaymentCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.PaymentId == command.PaymentId, cancellationToken)
            ?? throw new ContractOperationException(PaymentErrorCodes.Missing);
        var attempt = await _db.Attempts.SingleOrDefaultAsync(
            x => x.AttemptId == command.AttemptId && x.PaymentId == payment.PaymentId,
            cancellationToken)
            ?? throw new ContractOperationException("payment.attempt.not_found");
        if (attempt.ProviderRequestReference != command.ProviderRequestReference)
        {
            throw new ContractOperationException("payment.provider_reference.mismatch");
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

            payment.ApplyVerifiedFailure(attempt.AttemptId, verified.FailureCode, _clock.UtcNow);
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

        var firstSuccess = payment.ApplyVerifiedSuccess(attempt.AttemptId, verified.ProviderTransactionReference, _clock.UtcNow);
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

        await _actorAccess.EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
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

    internal static ContractOperationException AlreadySucceeded() =>
        new(PaymentErrorCodes.AlreadySucceeded);

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
            ?? throw new ContractOperationException(PaymentErrorCodes.Missing);
        await _actorAccess.EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new ContractOperationException("payment.method.not_manual");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new ContractOperationException("payment.manual.submit.invalid_state");
        }

        var duplicate = await _db.ProofAssets.AnyAsync(x => x.MediaAssetId == mediaAssetId, cancellationToken);
        if (duplicate)
        {
            var owned = await _db.ProofAssets.AnyAsync(
                x => x.MediaAssetId == mediaAssetId && x.PaymentId == paymentId,
                cancellationToken);
            if (!owned)
            {
                throw new ContractOperationException("payment.proof.foreign");
            }

            return;
        }

        _db.ProofAssets.Add(PaymentProofAsset.Attach(_ids.NewId(), paymentId, mediaAssetId, _clock.UtcNow));
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
            ?? throw new ContractOperationException(PaymentErrorCodes.Missing);
        await _actorAccess.EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
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
                throw new ContractOperationException("payment.proof.foreign");
            }
        }

        payment.SubmitManualEvidence(transferReference, proofMediaAssetId, _clock.UtcNow);
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
            ?? throw new ContractOperationException(PaymentErrorCodes.Missing);
        await _actorAccess.EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        if (_adminDirectory is null)
        {
            throw new InvalidOperationException("payment.admin_directory.unavailable");
        }

        await _adminDirectory().RestoreDepositAsync(paymentId, cancellationToken);
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
