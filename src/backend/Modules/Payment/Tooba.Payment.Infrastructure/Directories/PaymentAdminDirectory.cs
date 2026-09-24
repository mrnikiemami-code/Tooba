using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Returns;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Providers;

namespace Tooba.Payment.Infrastructure.Directories;

/// <summary>
/// بازرسی و عملیات مدیریتی پرداخت. فقط <see cref="IPaymentAdminDirectory"/> را پیاده می‌کند؛ تأیید متعارف را
/// از <see cref="IPaymentDirectory"/> می‌گیرد و منطق Verify را بازنویسی نمی‌کند.
/// </summary>
public sealed class PaymentAdminDirectory(
    PaymentDbContext db,
    IPaymentUseCaseGuard guard,
    IPaymentDirectory payments,
    IClock clock,
    IIdGenerator ids,
    IPaymentRefundGateway? refundGateway = null) : IPaymentAdminDirectory
{
    /// <inheritdoc />
    public async Task<PaymentOperationalSnapshot?> GetOperationalAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await db.Payments.AsNoTracking()
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
        var payment = await db.Payments.AsNoTracking()
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
        var payment = await db.Payments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ContractOperationException("payment.missing");
        var attempt = await db.Attempts.AsNoTracking()
            .Where(x => x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ContractOperationException("payment.attempt.missing");
        return await payments.VerifyAsync(
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
        await guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await db.Payments
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ContractOperationException("payment.missing");
        if (payment.Status == PaymentStatus.Succeeded)
        {
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
        }

        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new ContractOperationException("payment.method.not_manual");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new ContractOperationException("payment.confirm.invalid_state");
        }

        var attempt = await db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ContractOperationException("payment.attempt.missing");
        if (string.IsNullOrWhiteSpace(attempt.CustomerTransferReference))
        {
            throw new ContractOperationException("payment.tracking_reference.required");
        }

        payment.AttachLoadedAttempt(attempt);
        var allocations = await db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        payment.AttachLoadedAllocations(allocations);
        var txn = $"manual-confirm-{payment.PaymentId:N}-{attempt.AttemptId:N}";
        var duplicateTxn = await db.Attempts.AnyAsync(
            x => x.ProviderTransactionReference == txn,
            cancellationToken).ConfigureAwait(false);
        if (duplicateTxn)
        {
            return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
        }

        var firstSuccess = payment.ApplyVerifiedSuccess(attempt.AttemptId, txn, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, firstSuccess);
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> RejectDepositAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await db.Payments
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ContractOperationException("payment.missing");
        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new ContractOperationException("payment.method.not_manual");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new ContractOperationException("payment.reject.invalid_state");
        }

        var attempt = await db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ContractOperationException("payment.attempt.missing");

        payment.AttachLoadedAttempt(attempt);
        var allocations = await db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        payment.AttachLoadedAllocations(allocations);
        payment.ApplyVerifiedFailure(attempt.AttemptId, "MANUAL_DEPOSIT_REJECTED", clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> RestoreDepositAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await db.Payments
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ContractOperationException("payment.missing");
        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new ContractOperationException("payment.restore.not_manual");
        }

        var attempts = await db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        var allocations = await db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        payment.AttachLoadedAllocations(allocations);
        var restored = payment.RestoreRejectedManualToPending(ids.NewId(), clock.UtcNow);
        if (attempts.All(x => x.AttemptId != restored.AttemptId))
        {
            db.Attempts.Add(restored);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
    }

    /// <inheritdoc />
    public async Task<PaymentVerificationResult> UnconfirmDepositAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        await guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await db.Payments
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ContractOperationException("payment.missing");
        if (!ManualPaymentGateway.IsManual(payment.ProviderCode))
        {
            throw new ContractOperationException("payment.unconfirm.not_manual");
        }

        var attempts = await db.Attempts
            .Where(x => x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        var allocations = await db.Allocations.Where(x => x.PaymentId == payment.PaymentId).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        payment.AttachLoadedAllocations(allocations);
        var unconfirmed = payment.UnconfirmManualDeposit(ids.NewId(), clock.UtcNow);
        if (attempts.All(x => x.AttemptId != unconfirmed.AttemptId))
        {
            db.Attempts.Add(unconfirmed);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentVerificationResult(payment.PaymentId, payment.Status, NewlySucceeded: false);
    }

    /// <inheritdoc />
    public async Task CloseOrStartRefundForOrderCancelAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await db.Payments
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        var now = clock.UtcNow;
        if (payment.Status is PaymentStatus.Created
            or PaymentStatus.Pending
            or PaymentStatus.Failed
            or PaymentStatus.Expired)
        {
            payment.CloseForOrderCancel(now);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (payment.Status is PaymentStatus.Cancelled or PaymentStatus.Refunded or PaymentStatus.RefundFailed)
        {
            return;
        }

        payment.BeginOrderCancelRefund(now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (ManualPaymentGateway.IsManual(payment.ProviderCode) || refundGateway is null)
        {
            return;
        }

        try
        {
            var result = await refundGateway.RefundAsync(
                payment.PaymentId,
                payment.Amount,
                payment.Currency,
                $"order-cancel-refund:{payment.PaymentId:N}",
                cancellationToken).ConfigureAwait(false);
            if (result.Succeeded)
            {
                payment.MarkRefunded(clock.UtcNow);
            }
            else
            {
                payment.MarkRefundFailed(result.FailureCode, clock.UtcNow);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ContractOperationException ex) when (ex.Code == "payment.refund.gateway.unconfigured")
        {
            // RefundPending remains for admin action.
        }
        catch (ContractOperationException ex) when (ex.Code.StartsWith("payment.", StringComparison.Ordinal))
        {
            payment.MarkRefundFailed(ex.Code, clock.UtcNow);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RestoreAfterOrderCancelRestoreAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);
        var payment = await db.Payments
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            return;
        }

        var attempts = await db.Attempts
            .Where(x => x.PaymentId == payment.PaymentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        payment.RestoreAfterOrderCancelRestore(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<PaymentOperationalSnapshot> ToOperationalAsync(
        CustomerPayment payment,
        CancellationToken cancellationToken)
    {
        var attempts = await db.Attempts.AsNoTracking()
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
}
