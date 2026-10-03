using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Events;

/// <summary>
/// رویداد ایجاد سفارش فروشنده.
/// </summary>
public sealed class SellerOrderCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public SellerOrderCreatedDomainEvent(Guid checkoutId, Guid sellerOrderId, Guid sellerPartyId, OrderMode mode)
    {
        CheckoutId = checkoutId;
        SellerOrderId = sellerOrderId;
        SellerPartyId = sellerPartyId;
        Mode = mode;
        Metadata = EventMetadataFactory.ForDomain("order.seller_order_created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; }

    /// <summary>
    /// سفارش فروشنده.
    /// </summary>
    public Guid SellerOrderId { get; }

    /// <summary>
    /// فروشنده.
    /// </summary>
    public Guid SellerPartyId { get; }

    /// <summary>
    /// حالت.
    /// </summary>
    public OrderMode Mode { get; }
}

