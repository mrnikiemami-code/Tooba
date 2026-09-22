using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain.Events;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>
/// واحد fulfillment برای یک SellerOrder پرداخت‌شده.
/// </summary>
public sealed class FulfillmentUnit : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();
    private readonly List<FulfillmentItem> _items = [];
    private readonly List<Shipment> _shipments = [];

    private FulfillmentUnit()
    {
    }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>سفارش فروشنده مرجع.</summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>خریدار.</summary>
    public Guid PlacedByUserId { get; init; }

    /// <summary>وضعیت عملیاتی.</summary>
    public FulfillmentStatus Status { get; private set; }

    /// <summary>snapshot گیرنده.</summary>
    public string RecipientName { get; init; } = string.Empty;

    /// <summary>snapshot موبایل.</summary>
    public string ContactMobile { get; init; } = string.Empty;

    /// <summary>snapshot استان.</summary>
    public string ProvinceName { get; init; } = string.Empty;

    /// <summary>snapshot شهر.</summary>
    public string CityName { get; init; } = string.Empty;

    /// <summary>snapshot آدرس.</summary>
    public string PostalAddress { get; init; } = string.Empty;

    /// <summary>snapshot کدپستی.</summary>
    public string PostalCode { get; init; } = string.Empty;

    /// <summary>snapshot روش ارسال.</summary>
    public string ShippingMethodCode { get; init; } = string.Empty;

    /// <summary>snapshot برچسب روش ارسال.</summary>
    public string ShippingMethodLabel { get; init; } = string.Empty;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>خطوط fulfillment.</summary>
    public IReadOnlyList<FulfillmentItem> Items => _items;

    /// <summary>محموله‌ها.</summary>
    public IReadOnlyList<Shipment> Shipments => _shipments;

    /// <summary>رویدادهای دامنه.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// fulfillment را از snapshot Paid می‌سازد.
    /// </summary>
    public static FulfillmentUnit CreateFromPaidOrder(
        Guid fulfillmentId,
        Func<Guid> newId,
        Guid sellerOrderId,
        Guid checkoutId,
        Guid sellerPartyId,
        Guid placedByUserId,
        string recipientName,
        string contactMobile,
        string provinceName,
        string cityName,
        string postalAddress,
        string postalCode,
        string shippingMethodCode,
        string shippingMethodLabel,
        IEnumerable<(Guid OrderLineId, decimal Quantity, Guid? ReservationId)> lines,
        DateTimeOffset now)
    {
        var unit = new FulfillmentUnit
        {
            FulfillmentId = fulfillmentId,
            SellerOrderId = sellerOrderId,
            CheckoutId = checkoutId,
            SellerPartyId = sellerPartyId,
            PlacedByUserId = placedByUserId,
            Status = FulfillmentStatus.ReadyToFulfill,
            RecipientName = recipientName.Trim(),
            ContactMobile = contactMobile.Trim(),
            ProvinceName = provinceName.Trim(),
            CityName = cityName.Trim(),
            PostalAddress = postalAddress.Trim(),
            PostalCode = postalCode.Trim(),
            ShippingMethodCode = shippingMethodCode.Trim(),
            ShippingMethodLabel = shippingMethodLabel.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        foreach (var line in lines)
        {
            unit._items.Add(FulfillmentItem.Create(newId(), unit.FulfillmentId, line.OrderLineId, line.Quantity, line.ReservationId));
        }

        unit._domainEvents.Add(new FulfillmentCreatedDomainEvent(unit.FulfillmentId, unit.SellerOrderId, unit.CheckoutId));
        return unit;
    }

    /// <summary>خطوط بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedItems(IEnumerable<FulfillmentItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    /// <summary>محموله‌های بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedShipments(IEnumerable<Shipment> shipments)
    {
        _shipments.Clear();
        _shipments.AddRange(shipments);
    }

    /// <summary>همهٔ باقیمانده را وارد پردازش می‌کند.</summary>
    public void MarkProcessing(DateTimeOffset now)
    {
        var remaining = _items
            .Select(item => (item.OrderLineId, Quantity: item.QuantityOrdered - item.QuantityProcessing))
            .Where(x => x.Quantity > 0)
            .ToArray();
        if (remaining.Length > 0)
        {
            ProcessSelections(remaining, now);
            return;
        }

        EnsureNotTerminal();
        if (Status is FulfillmentStatus.ReadyToFulfill or FulfillmentStatus.Processing)
        {
            Status = FulfillmentStatus.Processing;
            UpdatedAt = now;
            return;
        }

        throw new ContractOperationException("fulfillment.status.processing_invalid");
    }

    /// <summary>پردازش انتخاب‌شده فقط همان خطوط را جلو می‌برد.</summary>
    public IReadOnlyList<(Guid OrderLineId, decimal Quantity)> ProcessSelections(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections,
        DateTimeOffset now)
    {
        EnsureNotTerminal();
        if (Status == FulfillmentStatus.Delivered)
        {
            throw new ContractOperationException("fulfillment.process.after_delivered");
        }

        var normalized = NormalizeSelections(selections);
        var affected = new List<(Guid OrderLineId, decimal Quantity)>(normalized.Count);
        foreach (var selection in normalized)
        {
            var item = RequireItem(selection.OrderLineId);
            item.ApplyProcessingQuantity(selection.Quantity);
            affected.Add(selection);
        }

        RefreshWarehouseStatus(now);
        return affected;
    }

    /// <summary>برگشت از پردازش فقط برای تعداد بسته‌بندی‌نشده.</summary>
    public IReadOnlyList<(Guid OrderLineId, decimal Quantity)> UnprocessSelections(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections,
        DateTimeOffset now)
    {
        EnsureNotTerminal();
        var normalized = NormalizeSelections(selections);
        var affected = new List<(Guid OrderLineId, decimal Quantity)>(normalized.Count);
        foreach (var selection in normalized)
        {
            var item = RequireItem(selection.OrderLineId);
            item.ReleaseProcessingQuantity(selection.Quantity);
            affected.Add(selection);
        }

        RefreshWarehouseStatus(now);
        return affected;
    }

    /// <summary>همهٔ باقیماندهٔ پردازش‌شدهٔ قابل بسته‌بندی را بسته‌بندی می‌کند.</summary>
    public void MarkPacked(DateTimeOffset now)
    {
        EnsureNotTerminal();
        if (Status == FulfillmentStatus.Delivered)
        {
            throw new ContractOperationException("fulfillment.pack.after_delivered");
        }

        var selections = _items
            .Select(item => (item.OrderLineId, Quantity: item.QuantityProcessing - item.QuantityPacked))
            .Where(x => x.Quantity > 0)
            .ToArray();
        if (selections.Length == 0)
        {
            throw new ContractOperationException("fulfillment.pack.requires_processing");
        }

        PackSelections(selections, now);
    }

    /// <summary>بسته‌بندی انتخاب‌شده (خط/تعداد) روی همان مسیر canonical.</summary>
    public IReadOnlyList<(Guid OrderLineId, decimal Quantity)> PackSelections(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections,
        DateTimeOffset now)
    {
        EnsureNotTerminal();
        if (Status == FulfillmentStatus.Delivered)
        {
            throw new ContractOperationException("fulfillment.pack.after_delivered");
        }

        var normalized = NormalizeSelections(selections);
        var affected = new List<(Guid OrderLineId, decimal Quantity)>(normalized.Count);
        foreach (var selection in normalized)
        {
            var item = RequireItem(selection.OrderLineId);
            if (item.QuantityPacked + selection.Quantity > item.QuantityOrdered)
            {
                throw new ContractOperationException("fulfillment.pack.qty_exceeds");
            }

            if (item.QuantityProcessing < item.QuantityPacked + selection.Quantity)
            {
                throw new ContractOperationException("fulfillment.pack.requires_processing");
            }

            item.ApplyPackedQuantity(selection.Quantity);
            affected.Add(selection);
            _domainEvents.Add(new FulfillmentLinePackedDomainEvent(
                FulfillmentId,
                SellerOrderId,
                selection.OrderLineId,
                selection.Quantity));
        }

        RefreshWarehouseStatus(now);
        return affected;
    }

    /// <summary>بازگشت از بسته‌بندی فقط برای تعداد تخصیص‌نشده/ارسال‌نشده.</summary>
    public IReadOnlyList<(Guid OrderLineId, decimal Quantity)> UnpackSelections(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections,
        DateTimeOffset now)
    {
        EnsureNotTerminal();
        var normalized = NormalizeSelections(selections);
        var affected = new List<(Guid OrderLineId, decimal Quantity)>(normalized.Count);
        foreach (var selection in normalized)
        {
            var item = RequireItem(selection.OrderLineId);
            var blockingAllocated = ActiveAllocatedQuantity(selection.OrderLineId);
            var unpackable = item.QuantityPacked - blockingAllocated;
            if (selection.Quantity > unpackable)
            {
                throw new ContractOperationException("fulfillment.pack.release_allocated");
            }

            item.ReleasePackedQuantity(selection.Quantity);
            affected.Add(selection);
            _domainEvents.Add(new FulfillmentLineUnpackedDomainEvent(
                FulfillmentId,
                SellerOrderId,
                selection.OrderLineId,
                selection.Quantity));
        }

        RefreshWarehouseStatus(now);
        return affected;
    }

    private void RefreshWarehouseStatus(DateTimeOffset now)
    {
        if (Status is FulfillmentStatus.Dispatched
            or FulfillmentStatus.InTransit
            or FulfillmentStatus.Delivered
            or FulfillmentStatus.Failed
            or FulfillmentStatus.Cancelled)
        {
            UpdatedAt = now;
            return;
        }

        if (_items.Count > 0 && _items.All(x => x.QuantityPacked >= x.QuantityOrdered))
        {
            Status = FulfillmentStatus.Packed;
        }
        else if (_items.Any(x => x.QuantityProcessing > 0 || x.QuantityPacked > 0))
        {
            Status = FulfillmentStatus.Processing;
        }
        else
        {
            Status = FulfillmentStatus.ReadyToFulfill;
        }

        UpdatedAt = now;
    }

    /// <summary>محموله جدید ثبت می‌کند.</summary>
    public Shipment CreateShipment(
        Guid shipmentId,
        Func<Guid> newId,
        string carrierDisplayName,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> items,
        DateTimeOffset now,
        string? shippingMethodCode = null,
        string? shippingMethodLabel = null,
        string? providerMetadataJson = null,
        int providerMetadataVersion = 0)
    {
        EnsureNotTerminal();
        if (Status is FulfillmentStatus.Cancelled or FulfillmentStatus.Failed or FulfillmentStatus.Delivered)
        {
            throw new ContractOperationException("fulfillment.shipment.create_terminal");
        }

        var normalized = NormalizeSelections(items);
        var shipment = Shipment.Create(
            shipmentId,
            newId,
            FulfillmentId,
            carrierDisplayName,
            normalized,
            _items,
            _shipments,
            now,
            shippingMethodCode,
            shippingMethodLabel,
            providerMetadataJson,
            providerMetadataVersion);
        _shipments.Add(shipment);
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentCreatedDomainEvent(FulfillmentId, shipment.ShipmentId, SellerOrderId));
        return shipment;
    }

    /// <summary>ابطال مرسولهٔ پیش از dispatch و آزادسازی تخصیص.</summary>
    public void CancelShipment(Guid shipmentId, DateTimeOffset now)
    {
        EnsureNotTerminal();
        var shipment = RequireShipment(shipmentId);
        shipment.CancelPreDispatch(now);
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentCancelledDomainEvent(FulfillmentId, shipmentId, SellerOrderId));
    }

    /// <summary>
    /// لغو کل سفارش: مرسوله‌های پیش از Dispatch باطل می‌شوند، پیشرفت انبار ارسال‌نشده
    /// بازنشانی می‌شود و واحد Cancelled می‌ماند. اگر قبلاً Cancelled باشد no-op است.
    /// </summary>
    public void AbortForOrderCancel(DateTimeOffset now)
    {
        if (Status == FulfillmentStatus.Cancelled)
        {
            return;
        }

        if (HasDispatchedQuantity())
        {
            throw new ContractOperationException("fulfillment.cancel.already_dispatched");
        }

        foreach (var shipment in _shipments.Where(x => x.Status == ShipmentStatus.Created).ToList())
        {
            shipment.CancelPreDispatch(now);
            _domainEvents.Add(new ShipmentCancelledDomainEvent(FulfillmentId, shipment.ShipmentId, SellerOrderId));
        }

        foreach (var item in _items)
        {
            item.ResetWarehouseProgress();
        }

        Status = FulfillmentStatus.Cancelled;
        UpdatedAt = now;
    }

    /// <summary>
    /// بازگردانی: مرسوله‌های باطل‌شده زنده نمی‌شوند. پیشرفت انبار ارسال‌نشده صفر می‌شود
    /// تا واحد ReadyToFulfill شود، نه Packed روی تخصیص مرده.
    /// </summary>
    public void ReactivateAfterOrderRestore(DateTimeOffset now)
    {
        if (Status != FulfillmentStatus.Cancelled)
        {
            return;
        }

        if (HasDispatchedQuantity())
        {
            throw new ContractOperationException("fulfillment.restore.already_dispatched");
        }

        foreach (var item in _items)
        {
            item.ResetWarehouseProgress();
        }

        Status = FulfillmentStatus.ReadyToFulfill;
        RefreshWarehouseStatus(now);
    }

    /// <summary>
    /// مرجع رزرو فعال خطوط را با شناسه‌های فعلی OrderLine هم‌تراز می‌کند (idempotent).
    /// </summary>
    public void RebindActiveReservations(IReadOnlyDictionary<Guid, Guid?> reservationsByOrderLineId)
    {
        if (HasDispatchedQuantity() && _items.All(x => x.ReservationConsumed || x.QuantityShipped >= x.QuantityOrdered))
        {
            return;
        }

        foreach (var item in _items)
        {
            if (!reservationsByOrderLineId.TryGetValue(item.OrderLineId, out var reservationId))
            {
                continue;
            }

            item.RebindActiveReservation(reservationId);
        }
    }

    /// <summary>آیا در این واحد quantity واقعی Dispatch شده است.</summary>
    public bool HasDispatchedQuantity() =>
        Status is FulfillmentStatus.Dispatched or FulfillmentStatus.InTransit or FulfillmentStatus.Delivered
        || _items.Any(item => item.QuantityShipped > 0)
        || _shipments.Any(shipment =>
            shipment.Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered
            || shipment.DispatchedAt is not null
            || shipment.DeliveredAt is not null);

    /// <summary>تعداد فعال تخصیص‌یافته (Created + Shipped) برای یک خط.</summary>
    public decimal ActiveAllocatedQuantity(Guid orderLineId) =>
        OpenAllocatedQuantity(orderLineId) + (_items.SingleOrDefault(x => x.OrderLineId == orderLineId)?.QuantityShipped ?? 0);

    /// <summary>تعداد تخصیص باز در مرسوله‌های Created.</summary>
    public decimal OpenAllocatedQuantity(Guid orderLineId) =>
        _shipments
            .Where(s => s.Status == ShipmentStatus.Created)
            .SelectMany(s => s.Items)
            .Where(x => x.OrderLineId == orderLineId)
            .Sum(x => x.Quantity);

    private FulfillmentItem RequireItem(Guid orderLineId) =>
        _items.SingleOrDefault(x => x.OrderLineId == orderLineId)
        ?? throw new ContractOperationException("fulfillment.order_line.not_found");

    private static IReadOnlyList<(Guid OrderLineId, decimal Quantity)> NormalizeSelections(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections)
    {
        if (selections is null || selections.Count == 0)
        {
            throw new ContractOperationException("fulfillment.selection.required");
        }

        var map = new Dictionary<Guid, decimal>();
        foreach (var selection in selections)
        {
            if (selection.Quantity <= 0)
            {
                throw new ContractOperationException("fulfillment.qty.positive");
            }

            map[selection.OrderLineId] = map.TryGetValue(selection.OrderLineId, out var existing)
                ? existing + selection.Quantity
                : selection.Quantity;
        }

        return map.Select(x => (x.Key, x.Value)).ToArray();
    }

    /// <summary>پس از dispatch محموله وضعیت را به‌روز می‌کند.</summary>
    public void ApplyShipmentDispatched(Guid shipmentId, DateTimeOffset now)
    {
        var shipment = RequireShipment(shipmentId);
        shipment.EnsureDispatched(now);
        foreach (var item in shipment.Items)
        {
            var fulfillmentItem = _items.Single(x => x.OrderLineId == item.OrderLineId);
            fulfillmentItem.ApplyShippedQuantity(item.Quantity);
        }

        Status = FulfillmentStatus.Dispatched;
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentDispatchedDomainEvent(FulfillmentId, shipmentId, SellerOrderId));
    }

    /// <summary>محموله را Delivered علامت می‌زند.</summary>
    public void ApplyShipmentDelivered(Guid shipmentId, DateTimeOffset now)
    {
        var shipment = RequireShipment(shipmentId);
        shipment.EnsureDelivered(now);
        Status = AllItemsDelivered() ? FulfillmentStatus.Delivered : FulfillmentStatus.InTransit;
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentDeliveredDomainEvent(FulfillmentId, shipmentId, SellerOrderId));
    }

    /// <summary>ردیابی را idempotent ثبت می‌کند.</summary>
    public void AssignTracking(Guid shipmentId, string trackingReference, DateTimeOffset now)
    {
        var shipment = RequireShipment(shipmentId);
        shipment.AssignTracking(trackingReference, now);
        UpdatedAt = now;
    }

    /// <summary>کد رهگیری را فقط پیش از dispatch اصلاح می‌کند و مقدار قبلی را نگه می‌دارد.</summary>
    public void CorrectTracking(Guid shipmentId, string trackingReference, DateTimeOffset now)
    {
        var shipment = RequireShipment(shipmentId);
        var previous = shipment.TrackingReference;
        shipment.CorrectTracking(trackingReference, now);
        UpdatedAt = now;
        if (!string.Equals(previous, shipment.TrackingReference, StringComparison.Ordinal))
        {
            _domainEvents.Add(new ShipmentTrackingCorrectedDomainEvent(
                FulfillmentId,
                shipmentId,
                SellerOrderId,
                previous,
                shipment.TrackingReference));
        }
    }

    private Shipment RequireShipment(Guid shipmentId) =>
        _shipments.SingleOrDefault(x => x.ShipmentId == shipmentId)
        ?? throw new ContractOperationException("fulfillment.shipment.not_found");

    private bool AllItemsDelivered() =>
        _items.Count > 0 && _items.All(x => x.QuantityShipped >= x.QuantityOrdered);

    private void EnsureNotTerminal()
    {
        if (Status is FulfillmentStatus.Cancelled or FulfillmentStatus.Failed)
        {
            throw new ContractOperationException("fulfillment.status.terminal");
        }
    }
}
