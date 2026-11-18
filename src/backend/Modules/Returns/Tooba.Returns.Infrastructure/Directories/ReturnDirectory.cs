using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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

    // TB-272: ordinary creation lives in ReturnDirectory.Creation.cs (authority → intent → one serialized decision).

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
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);

        // TB-274-R1 (B): the owning seller scope is enforced HERE, inside the owning directory, before any decision,
        // receipt or money write — not only in the Application handler that internal adapters can bypass. A foreign
        // seller scope answers exactly like an unknown case.
        EnsureOwningSellerScope(request, command.SellerPartyId);

        // TB-277: approval is a DECISION. This method deliberately does NOT create a refund attempt, mark the case
        // processing, call the refund gateway/Wallet, restock stock, settle a physical phase or run a courier
        // obligation/branch closure. The first money execution is the separate StartFirstRefundAsync intent, so no
        // route that approves a return can move money as a side effect.
        // TB-279: the EFFECTIVE (append-only) decision is the authority, not only the legacy baseline row.
        var decisionState = await ReadDecisionStateAsync(command.ReturnRequestId, cancellationToken);

        if (string.Equals(
                decisionState.EffectiveDecision,
                ReturnDecisionRevision.RejectDecision,
                StringComparison.Ordinal))
        {
            // A durable REJECTION is the effective decision; it can never be approved afterwards, and no unrelated
            // missing-payment fault is reported for it. This is a read: it writes nothing.
            throw new ContractOperationException(ReturnsErrorCodes.AlreadyRejected);
        }

        if (string.Equals(
                decisionState.EffectiveDecision,
                ReturnDecisionRevision.ApproveDecision,
                StringComparison.Ordinal)
            && request.Status != ReturnRequestStatus.Requested)
        {
            // TB-277: an exact recorded approval replays its recorded state with ZERO effect — including when the
            // money later progressed, failed or completed. It never re-approves, never re-sends and never resumes.
            return await MapSnapshotAsync(request, cancellationToken);
        }

        // The case's FIXED payment binding is part of the lawful approval decision (the later first-refund intent
        // names no payment of its own); reading it moves no money.
        var payment = await ResolvePaymentAsync(request, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
        if (!string.Equals(payment.Status, "Succeeded", StringComparison.Ordinal))
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundAlreadyStarted);
        }

        // TB-274 / TB-279 (C): the reviewed facts that authorize the approval, read ONCE before the lock so an
        // unlawful approval is refused without opening a transaction. They are re-read under the lock below.
        var reviewed = await MapSnapshotAsync(request, cancellationToken);
        var reviewedResolution = EnsureReviewedMoneyBasis(request, reviewed);
        var reviewedVersion = reviewedResolution.Version;
        var reviewedAmount = reviewedResolution.ApprovedAmount;

        // TB-265-R2 / TB-279-R1 (F4): ONE short lock holds the reviewed decision, the case transition and the FROZEN
        // approved amount. The decision can therefore never commit while its status/amount wait for a later write.
        return await InLockedDecisionScopeAsync(request, async () =>
        {
            EnsureOwningSellerScope(request, command.SellerPartyId);

            // Never let a pre-lock tracked value overwrite a newer correction: the reviewed facts are re-read now.
            var locked = await MapSnapshotAsync(request, cancellationToken);
            var lockedResolution = EnsureReviewedMoneyBasis(request, locked);
            if (lockedResolution.Version != reviewedVersion || lockedResolution.ApprovedAmount != reviewedAmount)
            {
                throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
            }

            if (request.PaymentId is { } existingPayment && existingPayment != payment.PaymentId)
            {
                throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
            }

            var append = await AppendReviewedDecisionAsync(
                request,
                ReturnDecisionKind.Approve,
                command.ActorUserId,
                reason: null,
                command.ExpectedDecisionVersion,
                cancellationToken);

            // A NEW decision, or an exact replay of THIS decision on a case that is still undecided (a concurrent
            // duplicate/winner recorded the same approval). Both move the case exactly once.
            if (append == DecisionAppend.Recorded || request.Status == ReturnRequestStatus.Requested)
            {
                // Only the CASE moves to Approved. There is no RefundProcessing transition and no attempt yet: the
                // operator must deliberately start the money through the explicit first-refund action. The reviewed
                // amount is frozen here, before any money exists, so the first refund can never name another number.
                request.Approve(payment.PaymentId, _clock.UtcNow, command.RefundDestination, reviewedAmount);
                await _db.SaveChangesAsync(cancellationToken);
                _telemetry.RecordApproved();
            }

            return await MapSnapshotAsync(request, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// TB-274 / TB-279-R1 (F2): the ONE money/decision basis of a NEW ordinary approval.
    /// <para>
    /// The reviewed resolution — not "every requested unit arrived" — is the basis: an accepted received portion plus
    /// a rejected remainder that never arrived is a lawful COMPLETE plan, while a case with no reviewed split still
    /// needs its whole claim physically received. Incoherent stored evidence is always refused, and the accepted
    /// portions must already be covered by their own received condition (that bound lives in the resolution policy).
    /// </para>
    /// </summary>
    private static ReturnResolutionSnapshot EnsureReviewedMoneyBasis(ReturnRequest request, ReturnSnapshot snapshot)
    {
        var resolution = ReturnResolutionPolicy.Resolve(snapshot);
        if (request.Origin != ReturnOrigin.CustomerDelivered)
        {
            return resolution;
        }

        if (!ReturnResolutionPolicy.TryDescribeCompleteCoverage(
                snapshot,
                out resolution,
                out var blockerCode,
                incoherentCode: ReturnsErrorCodes.ReceivingIncomplete,
                incompleteReceiptCode: ReturnsErrorCodes.ReceivingIncomplete))
        {
            throw new ContractOperationException(blockerCode);
        }

        return resolution;
    }

    /// <inheritdoc />
    public async Task<ReturnSnapshot> RejectAsync(RejectReturnCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        // TB-274-R1 (B/D): the owning scope AND the case are settled BEFORE the durable decision is recorded, so a
        // foreign seller scope can never write a winning decision or a rejection reason for another seller's case.
        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
        EnsureOwningSellerScope(request, command.SellerPartyId);

        // TB-265-R2 / TB-274 / TB-279-R1 (F4): the SAME durable decision authority as approval, now carrying the
        // reviewed rejection reason in the same row, the case transition and the released per-delivery capacity in ONE
        // locked transaction. A case whose approval already won can never be rejected here, so its source
        // claim/hold/money are never released by a losing concurrent request; a changed reason is a typed conflict.
        var snapshot = await InLockedDecisionScopeAsync(request, async () =>
        {
            EnsureOwningSellerScope(request, command.SellerPartyId);

            var append = await AppendReviewedDecisionAsync(
                request,
                ReturnDecisionKind.Reject,
                command.ActorUserId,
                command.Reason,
                command.ExpectedDecisionVersion,
                cancellationToken);

            // A NEW decision, or an exact replay of THIS decision on a case that is still undecided (a concurrent
            // duplicate/winner recorded the same rejection). Both move the case exactly once.
            if (append == DecisionAppend.Recorded || request.Status == ReturnRequestStatus.Requested)
            {
                // TB-265-R1: only a case that was never approved can be rejected. A case already refunding/approved
                // keeps its obligation and its claim (already a typed refusal above); releasing its branch here would
                // free capacity a live case still owns and would let the same units fund a second branch.
                if (request.Status != ReturnRequestStatus.Requested)
                {
                    throw new ContractOperationException("return.reject.invalid_state");
                }

                request.Reject(_clock.UtcNow);

                // TB-279 (D) / TB-279-R1 (F3): a rejected claim returns its NEVER-RECEIVED remainder to the pool, while
                // the goods that physically arrived stay claimed (under this same seller-order lock) until their real
                // handback is durable. Releasing everything would let a sibling case claim units the seller still holds.
                await ReleaseRejectedDeliveriesAsync(request, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                _telemetry.RecordRejected();
            }

            return await MapSnapshotAsync(request, cancellationToken);
        }, cancellationToken);

        // Releasing the source branch AFTER the durable reject is the only lawful release of this claim. It talks to
        // the Fulfillment contract, so it deliberately runs OUTSIDE the decision transaction.
        if (request.Status is ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled)
        {
            await ReleaseCourierOriginClaimAsync(
                request, request.Status.ToString(), command.ActorUserId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        return snapshot;
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
            // TB-276 (B): this is deliberately NOT a failed-refund retry — the case owns no failed attempt to review —
            // and the branch sends no money, so the required reviewed failure id only governs the money path below.
            await SettlePhysicalDispositionAsync(request, cancellationToken);
            await MarkRefundReleasedWhenFullyDisposedAsync(request, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        // TB-274 / TB-279-R1 (F2): a retry mints a NEW refund attempt over a REVIEWED resolution, not over the whole
        // original claim. A coherent receipt plus a complete reviewed plan is the money basis; "every requested unit
        // arrived" is not required, because an accepted received portion plus a rejected unreceived remainder is a
        // lawful complete plan. A courier case owns its Fulfillment receipt and is unaffected.
        if (request.Origin != ReturnOrigin.CourierOrigin)
        {
            var retrySnapshot = await MapSnapshotAsync(request, cancellationToken);
            if (retrySnapshot.Receiving is { IsCoherent: false })
            {
                throw new ContractOperationException(ReturnsErrorCodes.RecoveryEvidenceConflict);
            }

            if (retrySnapshot.Receiving is not { } receiving
                || !receiving.Lines.Any(x => x.Received > 0m))
            {
                // No recorded physical occurrence at all: the same receiving authority gates this path.
                throw new ContractOperationException(ReturnsErrorCodes.ReceivingIncomplete);
            }

            if (retrySnapshot.Resolution is not { IsComplete: true })
            {
                throw new ContractOperationException(
                    retrySnapshot.Resolution?.BlockerCode ?? ReturnsErrorCodes.ResolutionRequired);
            }
        }

        // TB-276 (B): the reviewed DEFINITE failure is REQUIRED — every reachable retry boundary must name the stored
        // attempt the operator reviewed, because "the current failure" is a guess that would let an unreviewed caller
        // retry whichever row happened to be newest. The child identity is derived from that exact stored failure, so a
        // duplicate/lost-response POST resolves to the SAME child instead of minting a count-based one.
        var failed = RequireReviewedFailedAttempt(request, command.ExpectedFailedAttemptId);
        var paymentId = request.PaymentId
            ?? throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
        var childAttemptId = DeriveRetryAttemptId(request.ReturnRequestId, failed.RefundAttemptId);
        var childKey = DeriveRetryIdempotencyKey(failed.RefundAttemptId);

        var alreadyMinted = request.RefundAttempts
            .FirstOrDefault(x => x.RefundAttemptId == childAttemptId);
        if (alreadyMinted is not null)
        {
            // TB-276 (B): a replay of the PARENT intent answers its deterministic CHILD — whatever that child's stored
            // outcome is (pending, succeeded or failed). Nothing is re-minted, no new key is minted, and a terminal
            // child is never re-sent; the answer is the child's own durable row.
            EnsureRetryAttemptIdentity(request, alreadyMinted, paymentId, childKey);
            await ExecuteRefundAsync(request, alreadyMinted, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        // TB-279-R1 (F1/F4): the reviewed decision revision is REQUIRED and the owning payment facts are read before any
        // transaction, so this NEW child obeys the SAME reservation protocol as the first refund. Both are checked
        // AFTER the reviewed failure itself, so a case without one reviewable failure answers that truthful refusal.
        var expectedDecisionVersion = RequireReviewedDecisionVersion(command.ExpectedDecisionVersion);
        var authority = await RequirePaymentAuthorityAsync(request, cancellationToken);

        var reservation = await PersistNewAttemptUnderReservationAsync(
            request,
            childAttemptId,
            childKey,
            authority,
            expectedDecisionVersion,
            failed.RefundAttemptId,
            cancellationToken);
        if (reservation == AttemptReservation.AlreadyAnswered)
        {
            // A concurrent duplicate of the SAME reviewed intent won the deterministic child identity, or a correction
            // moved the case. This request sends nothing.
            return await GetAsync(request.ReturnRequestId, cancellationToken)
                ?? await MapSnapshotAsync(request, cancellationToken);
        }

        var attempt = request.RefundAttempts.Single(x => x.RefundAttemptId == childAttemptId);
        _telemetry.RecordRetry();

        await ExecuteRefundAsync(request, attempt, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }

    /// <summary>
    /// TB-276 (B): the EXACT reviewed failure. The reviewed attempt id is REQUIRED — an absent or empty value is a
    /// typed refusal, never "the stored failure". The named row must be a stored FAILED attempt positively bound to
    /// this case; anything else is a refusal and mints nothing.
    /// </summary>
    private static RefundAttempt RequireReviewedFailedAttempt(ReturnRequest request, Guid? expectedFailedAttemptId)
    {
        if (expectedFailedAttemptId is not { } expected || expected == Guid.Empty)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);
        }

        var reviewed = request.RefundAttempts
            .FirstOrDefault(x => x.RefundAttemptId == expected)
            ?? throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);

        if (reviewed.Status != RefundAttemptStatus.Failed || !IsBoundToRequest(request, reviewed))
        {
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);
        }

        return reviewed;
    }

    /// <summary>
    /// TB-275 (C2) / TB-276 (B) / TB-279-R1 (F1): minting a NEW child may only answer the case's CURRENT reviewable
    /// failure — the same single positively bound, not-yet-retried failure the read publishes — re-read AFTER the
    /// seller/case locks, so an ambiguous, superseded or already-retried parent mints nothing.
    /// </summary>
    private static void EnsureReviewedFailureIsStillCurrent(ReturnRequest request, Guid reviewedFailedAttemptId)
    {
        var reviewable = request.RefundAttempts
            .Where(x => x.Status == RefundAttemptStatus.Failed && IsBoundToRequest(request, x))
            .Where(x => request.RefundAttempts.All(child =>
                child.RefundAttemptId != ReturnRetryIdentity.DeriveAttemptId(request.ReturnRequestId, x.RefundAttemptId)))
            .ToArray();
        if (reviewable.Length != 1 || reviewable[0].RefundAttemptId != reviewedFailedAttemptId)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptUnbound);
        }
    }

    /// <summary>
    /// TB-275 (C2): the child attempt must answer for THIS case with the reviewed payment, the exact refund amount and
    /// currency and the deterministic key derived from the reviewed failure. A row that does not is never re-sent.
    /// </summary>
    private static void EnsureRetryAttemptIdentity(
        ReturnRequest request,
        RefundAttempt attempt,
        Guid paymentId,
        string expectedKey)
    {
        if (attempt.PaymentId != paymentId
            || attempt.Amount != request.ReviewableMoney
            || !string.Equals(attempt.Currency.Trim(), request.Currency.Trim(), StringComparison.Ordinal)
            || !string.Equals(attempt.IdempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);
        }
    }

    /// <summary>The same binding rule the recovery read uses, over the loaded aggregate.</summary>
    private static bool IsBoundToRequest(ReturnRequest request, RefundAttempt attempt) =>
        attempt.RefundAttemptId != Guid.Empty
        && !string.IsNullOrWhiteSpace(attempt.IdempotencyKey)
        && request.PaymentId is { } paymentId
        && paymentId != Guid.Empty
        && attempt.PaymentId == paymentId
        && attempt.Amount == request.ReviewableMoney
        && string.Equals(attempt.Currency.Trim(), request.Currency.Trim(), StringComparison.Ordinal);

    /// <summary>
    /// TB-275 (C2): the deterministic child attempt identity of ONE reviewed failure. Deriving it (instead of a
    /// count-based key) is what makes a duplicate or lost-response POST resolve to the same intent: the primary key
    /// itself arbitrates, and no second table or counter is introduced.
    /// </summary>
    private static Guid DeriveRetryAttemptId(Guid returnRequestId, Guid failedAttemptId) =>
        ReturnRetryIdentity.DeriveAttemptId(returnRequestId, failedAttemptId);

    /// <summary>TB-275 (C2): the deterministic provider-facing key of that same child attempt.</summary>
    private static string DeriveRetryIdempotencyKey(Guid failedAttemptId) =>
        ReturnRetryIdentity.DeriveIdempotencyKey(failedAttemptId);

    /// <summary>The exact provider collision shape that means "this reviewed child already exists".</summary>
    private static bool IsRetryAttemptKeyConflict(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException { SqlState: "23505" } postgres
        && string.Equals(postgres.TableName, "refund_attempts", StringComparison.Ordinal)
        && string.Equals(postgres.SchemaName, ReturnsDbContext.Schema, StringComparison.Ordinal)
        && (string.Equals(postgres.ConstraintName, "pk_refund_attempts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(postgres.ConstraintName, "ix_refund_attempts_IdempotencyKey", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// TB-273: what one refund execution actually proved. <c>Unavailable</c> is a definite "nothing was sent";
    /// <c>Unresolved</c> is unknown; <c>Resolved</c> means a durable terminal outcome now exists.
    /// </summary>
    private enum RefundExecutionOutcome
    {
        Resolved = 0,
        Unresolved = 1,
        Unavailable = 2,
    }

    /// <summary>
    /// Executes ONE refund attempt. A durable terminal attempt is never re-sent, and the external call is never made
    /// inside a database transaction or under a row lock (TB-273 D).
    /// <para>
    /// TB-273: the outcome reports whether the owning adapter was EXPLICITLY unconfigured (so nothing was even sent)
    /// separately from an answer that is genuinely unknown. Only the caller decides what a definite "cannot send"
    /// means for its own intent; the shared approve/retry paths keep their existing pending behaviour.
    /// </para>
    /// </summary>
    private async Task<RefundExecutionOutcome> ExecuteRefundAsync(
        ReturnRequest request,
        RefundAttempt attempt,
        CancellationToken cancellationToken)
    {
        // TB-265-R2: an attempt that ALREADY recorded its money outcome is a durable answer, never a new money call.
        // This is what makes a concurrent/late resume of the same case safe with the same attempt identity.
        if (attempt.Status != RefundAttemptStatus.Pending)
        {
            return RefundExecutionOutcome.Resolved;
        }

        // TB-273-R1 B: the SHARED external-call boundary. No gateway/Wallet call may be made while a caller-owned,
        // enlisted or ambient transaction is open, because the owning outcome write could then be joined to it
        // without being committed before the physical phase runs. Every direct caller of this helper is covered,
        // so a future path cannot silently bypass the precondition.
        RefuseConstrainedTransaction();

        var now = _clock.UtcNow;

        if (request.RefundDestination == RefundDestination.Wallet)
        {
            // انتخاب معماری: اعتبار کیف پول جایگزین PSP gateway است؛ gateway صدا زده نمی‌شود تا double-credit نشود.
            // The wallet ledger key is the CASE key, so a repeated same-intent resume can only ever credit once.
            try
            {
                await _wallets.CreditRefundAsync(
                    request.RequestedByUserId,
                    attempt.Amount,
                    attempt.Currency,
                    request.ReturnRequestId,
                    $"wallet-refund-credit:{request.ReturnRequestId:D}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // TB-265-R1/TB-273: an UNKNOWN/transport outcome is not a failure. The durable attempt and its key are
                // retained and the case stays RefundProcessing so the SAME attempt can be reconciled.
                _logger.LogWarning(ex, "Wallet refund outcome unknown for return {ReturnRequestId}", request.ReturnRequestId);
                return RefundExecutionOutcome.Unresolved;
            }

            await ApplyRefundSuccessAsync(
                request,
                attempt,
                $"wallet-refund:{request.ReturnRequestId:D}",
                now,
                cancellationToken);
            return RefundExecutionOutcome.Resolved;
        }

        GatewayRefundResult result;
        try
        {
            result = await _refundGateway.RefundAsync(
                attempt.PaymentId,
                attempt.Amount,
                attempt.Currency,
                attempt.IdempotencyKey,
                cancellationToken);
        }
        catch (ContractOperationException ex) when (string.Equals(
            ex.Code, UnconfiguredRefundGatewayCode, StringComparison.Ordinal))
        {
            // TB-273 B: an EXPLICITLY unconfigured adapter proves no send happened, so this is not unknown money and
            // must never be promoted into a terminal provider rejection. The pending attempt and its key are kept.
            _logger.LogWarning(ex, "Refund adapter unconfigured for return {ReturnRequestId}", request.ReturnRequestId);
            return RefundExecutionOutcome.Unavailable;
        }
        catch (Exception ex)
        {
            // An unknown answer after a possible send stays unknown with the SAME attempt — never a synthetic
            // provider reference, never a definite failure, and never a second idempotency key.
            _logger.LogWarning(ex, "Refund outcome unknown for return {ReturnRequestId}", request.ReturnRequestId);
            return RefundExecutionOutcome.Unresolved;
        }

        if (result.Succeeded)
        {
            // TB-273-R1 C: the authoritative SUCCESS boolean is preserved truthfully even when the adapter supplies no
            // reference. A synthetic PSP id is never invented, and a missing reference alone is never read as failure.
            await ApplyRefundSuccessAsync(
                request,
                attempt,
                result.ProviderReference,
                now,
                cancellationToken);
            return RefundExecutionOutcome.Resolved;
        }

        // A proven terminal refusal is a real failed money attempt.
        await RecordRefundOutcomeAsync(
            request, attempt, succeeded: false, providerReference: null,
            failureCode: result.FailureCode ?? "REFUND_FAILED", now, cancellationToken);
        return RefundExecutionOutcome.Resolved;
    }

    /// <summary>
    /// TB-273: the owning Payment contract's exact "no refund adapter is configured" code. Classified by typed code
    /// only — never by message text — and only to distinguish "definitely not sent" from "unknown".
    /// </summary>
    private const string UnconfiguredRefundGatewayCode = "payment.refund.gateway.unconfigured";

    private async Task ApplyRefundSuccessAsync(
        ReturnRequest request,
        RefundAttempt attempt,
        string? providerReference,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!await RecordRefundOutcomeAsync(
                request, attempt, succeeded: true, providerReference, failureCode: null, now, cancellationToken))
        {
            // A concurrent writer already recorded a terminal outcome: nothing is downgraded and no second event is
            // raised. The physical phase stays reachable through the explicit physical intent.
            return;
        }

        await TrySettlePhysicalDispositionAsync(request, cancellationToken);
    }

    /// <summary>
    /// TB-273 D: applies a money answer under a SHORT owning coordination that re-reads the exact durable attempt.
    /// Returns <c>false</c> when a terminal outcome was already durable (this answer is stale and is not applied).
    /// A local persistence failure propagates: an external success whose save did not land is UNCERTAIN, never an
    /// undurable success.
    /// </summary>
    private async Task<bool> RecordRefundOutcomeAsync(
        ReturnRequest request,
        RefundAttempt attempt,
        bool succeeded,
        string? providerReference,
        string? failureCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // TB-273-R1 B: an outcome may only be recorded by a coordination this owner can commit itself. A caller
        // transaction would make "durable before the physical phase" false, so it is refused instead of joined.
        RefuseConstrainedTransaction();

        // The coordination is a real row lock on the relational production store and a no-transaction read on a
        // provider that cannot serialize; it never spans the gateway/Wallet/Inventory call above.
        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        var transaction = ownsTransaction ? await _db.Database.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            if (relational)
            {
                await _db.Database.ExecuteSqlAsync(
                    $"SELECT 1 FROM returns.refund_attempts WHERE refund_attempt_id = {attempt.RefundAttemptId} FOR UPDATE",
                    cancellationToken);
            }

            // Re-read BEFORE applying: a stale success/failure can never downgrade a terminal row, resurrect an
            // attempt another writer settled, or produce a second outcome event.
            await _db.Entry(attempt).ReloadAsync(cancellationToken);
            await _db.Entry(request).ReloadAsync(cancellationToken);

            if (attempt.Status != RefundAttemptStatus.Pending)
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return false;
            }

            if (succeeded)
            {
                attempt.MarkSucceeded(providerReference, now);
                request.MarkRefundSucceeded(now);
                _telemetry.RecordRefundSucceeded();
            }
            else
            {
                attempt.MarkFailed(failureCode!, now);
                request.MarkRefundFailed(now);
                _telemetry.RecordRefundFailed();
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return true;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// TB-265-R1 / TB-273-R1: the money phase is only recorded as finished when every received line has a DURABLE
    /// disposition and, for a courier-origin case, the owning obligation/stock closure markers are durable too.
    /// <para>
    /// The decision is taken from a durable re-read, never from tracked/in-memory flags: a lost item or marker save
    /// must not be promoted into a completion by a retry in the same request scope. If the marker save itself fails,
    /// the tracked case is restored from the store before the failure propagates, so no unsaved
    /// <c>RefundReleased</c> survives to authorize a false completion.
    /// </para>
    /// </summary>
    private async Task MarkRefundReleasedWhenFullyDisposedAsync(
        ReturnRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Origin != ReturnOrigin.CourierOrigin)
        {
            // Legacy ordinary cases carry no owning closure marker to finalize.
            return;
        }

        var durableDispositions = await _db.ReturnItems.AsNoTracking()
            .Where(x => x.ReturnRequestId == request.ReturnRequestId)
            .Select(x => x.Disposition)
            .ToListAsync(cancellationToken);
        if (durableDispositions.Count == 0 || durableDispositions.Any(x => x == ReturnItemDisposition.None))
        {
            return;
        }

        var durableClosure = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.ReturnRequestId == request.ReturnRequestId)
            .Select(x => new { x.ClosurePhase, x.ObligationClosedAt, x.StockSettledAt })
            .SingleAsync(cancellationToken);
        if (durableClosure.ClosurePhase >= CourierOriginClosurePhase.RefundReleased
            || durableClosure.ObligationClosedAt is null
            || durableClosure.StockSettledAt is null
            || durableClosure.ClosurePhase < CourierOriginClosurePhase.StockSettled)
        {
            // Either the final marker is already durable (nothing to do) or an earlier owning phase is still missing.
            return;
        }

        var entry = _db.Entry(request);
        request.MarkRefundReleased(_clock.UtcNow);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await RestoreTrackedAsync(entry, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// TB-273-R1 B / TB-273-R2 C: refuses an effectful step while a caller-owned, ENLISTED or ambient transaction is
    /// open. It never commits/rolls back/disposes the caller's transaction and never touches its entities.
    /// <para>
    /// An explicit <see cref="System.Transactions.CommittableTransaction"/> enlisted on this context is NOT visible as
    /// <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.CurrentTransaction"/> and leaves
    /// <see cref="System.Transactions.Transaction.Current"/> null, so EF's own owning transaction manager
    /// (<see cref="RelationalConnection.EnlistedTransaction"/>) is inspected as well. Only the installed provider's
    /// supported state is read — no reflection, no name guessing, no second transaction framework.
    /// </para>
    /// </summary>
    private void RefuseConstrainedTransaction()
    {
        if (_db.Database.CurrentTransaction is not null
            || System.Transactions.Transaction.Current is not null
            || HasEnlistedProviderTransaction())
        {
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryTransactionOwned);
        }
    }

    /// <summary>
    /// TB-273-R2 C: the provider-enlisted transaction state, read from the context's own relational transaction
    /// manager. A non-relational (controlled unit-test) provider owns no enlistment, so this is a strict no-op there.
    /// </summary>
    private bool HasEnlistedProviderTransaction() =>
        _db.Database.IsRelational()
        && _db.Database.GetService<IRelationalConnection>() is RelationalConnection { EnlistedTransaction: not null };

    /// <summary>
    /// TB-273-R1 C: restores ONLY the affected tracked entry after a failed local save, so an unsaved in-memory flag
    /// can never be read as a durable fact. There is deliberately no global <c>ChangeTracker.Clear()</c>.
    /// </summary>
    private static async Task RestoreTrackedAsync<TEntity>(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> entry,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        try
        {
            await entry.ReloadAsync(cancellationToken);
        }
        catch
        {
            // The store could not be re-read: detaching is still strictly safer than keeping the unsaved value.
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// TB-265-R1: the physical disposition is a separate owner phase from money. Its failure can never change a
    /// successful refund into a failed one; the unfinished item stays explicitly pending for a disposition-only retry.
    /// </summary>
    private async Task TrySettlePhysicalDispositionAsync(
        ReturnRequest request,
        CancellationToken cancellationToken)
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

    /// <summary>
    /// TB-274-R1 (B): the stored case must belong to the acting seller scope before ANY decision or physical write,
    /// inside the owning directory. A foreign scope answers exactly like a missing case (no leak, no cross-seller write).
    /// </summary>
    private static void EnsureOwningSellerScope(ReturnRequest request, Guid? sellerPartyId)
    {
        if (sellerPartyId is { } scope && request.SellerPartyId != scope)
        {
            throw new ContractOperationException(ReturnsErrorCodes.Missing);
        }
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

        // TB-273-R1: the durable approve/reject decision is part of the SAME stored-fact description the recovery
        // command decides on. Reading it here keeps one evidence rule for reads and actions. TB-274 adds the reason
        // recorded with that winning decision and the receiving/inspection summary of the physical receipt.
        // TB-279: the APPEND-ONLY revision history is read beside it, so the effective decision/version and every
        // appended decision or correction are part of the same read the actions use.
        var decisionState = await ReadDecisionStateAsync(request.ReturnRequestId, cancellationToken);
        var storedLines = await _db.ReturnItemResolutions.AsNoTracking()
            .Where(x => x.ReturnRequestId == request.ReturnRequestId)
            .ToListAsync(cancellationToken);
        var custody = await _db.ReturnItemCustodies.AsNoTracking()
            .Where(x => x.ReturnRequestId == request.ReturnRequestId)
            .OrderBy(x => x.RecordedAt)
            .ToListAsync(cancellationToken);

        // TB-275: the receiving summary and the recorded occurrences come from ONE read of the same stored rows, so a
        // list of receipts can never disagree with the coverage it produced.
        ReturnReceivingSummary? receiving = null;
        IReadOnlyList<ReturnReceiptOccurrenceSnapshot> receipts = Array.Empty<ReturnReceiptOccurrenceSnapshot>();
        if (request.Origin != ReturnOrigin.CourierOrigin)
        {
            var evidence = await ReadReceiptEvidenceAsync(request.ReturnRequestId, cancellationToken);
            receiving = evidence.Summary;
            receipts = evidence.Occurrences;
        }
        var snapshot = new ReturnSnapshot(
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
                x.SourceReceiptId,
                // TB-274-R1: the exact inspected usable quantity that was restocked, never the requested quantity — a
                // mixed line therefore never reads as "the whole amount went back to saleable stock".
                SaleableRestockedQuantity: x.UsableRestockedQuantity
                    ?? (receiving is not null && x.Disposition == ReturnItemDisposition.SaleableRestock
                        ? receiving.UsableOf(x.ReturnItemId)
                        : null),
                // TB-279-R1 (F2): the durable per-portion facts of this line. A row recorded before those columns
                // existed keeps proving its ONE inspected portion through its immutable disposition; a pre-R1 row can
                // therefore never claim a mixed completion.
                UsableRestockedQuantity: x.UsableRestockedQuantity
                    ?? (receiving is not null && x.Disposition == ReturnItemDisposition.SaleableRestock
                        ? receiving.UsableOf(x.ReturnItemId)
                        : null),
                DamagedRecordedQuantity: x.DamagedRecordedQuantity
                    ?? (receiving is not null && x.Disposition == ReturnItemDisposition.DamagedNonSaleable
                        ? receiving.DamagedOf(x.ReturnItemId)
                        : null))).ToArray(),
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
            request.ReleasedHeldQuantity,
            RecordedDecision: decisionState.BaselineDecision,
            Receiving: receiving,
            DecisionReason: decisionState.BaselineReason,
            Receipts: receipts,
            EffectiveDecision: decisionState.EffectiveDecision,
            DecisionVersion: decisionState.EffectiveVersion,
            DecisionHistory: decisionState.ToSnapshots(),
            ApprovedAmount: request.ApprovedAmount,
            Custody: custody.Select(x => new ReturnCustodySnapshot(
                x.ReturnCustodyId,
                x.ReturnItemId,
                x.Quantity,
                string.Equals(x.Condition, nameof(ReturnItemCondition.Damaged), StringComparison.Ordinal)
                    ? ReturnItemCondition.Damaged
                    : ReturnItemCondition.Usable,
                x.RecipientUserId,
                x.ProofReference,
                x.RecordedAt,
                x.ProofKind)).ToArray());

        // TB-279: the reviewed resolution plan and the approved money it can lawfully name. Both are pure functions of
        // stored facts, so a read and a money action can never disagree about the amount.
        var resolutionVersion = storedLines.Count == 0 ? 0 : storedLines.Max(x => x.Version);
        // TB-279-R1 (F3): a case that was rejected/cancelled with goods physically in hand has no reviewed partial
        // plan, yet it still owes every received unit back to the owning customer. The honest plan of that case accepts
        // nothing, names the received portions per condition as owed custody, and is therefore a COMPLETE physical
        // basis for the custody command — never a false "nothing to do" closure.
        var resolution = storedLines.Count == 0
                && request.Status is ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled
            ? ReturnResolutionPolicy.DescribeRejectedCustody(snapshot.Items, receiving)
            : ReturnResolutionPolicy.Describe(
            request.Origin,
            snapshot.Items,
            receiving,
            storedLines
                .Where(x => x.Version == resolutionVersion)
                .Select(x => new ReturnResolutionLineSnapshot(
                    x.ReturnItemId,
                    snapshot.Items.FirstOrDefault(item => item.ReturnItemId == x.ReturnItemId)?.Quantity ?? 0m,
                    receiving?.Lines.FirstOrDefault(line => line.ReturnItemId == x.ReturnItemId)?.Received ?? 0m,
                    receiving?.Lines.FirstOrDefault(line => line.ReturnItemId == x.ReturnItemId)?.Usable ?? 0m,
                    receiving?.Lines.FirstOrDefault(line => line.ReturnItemId == x.ReturnItemId)?.Damaged ?? 0m,
                    // TB-279-R1 (F2/F3): the explicit per-condition portions the operator reviewed. A pre-R1 row has
                    // NULLs here on purpose: it is answered as an incomplete plan, never as a usable-first guess.
                    AcceptedUsable: x.AcceptedUsableQuantity ?? 0m,
                    AcceptedDamaged: x.AcceptedDamagedQuantity ?? 0m,
                    RejectedReceivedUsable: x.RejectedReceivedUsableQuantity ?? 0m,
                    RejectedReceivedDamaged: x.RejectedReceivedDamagedQuantity ?? 0m,
                    RejectedUnreceived: x.RejectedUnreceivedQuantity ?? 0m,
                    x.Reason,
                    x.Version,
                    PortionsKnown: x.AcceptedUsableQuantity is not null
                        && x.AcceptedDamagedQuantity is not null
                        && x.RejectedReceivedUsableQuantity is not null
                        && x.RejectedReceivedDamagedQuantity is not null
                        && x.RejectedUnreceivedQuantity is not null))
                .ToArray(),
            resolutionVersion,
            request.RefundAmount);
        var withResolution = snapshot with { Resolution = resolution };
        var withDecision = withResolution with
        {
            // TB-279: whether an audited correction is available NOW, and the exact typed reason when it is not.
            Correction = ReturnCorrectionPolicy.Describe(withResolution),
        };

        // TB-273: every read (admin/customer/seller/detail) carries the SAME stored-fact recovery description the
        // recovery command decides on, so a read and an action can never disagree.
        // TB-275: the work stage/actions are derived from that SAME description, so the queue bucket, the detail
        // workspace and the executable actions share one authority.
        var described = withDecision with { Recovery = ReturnRecoveryPolicy.Describe(withDecision) };
        return described with { Work = ReturnWorkPolicy.Describe(described) };
    }
}
