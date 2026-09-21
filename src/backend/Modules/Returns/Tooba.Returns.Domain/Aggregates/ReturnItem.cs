using Tooba.BuildingBlocks;
using Tooba.Returns.Domain.Events;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Domain.Aggregates;


/// <summary>
/// خط مرجوعی با snapshot قیمت سفارش.
/// </summary>
public sealed class ReturnItem
{
    private ReturnItem()
    {
    }

    /// <summary>شناسه خط مرجوعی.</summary>
    public Guid ReturnItemId { get; init; }

    /// <summary>درخواست مالک.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>خط سفارش مرجع.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>تعداد درخواستی.</summary>
    public decimal Quantity { get; init; }

    /// <summary>snapshot قیمت واحد.</summary>
    public decimal UnitPriceSnapshot { get; init; }

    /// <summary>snapshot ارز.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>رزرو موجودی مرجع؛ FK Inventory نیست.</summary>
    public Guid? ReservationId { get; init; }

    internal static ReturnItem Create(
        Guid returnItemId,
        Guid returnRequestId,
        Guid orderLineId,
        decimal quantity,
        decimal unitPriceSnapshot,
        string currency,
        Guid? reservationId) =>
        new()
        {
            ReturnItemId = returnItemId,
            ReturnRequestId = returnRequestId,
            OrderLineId = orderLineId,
            Quantity = quantity,
            UnitPriceSnapshot = unitPriceSnapshot,
            Currency = currency.Trim(),
            ReservationId = reservationId,
        };
}
