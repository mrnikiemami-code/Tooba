using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Infrastructure.Directories.Shared;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Providers;

namespace Tooba.Payment.Infrastructure.Directories;

/// <summary>
/// انقضای پرداخت‌های unpaid و بازگشایی برای تلاش دوباره. فقط <see cref="IPaymentExpiryDirectory"/> را پیاده می‌کند؛
/// مرزهای تراکنش و قفل دسته‌ای (<c>FOR UPDATE SKIP LOCKED</c>) دست‌نخورده مانده‌اند.
/// </summary>
public sealed class PaymentExpiryDirectory : IPaymentExpiryDirectory
{
    private readonly PaymentDbContext _db;
    private readonly IPaymentUseCaseGuard _guard;
    private readonly PaymentActorAccess _actorAccess;
    private readonly IPaymentGatewayRegistry _gateways;
    private readonly PaymentGatewayActorContext _actorContext;
    private readonly PaymentUnpaidTimeoutAssigner _timeoutAssigner;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>
    /// دایرکتوری انقضا را به schema payment و رجیستری درگاه وصل می‌کند. سیاست مهلت از نگه‌داشت تجاری می‌آید.
    /// </summary>
    public PaymentExpiryDirectory(
        PaymentDbContext db,
        IPaymentUseCaseGuard guard,
        IPayableCheckoutReader orders,
        IPaymentGatewayRegistry gateways,
        PaymentGatewayActorContext actorContext,
        IClock clock,
        IIdGenerator ids,
        ICommerceHoldPolicy? holdPolicy = null)
    {
        _db = db;
        _guard = guard;
        _actorAccess = new PaymentActorAccess(orders);
        _gateways = gateways;
        _actorContext = actorContext;
        _timeoutAssigner = new PaymentUnpaidTimeoutAssigner(holdPolicy);
        _clock = clock;
        _ids = ids;
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
            ?? throw new ContractOperationException("payment.not_found");
        await _actorAccess.EnsureActorCanSeeAsync(payment, actorUserId, buyerPartyId, cancellationToken);
        if (payment.Status != PaymentStatus.Expired)
        {
            throw new ContractOperationException("payment.unpaid.retry.invalid_state");
        }

        var attempts = await _db.Attempts.Where(x => x.PaymentId == paymentId).ToListAsync(cancellationToken);
        foreach (var loaded in attempts)
        {
            payment.AttachLoadedAttempt(loaded);
        }

        _actorContext.ActorUserId = actorUserId;
        var gateway = _gateways.Resolve(payment.ProviderCode);
        var initiation = await gateway.InitiateAsync(payment.PaymentId, payment.Amount, payment.Currency, cancellationToken);
        var attempt = payment.RecordInitiation(_ids.NewId(), initiation.ProviderRequestReference, _clock.UtcNow);
        _timeoutAssigner.Assign(payment, _clock.UtcNow);
        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
