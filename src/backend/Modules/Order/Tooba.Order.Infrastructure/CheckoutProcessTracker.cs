using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>پیاده‌سازی EF ردیابی فرآیند checkout روی schema order.</summary>
public sealed class CheckoutProcessTracker : ICheckoutProcessTracker
{
    private readonly OrderDbContext _db;
    private readonly IIdGenerator _ids;

    /// <summary>Tracker را می‌سازد.</summary>
    public CheckoutProcessTracker(OrderDbContext db, IIdGenerator ids)
    {
        _db = db;
        _ids = ids;
    }

    /// <inheritdoc />
    public Task<CheckoutProcess?> FindSucceededBySubmissionKeyAsync(
        string submissionIdempotencyKey,
        CancellationToken cancellationToken)
    {
        var key = submissionIdempotencyKey.Trim();
        return _db.CheckoutProcesses
            .FirstOrDefaultAsync(
                x => x.SubmissionIdempotencyKey == key && x.Status == CheckoutProcessStatus.PaymentPending,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CheckoutProcess> BeginAsync(
        string submissionIdempotencyKey,
        Guid cartId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var key = submissionIdempotencyKey.Trim();
        var existing = await _db.CheckoutProcesses
            .FirstOrDefaultAsync(x => x.SubmissionIdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            if (existing.IsSubmitSucceeded)
            {
                return existing;
            }

            if (existing.Status == CheckoutProcessStatus.Failed)
            {
                _db.CheckoutProcesses.Remove(existing);
            }
            else
            {
                // فرآیند نیمه‌کارهٔ همان کلید: برای W1 با TransactionScope مشترک، ردیف باید با rollback پاک شود.
                // اگر باقی مانده، همان هویت را برای ادامهٔ همان کلید بازمی‌گردانیم.
                return existing;
            }
        }

        var process = CheckoutProcess.Start(
            _ids.NewId(),
            key,
            cartId,
            $"checkout-process:{key}",
            now);
        _db.CheckoutProcesses.Add(process);
        return process;
    }

    /// <inheritdoc />
    public void MarkValidating(CheckoutProcess process, DateTimeOffset now) => process.MarkValidating(now);

    /// <inheritdoc />
    public void MarkInventoryReserving(CheckoutProcess process, DateTimeOffset now) =>
        process.MarkInventoryReserving(now);

    /// <inheritdoc />
    public void MarkOrderPersisting(CheckoutProcess process, Guid checkoutId, DateTimeOffset now) =>
        process.MarkOrderPersisting(checkoutId, now);

    /// <inheritdoc />
    public void MarkCartCommitting(CheckoutProcess process, DateTimeOffset now) =>
        process.MarkCartCommitting(now);

    /// <inheritdoc />
    public void MarkPaymentPending(CheckoutProcess process, DateTimeOffset now) =>
        process.MarkPaymentPending(now);

    /// <inheritdoc />
    public void MarkFailed(CheckoutProcess process, string failureCode, DateTimeOffset now) =>
        process.MarkFailed(failureCode, now);
}
