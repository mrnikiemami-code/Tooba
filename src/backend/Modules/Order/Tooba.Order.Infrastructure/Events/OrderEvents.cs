using Tooba.BuildingBlocks;
using Tooba.Order.Domain;

namespace Tooba.Order.Infrastructure.Events;

/// <summary>
/// Integration ارسال checkout. پرداخت موفق نیست.
/// </summary>
public sealed class CheckoutSubmittedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "order.checkout_submitted.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; set; }

    /// <summary>
    /// سبد مبدأ.
    /// </summary>
    public Guid CartId { get; set; }
}

/// <summary>
/// Integration ایجاد سفارش فروشنده.
/// </summary>
public sealed class SellerOrderCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "order.seller_order_created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; set; }

    /// <summary>
    /// سفارش فروشنده.
    /// </summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>
    /// فروشنده.
    /// </summary>
    public Guid SellerPartyId { get; set; }
}
