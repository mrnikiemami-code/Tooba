using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// رکورد Inbox تصویر پرداخت. تکراری بودن delivery را در حافظه نگه نمی‌دارد.
/// </summary>
public sealed class OrderPaymentInboxRecord
{
    /// <summary>
    /// شناسهٔ رویداد Integration از ستون Outbox؛ کلید مصرف است نه PaymentId.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// پرداخت مرجع برای ممیزی. حقیقت Verify نیست.
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// زمان اعمال موفق تصویر Paid روی سفارش.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; init; }
}

/// <summary>
/// مصرف‌کنندهٔ متعلق به Order برای payment.succeeded.v1. PaymentDbContext باز نمی‌شود.
/// </summary>
public sealed class OrderPaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
{
    private readonly OrderDbContext _db;
    private readonly IOrderPaymentProjection _projection;

    /// <summary>
    /// مصرف‌کننده را به schema order و درز تصویر Paid وصل می‌کند.
    /// </summary>
    public OrderPaymentSucceededHandler(OrderDbContext db, IOrderPaymentProjection projection)
    {
        _db = db;
        _projection = projection;
    }

    /// <inheritdoc />
    public async Task HandleAsync(PaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        if (await _db.PaymentInbox.AnyAsync(x => x.EventId == integrationEvent.Metadata.EventId, cancellationToken))
        {
            return;
        }

        var payable = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == integrationEvent.CheckoutId, cancellationToken)
            ?? throw new InvalidOperationException("checkout برای تصویر پرداخت پیدا نشد و رویداد مصرف‌شده علامت نمی‌خورد.");

        var targets = payable.SellerOrders
            .Where(x => integrationEvent.SellerOrderIds.Contains(x.SellerOrderId))
            .ToArray();
        if (targets.Length != integrationEvent.SellerOrderIds.Distinct().Count())
        {
            throw new InvalidOperationException("تخصیص پرداخت با سفارش‌های checkout یکی نیست.");
        }

        if (!string.Equals(payable.Currency, integrationEvent.Currency, StringComparison.OrdinalIgnoreCase)
            || targets.Sum(x => x.GrandTotalSnapshot) != integrationEvent.Amount)
        {
            throw new InvalidOperationException("مبلغ یا ارز رویداد پرداخت با تصویر سفارش یکی نیست؛ Paid نمی‌شود.");
        }

        await _projection.ApplyVerifiedSuccessAsync(
            integrationEvent.CheckoutId,
            integrationEvent.PaymentId,
            integrationEvent.SellerOrderIds,
            cancellationToken);

        _db.PaymentInbox.Add(new OrderPaymentInboxRecord
        {
            EventId = integrationEvent.Metadata.EventId,
            PaymentId = integrationEvent.PaymentId,
            ProcessedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
