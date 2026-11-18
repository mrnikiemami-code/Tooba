using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-279-R1 (F1): the ONE shared Returns payment-reservation protocol of EVERY new money attempt — the FIRST refund of
/// an approved case and the NEW child of one reviewed definite failure.
/// <para>
/// Before this authority existed, the first attempt called the owning payment read INSIDE its case/payment locks and
/// skipped the budget ceiling when that second read answered nothing, while a reviewed retry minted its child with no
/// reservation at all. Either could therefore send money another Returns case had already reserved.
/// </para>
/// <para>
/// The protocol is deliberately short and ordered:
/// <list type="number">
/// <item><description>the owning payment facts are read BEFORE any database transaction/lock (no foreign port call is
/// ever made under a Returns lock);</description></item>
/// <item><description>the seller-order serialization, then the case row lock, are taken in that documented order
/// (<c>seller order → case → payment budget</c>);</description></item>
/// <item><description>the current decision revision, the frozen approved amount and the payment binding are re-read
/// AFTER the lock;</description></item>
/// <item><description>the payment's shared budget counts EVERY Pending/Succeeded Returns obligation of that payment —
/// not only this seller's or this case's — under its own stable advisory key;</description></item>
/// <item><description>the case processing state + the attempt row are persisted and COMMITTED before the external
/// port runs.</description></item>
/// </list>
/// </para>
/// <para>
/// A definite failure releases only its own reservation (the row becomes <c>Failed</c> and stops counting); an unknown
/// outcome keeps it (<c>Pending</c>), and a success remains counted (<c>Succeeded</c>). The scope is Returns-mediated
/// obligations only: card refunds executed outside Returns need the owning Payment authority and are not covered here.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <summary>
    /// The positively bound owning payment facts of one case's money step. It is acquired outside every transaction and
    /// lock and then carried into the short persistence step, so no foreign module read happens under a lock.
    /// </summary>
    private sealed record PaymentAuthority(Guid PaymentId, Guid CheckoutId, string Currency, decimal Amount);

    /// <summary>
    /// Reads and positively binds the owning payment of a case. Every mismatch fails CLOSED with its own typed code:
    /// a missing payment is never "budget unknown, continue", a foreign checkout/currency is never assumed equal, and a
    /// non-successful payment can never fund a refund.
    /// </summary>
    private async Task<PaymentAuthority> RequirePaymentAuthorityAsync(
        ReturnRequest request,
        CancellationToken cancellationToken)
    {
        var payment = request.PaymentId is { } bound && bound != Guid.Empty
            ? await _payments.GetAsync(bound, request.RequestedByUserId, null, cancellationToken)
            : await _payments.GetLatestForCheckoutAsync(request.CheckoutId, request.RequestedByUserId, null, cancellationToken);

        if (payment is null || payment.PaymentId == Guid.Empty)
        {
            // No owning payment authority answered: the ceiling can never be skipped, so nothing is reserved or sent.
            throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
        }

        if (request.PaymentId is { } expected && expected != Guid.Empty && payment.PaymentId != expected)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundBudgetBindingInvalid);
        }

        if (payment.CheckoutId != request.CheckoutId
            || !string.Equals(payment.Currency.Trim(), request.Currency.Trim(), StringComparison.Ordinal))
        {
            // The budget belongs to ONE checkout and ONE currency; a different one is a different money authority.
            throw new ContractOperationException(ReturnsErrorCodes.RefundBudgetBindingInvalid);
        }

        if (!string.Equals(payment.Status, "Succeeded", StringComparison.Ordinal))
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundAlreadyStarted);
        }

        var amount = request.ReviewableMoney;
        if (amount < 0m || amount > payment.Amount)
        {
            // A reviewed amount can never exceed the owning payment, and a negative one is not money.
            throw new ContractOperationException(ReturnsErrorCodes.RefundBudgetExceeded);
        }

        return new PaymentAuthority(payment.PaymentId, payment.CheckoutId, payment.Currency.Trim(), payment.Amount);
    }

    /// <summary>What the shared reservation step did with a NEW attempt request.</summary>
    private enum AttemptReservation
    {
        /// <summary>The attempt + reservation are durably committed; the caller may run the external port once.</summary>
        Reserved = 0,

        /// <summary>A duplicate of the exact same intent (or a correction) already owns the identity: send nothing.</summary>
        AlreadyAnswered = 1,
    }

    /// <summary>
    /// The ONE persistence/reservation step of a NEW money attempt (first refund or reviewed retry child).
    /// <para>
    /// It re-reads every fact it depends on AFTER the locks, refuses a stale reviewed decision/amount, counts the
    /// payment's whole Returns budget, and commits the attempt + case state BEFORE the caller touches an external port.
    /// It never commits or rolls back a caller-owned transaction, never clears the tracker, and never detaches an entry
    /// it did not add.
    /// </para>
    /// </summary>
    private async Task<AttemptReservation> PersistNewAttemptUnderReservationAsync(
        ReturnRequest request,
        Guid attemptId,
        string idempotencyKey,
        PaymentAuthority authority,
        int expectedDecisionVersion,
        Guid? reviewedFailedAttemptId,
        CancellationToken cancellationToken)
    {
        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            if (relational)
            {
                // Documented order: seller order → case → payment budget.
                await LockOwningSellerOrderAsync(request.ReturnRequestId, cancellationToken);
                await _db.Database.ExecuteSqlAsync(
                    $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {request.ReturnRequestId} FOR UPDATE",
                    cancellationToken);
            }

            // Re-read the CURRENT committed case and its attempts: every pre-lock value is discarded.
            await _db.Entry(request).ReloadAsync(cancellationToken);
            var attempts = await _db.RefundAttempts
                .Where(x => x.ReturnRequestId == request.ReturnRequestId)
                .ToListAsync(cancellationToken);
            request.AttachLoadedRefundAttempts(attempts);

            var state = await ReadDecisionStateAsync(request.ReturnRequestId, cancellationToken);
            if (state.EffectiveVersion != expectedDecisionVersion
                || !string.Equals(state.EffectiveDecision, ReturnDecisionRevision.ApproveDecision, StringComparison.Ordinal))
            {
                // The reviewed decision was corrected (or is not an approval) while this request waited: a stale review
                // can never send money.
                throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
            }

            if (request.PaymentId != authority.PaymentId)
            {
                throw new ContractOperationException(ReturnsErrorCodes.RefundBudgetBindingInvalid);
            }

            var amount = request.ReviewableMoney;
            if (amount < 0m || amount > authority.Amount)
            {
                throw new ContractOperationException(ReturnsErrorCodes.RefundBudgetExceeded);
            }

            if (attempts.Any(x => x.RefundAttemptId == attemptId
                    || string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)))
            {
                // A duplicate of the SAME intent already owns the identity, or a correction moved the case. Nothing is
                // re-minted and nothing is sent.
                await CommitCorrectionAsync(transaction, ownsTransaction, cancellationToken);
                return AttemptReservation.AlreadyAnswered;
            }

            if (reviewedFailedAttemptId is { } failedId)
            {
                // A NEW child exists only for the CURRENT reviewable definite failure, re-read after the lock.
                EnsureReviewedFailureIsStillCurrent(request, failedId);
                if (request.Status != ReturnRequestStatus.RefundFailed)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.RefundRetryInvalidState);
                }
            }
            else if (request.Status != ReturnRequestStatus.Approved)
            {
                throw new ContractOperationException(ReturnsErrorCodes.FirstRefundNotLawful);
            }

            if (relational)
            {
                // ONE stable advisory key per PAYMENT: two seller cases of one checkout can never both pass the ceiling.
                await ReturnBudgetSerialization.AcquireAsync(_db.Database, authority.PaymentId, cancellationToken);
            }

            // Every Returns case's Pending + Succeeded obligation of this payment is counted, not only this case/seller.
            var committed = await _db.RefundAttempts.AsNoTracking()
                .Where(x => x.PaymentId == authority.PaymentId
                    && (x.Status == RefundAttemptStatus.Pending || x.Status == RefundAttemptStatus.Succeeded))
                .Select(x => x.Amount)
                .ToListAsync(cancellationToken);
            if (committed.Sum() + amount > authority.Amount)
            {
                throw new ContractOperationException(ReturnsErrorCodes.RefundBudgetExceeded);
            }

            request.MarkRefundProcessing(_clock.UtcNow);
            request.FreezeApprovedAmount(amount);
            var attempt = request.BeginRefundAttempt(attemptId, authority.PaymentId, idempotencyKey, _clock.UtcNow);
            _db.RefundAttempts.Add(attempt);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsNewAttemptKeyConflict(ex))
            {
                // A concurrent duplicate of this SAME intent won the identity. Only THIS attempt's entry is detached
                // (never a caller-wide tracker clear) and nothing is sent.
                _db.Entry(attempt).State = EntityState.Detached;
                if (ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return AttemptReservation.AlreadyAnswered;
            }

            await CommitCorrectionAsync(transaction, ownsTransaction, cancellationToken);
            return AttemptReservation.Reserved;
        }
        catch
        {
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    /// <summary>
    /// TB-279-R1 (F4): a NEW effectful money command must name the reviewed decision revision. Missing is refused
    /// exactly like stale, because the server cannot otherwise tell whether the operator's review is still the
    /// effective one. (An exact stored-identity replay is answered BEFORE this gate by the caller.)
    /// </summary>
    private static int RequireReviewedDecisionVersion(int? expectedDecisionVersion) =>
        expectedDecisionVersion ?? throw new ContractOperationException(ReturnsErrorCodes.RefundReviewVersionRequired);

    /// <summary>The exact provider collision shape that means "this new attempt identity already exists".</summary>
    private static bool IsNewAttemptKeyConflict(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException { SqlState: "23505" } postgres
        && string.Equals(postgres.TableName, "refund_attempts", StringComparison.Ordinal)
        && string.Equals(postgres.SchemaName, ReturnsDbContext.Schema, StringComparison.Ordinal)
        && (string.Equals(postgres.ConstraintName, "pk_refund_attempts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(postgres.ConstraintName, "ix_refund_attempts_IdempotencyKey", StringComparison.OrdinalIgnoreCase));
}
