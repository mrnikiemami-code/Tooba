using Microsoft.EntityFrameworkCore;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Payment.Infrastructure.Directories;

/// <summary>
/// reconciliation پرداخت‌های Pending قدیمی (callback گم‌شده/دیررسیده). فقط <see cref="IPaymentReconciliationDirectory"/>
/// را پیاده می‌کند و منطق تأیید را بازنویسی نمی‌کند؛ Verify متعارف از <see cref="IPaymentDirectory"/> می‌آید.
/// </summary>
public sealed class PaymentReconciliationDirectory(
    PaymentDbContext db,
    IPaymentDirectory payments) : IPaymentReconciliationDirectory
{
    /// <inheritdoc />
    public async Task<int> ReconcileStalePendingAsync(
        DateTimeOffset asOf,
        TimeSpan minAge,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var cutoff = asOf - minAge;
        var pending = await db.Payments.AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Pending && x.UpdatedAt <= cutoff)
            .OrderBy(x => x.UpdatedAt)
            .Take(Math.Max(1, batchSize))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var processed = 0;
        foreach (var payment in pending)
        {
            var attempt = await db.Attempts.AsNoTracking()
                .Where(x => x.PaymentId == payment.PaymentId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (attempt is null)
            {
                continue;
            }

            await payments.VerifyAsync(
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
}
