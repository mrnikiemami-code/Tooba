using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Storefront;

/// <summary>
/// TB240-R4: خواندن فقط-خواندنی گروه‌های ارسال یک سفارش پایدار از خود Order.
/// </summary>
/// <remarks>
/// هیچ Shipping/DbContext دیگری خوانده نمی‌شود، هیچ مبلغ/تاریخی بازمحاسبه نمی‌گردد و هیچ نوشتنی رخ نمی‌دهد.
/// </remarks>
public sealed class OrderShippingGroupHistoryReader : IOrderShippingGroupHistoryReader
{
    private readonly OrderDbContext _db;

    /// <summary>پل را به schema order وصل می‌کند.</summary>
    public OrderShippingGroupHistoryReader(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderShippingGroupHistoryRecord>> GetForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty)
        {
            return [];
        }

        var groups = await _db.OrderShippingGroups
            .AsNoTracking()
            .Include(group => group.Lines)
            .Include(group => group.AcceptedEvidence)
            .Where(group => group.CheckoutId == checkoutId)
            .OrderBy(group => group.SellerPartyId)
            .ThenBy(group => group.ShippingGroupId)
            .ToListAsync(cancellationToken);

        return groups.Select(group =>
        {
            var evidence = group.AcceptedEvidence;
            return new OrderShippingGroupHistoryRecord(
                group.ShippingGroupId,
                group.SellerOrderId,
                group.SellerPartyId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.MethodLabel,
                group.ShippingChargeSnapshot,
                group.Currency,
                group.MinimumDeliveryDate,
                group.RequestedDeliveryDate,
                group.RequestedDeliveryTimeWindow,
                group.LeadDays,
                group.State,
                group.Lines.Select(line => line.OrderLineId).ToArray(),
                evidence is null
                    ? null
                    : new OrderShippingGroupHistoryEvidence(
                        evidence.Owner.Kind.ToString(),
                        evidence.Owner.TenantKey,
                        evidence.Owner.SellerPartyId,
                        evidence.SnapshotId,
                        evidence.DraftId,
                        evidence.SourceRevision,
                        evidence.FormatVersion,
                        evidence.SelectionVersion,
                        evidence.AreaGroupId,
                        evidence.RateRowId,
                        evidence.ShippingTypeId,
                        evidence.FreightAmount,
                        evidence.PackagingAmount,
                        evidence.PayableAtCheckoutAmount,
                        evidence.CarrierCollectAmount,
                        evidence.ReceivingDate,
                        evidence.ReceivingOpenTicks,
                        evidence.ReceivingCloseTicks,
                        evidence.ReceivingTimeZone,
                        evidence.MinimumDeliveryDays,
                        evidence.MaximumDeliveryDays));
        }).ToArray();
    }
}
