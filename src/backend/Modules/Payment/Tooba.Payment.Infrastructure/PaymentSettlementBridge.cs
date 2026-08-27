using Microsoft.EntityFrameworkCore;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// snapshot پرداخت برای Settlement بدون cross-DbContext.
/// </summary>
public sealed class PaymentSettlementBridge : IPaymentSettlementReader
{
    private readonly PaymentDbContext _db;

    /// <summary>پل settlement را به schema payment وصل می‌کند.</summary>
    public PaymentSettlementBridge(PaymentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<PaymentSettlementSnapshot?> GetAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken);
        return payment is null
            ? null
            : new PaymentSettlementSnapshot(
                payment.PaymentId,
                payment.CheckoutId,
                payment.Amount,
                payment.Currency,
                payment.Status);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentSettlementAllocationSnapshot>> GetAllocationsAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var allocations = await _db.Allocations.AsNoTracking()
            .Where(x => x.PaymentId == paymentId)
            .ToListAsync(cancellationToken);
        return allocations
            .Select(x => new PaymentSettlementAllocationSnapshot(x.SellerOrderId, x.AllocatedAmount, x.Currency))
            .ToArray();
    }
}
