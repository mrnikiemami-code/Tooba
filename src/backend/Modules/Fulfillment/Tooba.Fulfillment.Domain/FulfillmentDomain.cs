using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain;

/// <summary>
/// وضعیت واحد fulfillment. با وضعیت تجاری Order یکی نیست.
/// </summary>
public enum FulfillmentStatus
{
    /// <summary>پس از Paid و آماده عملیات.</summary>
    ReadyToFulfill = 0,

    /// <summary>در حال پردازش انبار.</summary>
    Processing = 1,

    /// <summary>بسته‌بندی شده.</summary>
    Packed = 2,

    /// <summary>حداقل یک محموله dispatch شده.</summary>
    Dispatched = 3,

    /// <summary>در مسیر تحویل.</summary>
    InTransit = 4,

    /// <summary>تحویل نهایی.</summary>
    Delivered = 5,

    /// <summary>شکست عملیاتی.</summary>
    Failed = 6,

    /// <summary>لغو شده.</summary>
    Cancelled = 7,
}

/// <summary>
/// وضعیت محموله. Order نیست.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>ایجاد شده.</summary>
    Created = 0,

    /// <summary>dispatch شده.</summary>
    Dispatched = 1,

    /// <summary>در مسیر.</summary>
    InTransit = 2,

    /// <summary>تحویل شده.</summary>
    Delivered = 3,

    /// <summary>شکست خورده.</summary>
    Failed = 4,

    /// <summary>لغو شده.</summary>
    Cancelled = 5,
}

/// <summary>
/// خط fulfillment با snapshot تعداد سفارش.
/// </summary>
public sealed class FulfillmentItem
{
    private FulfillmentItem()
    {
    }

    /// <summary>شناسه خط fulfillment.</summary>
    public Guid FulfillmentItemId { get; init; }

    /// <summary>fulfillment مالک.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>شناسه خط سفارش مرجع.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>تعداد سفارش‌داده‌شده.</summary>
    public decimal QuantityOrdered { get; init; }

    /// <summary>تعداد واردشده به پردازش.</summary>
    public decimal QuantityProcessing { get; private set; }

    /// <summary>تعداد بسته‌بندی‌شده تجمعی.</summary>
    public decimal QuantityPacked { get; private set; }

    /// <summary>تعداد dispatch‌شده تجمعی.</summary>
    public decimal QuantityShipped { get; private set; }

    /// <summary>رزرو موجودی مرجع؛ FK Inventory نیست.</summary>
    public Guid? ReservationId { get; init; }

    /// <summary>آیا رزرو مصرف شده است.</summary>
    public bool ReservationConsumed { get; private set; }

    internal static FulfillmentItem Create(Guid fulfillmentId, Guid orderLineId, decimal quantityOrdered, Guid? reservationId) =>
        new()
        {
            FulfillmentItemId = Guid.NewGuid(),
            FulfillmentId = fulfillmentId,
            OrderLineId = orderLineId,
            QuantityOrdered = quantityOrdered,
            ReservationId = reservationId,
        };

    internal void ApplyProcessingQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد پردازش باید مثبت باشد.");
        }

        if (QuantityProcessing + quantity > QuantityOrdered)
        {
            throw new InvalidOperationException("تعداد پردازش از باقیمانده سفارش بیشتر است.");
        }

        QuantityProcessing += quantity;
    }

    internal void ReleaseProcessingQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد برگشت از پردازش باید مثبت باشد.");
        }

        if (QuantityProcessing - quantity < QuantityPacked)
        {
            throw new InvalidOperationException("برگشت از پردازش برای تعداد بسته‌بندی‌شده مجاز نیست.");
        }

        QuantityProcessing -= quantity;
    }

    internal void ApplyPackedQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد بسته‌بندی باید مثبت باشد.");
        }

        if (QuantityPacked + quantity > QuantityOrdered)
        {
            throw new InvalidOperationException("تعداد بسته‌بندی از باقیمانده سفارش بیشتر است.");
        }

        QuantityPacked += quantity;
    }

    internal void ReleasePackedQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد بازگشت از بسته‌بندی باید مثبت باشد.");
        }

        if (quantity > QuantityPacked)
        {
            throw new InvalidOperationException("تعداد بازگشت از بسته‌بندی از بسته‌بندی‌شده بیشتر است.");
        }

        QuantityPacked -= quantity;
    }

    internal void ResetWarehouseProgress()
    {
        QuantityProcessing = 0;
        QuantityPacked = 0;
    }

    internal void ApplyShippedQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد محموله باید مثبت باشد.");
        }

        if (QuantityShipped + quantity > QuantityOrdered)
        {
            throw new InvalidOperationException("تعداد محموله از سفارش بیشتر است.");
        }

        QuantityShipped += quantity;
    }

    /// <summary>رزرو را مصرف‌شده علامت می‌زند.</summary>
    public void MarkReservationConsumed() => ReservationConsumed = true;
}

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
            FulfillmentId = Guid.NewGuid(),
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
            unit._items.Add(FulfillmentItem.Create(unit.FulfillmentId, line.OrderLineId, line.Quantity, line.ReservationId));
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

        throw new InvalidOperationException("انتقال به Processing از این وضعیت مجاز نیست.");
    }

    /// <summary>پردازش انتخاب‌شده فقط همان خطوط را جلو می‌برد.</summary>
    public IReadOnlyList<(Guid OrderLineId, decimal Quantity)> ProcessSelections(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections,
        DateTimeOffset now)
    {
        EnsureNotTerminal();
        if (Status is FulfillmentStatus.Dispatched or FulfillmentStatus.InTransit or FulfillmentStatus.Delivered)
        {
            throw new InvalidOperationException("پردازش پس از ارسال مجاز نیست.");
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
        if (Status is FulfillmentStatus.Dispatched or FulfillmentStatus.InTransit or FulfillmentStatus.Delivered)
        {
            throw new InvalidOperationException("بسته‌بندی پس از ارسال مجاز نیست.");
        }

        var selections = _items
            .Select(item => (item.OrderLineId, Quantity: item.QuantityProcessing - item.QuantityPacked))
            .Where(x => x.Quantity > 0)
            .ToArray();
        if (selections.Length == 0)
        {
            throw new InvalidOperationException("fulfillment.pack.requires_processing");
        }

        PackSelections(selections, now);
    }

    /// <summary>بسته‌بندی انتخاب‌شده (خط/تعداد) روی همان مسیر canonical.</summary>
    public IReadOnlyList<(Guid OrderLineId, decimal Quantity)> PackSelections(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections,
        DateTimeOffset now)
    {
        EnsureNotTerminal();
        if (Status is FulfillmentStatus.Dispatched or FulfillmentStatus.InTransit or FulfillmentStatus.Delivered)
        {
            throw new InvalidOperationException("بسته‌بندی پس از ارسال مجاز نیست.");
        }

        var normalized = NormalizeSelections(selections);
        var affected = new List<(Guid OrderLineId, decimal Quantity)>(normalized.Count);
        foreach (var selection in normalized)
        {
            var item = RequireItem(selection.OrderLineId);
            if (item.QuantityPacked + selection.Quantity > item.QuantityOrdered)
            {
                throw new InvalidOperationException("تعداد بسته‌بندی از باقیمانده سفارش بیشتر است.");
            }

            if (item.QuantityProcessing < item.QuantityPacked + selection.Quantity)
            {
                throw new InvalidOperationException("fulfillment.pack.requires_processing");
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
                throw new InvalidOperationException(
                    "بازگشت از بسته‌بندی برای تعداد تخصیص‌یافته یا ارسال‌شده مجاز نیست.");
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
            throw new InvalidOperationException("ایجاد محموله در وضعیت پایانی مجاز نیست.");
        }

        var normalized = NormalizeSelections(items);
        var shipment = Shipment.Create(
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
    /// لغو کل سفارش: مرسوله‌های پیش از Dispatch باطل می‌شوند و واحد Cancelled می‌ماند.
    /// اگر قبلاً Cancelled باشد no-op است.
    /// </summary>
    public void AbortForOrderCancel(DateTimeOffset now)
    {
        if (Status == FulfillmentStatus.Cancelled)
        {
            return;
        }

        if (HasDispatchedQuantity())
        {
            throw new InvalidOperationException("fulfillment.cancel.already_dispatched");
        }

        foreach (var shipment in _shipments.Where(x => x.Status == ShipmentStatus.Created).ToList())
        {
            shipment.CancelPreDispatch(now);
            _domainEvents.Add(new ShipmentCancelledDomainEvent(FulfillmentId, shipment.ShipmentId, SellerOrderId));
        }

        Status = FulfillmentStatus.Cancelled;
        UpdatedAt = now;
    }

    /// <summary>
    /// بازگردانی rule-based: وضعیت انبار قبل از لغو برمی‌گردد؛ مرسوله‌های باطل‌شده زنده نمی‌شوند.
    /// </summary>
    public void ReactivateAfterOrderRestore(DateTimeOffset now)
    {
        if (Status != FulfillmentStatus.Cancelled)
        {
            return;
        }

        if (HasDispatchedQuantity())
        {
            throw new InvalidOperationException("fulfillment.restore.already_dispatched");
        }

        Status = FulfillmentStatus.ReadyToFulfill;
        RefreshWarehouseStatus(now);
    }

    /// <summary>آیا در این واحد quantity واقعی Dispatch شده است.</summary>
    public bool HasDispatchedQuantity() =>
        _items.Any(item => item.QuantityShipped > 0)
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
        ?? throw new InvalidOperationException("خط سفارش در این فروشنده پیدا نشد.");

    private static IReadOnlyList<(Guid OrderLineId, decimal Quantity)> NormalizeSelections(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections)
    {
        if (selections is null || selections.Count == 0)
        {
            throw new InvalidOperationException("انتخاب خط/تعداد الزامی است.");
        }

        var map = new Dictionary<Guid, decimal>();
        foreach (var selection in selections)
        {
            if (selection.Quantity <= 0)
            {
                throw new InvalidOperationException("تعداد باید بزرگ‌تر از صفر باشد.");
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
        shipment.EnsureDispatched();
        foreach (var item in shipment.Items)
        {
            var fulfillmentItem = _items.Single(x => x.OrderLineId == item.OrderLineId);
            fulfillmentItem.ApplyShippedQuantity(item.Quantity);
        }

        Status = AllItemsDelivered()
            ? FulfillmentStatus.Delivered
            : FulfillmentStatus.Dispatched;
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
        ?? throw new InvalidOperationException("محموله پیدا نشد.");

    private bool AllItemsDelivered() =>
        _items.Count > 0 && _items.All(x => x.QuantityShipped >= x.QuantityOrdered);

    private void EnsureNotTerminal()
    {
        if (Status is FulfillmentStatus.Cancelled or FulfillmentStatus.Failed)
        {
            throw new InvalidOperationException("وضعیت fulfillment پایانی است.");
        }
    }
}

/// <summary>
/// محموله fulfillment. چند محموله برای یک fulfillment مجاز است.
/// </summary>
public sealed class Shipment
{
    private readonly List<ShipmentItem> _items = [];

    private Shipment()
    {
    }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>fulfillment مالک.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>وضعیت محموله.</summary>
    public ShipmentStatus Status { get; private set; }

    /// <summary>نام نمایشی carrier.</summary>
    public string CarrierDisplayName { get; init; } = string.Empty;

    /// <summary>کد روش ارسال (رجیستری).</summary>
    public string ShippingMethodCode { get; init; } = string.Empty;

    /// <summary>برچسب روش ارسال.</summary>
    public string ShippingMethodLabel { get; init; } = string.Empty;

    /// <summary>متادیتای نرمال‌شده provider (بدون secret).</summary>
    public string? ProviderMetadataJson { get; init; }

    /// <summary>نسخهٔ schema متادیتا.</summary>
    public int ProviderMetadataVersion { get; init; }

    /// <summary>کد/مرجع ردیابی.</summary>
    public string? TrackingReference { get; private set; }

    /// <summary>کد رهگیری قبلی پس از اصلاح پیش از dispatch.</summary>
    public string? PreviousTrackingReference { get; private set; }

    /// <summary>زمان dispatch.</summary>
    public DateTimeOffset? DispatchedAt { get; private set; }

    /// <summary>زمان تحویل.</summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>خطوط محموله.</summary>
    public IReadOnlyList<ShipmentItem> Items => _items;

    internal static Shipment Create(
        Guid fulfillmentId,
        string carrierDisplayName,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> items,
        IReadOnlyList<FulfillmentItem> fulfillmentItems,
        IReadOnlyList<Shipment> existingShipments,
        DateTimeOffset now,
        string? shippingMethodCode = null,
        string? shippingMethodLabel = null,
        string? providerMetadataJson = null,
        int providerMetadataVersion = 0)
    {
        if (string.IsNullOrWhiteSpace(carrierDisplayName))
        {
            throw new InvalidOperationException("نام carrier الزامی است.");
        }

        var shipment = new Shipment
        {
            ShipmentId = Guid.NewGuid(),
            FulfillmentId = fulfillmentId,
            Status = ShipmentStatus.Created,
            CarrierDisplayName = carrierDisplayName.Trim(),
            ShippingMethodCode = (shippingMethodCode ?? string.Empty).Trim(),
            ShippingMethodLabel = string.IsNullOrWhiteSpace(shippingMethodLabel)
                ? carrierDisplayName.Trim()
                : shippingMethodLabel.Trim(),
            ProviderMetadataJson = string.IsNullOrWhiteSpace(providerMetadataJson) ? null : providerMetadataJson.Trim(),
            ProviderMetadataVersion = providerMetadataVersion,
            CreatedAt = now,
        };
        foreach (var item in items)
        {
            var remaining = fulfillmentItems.Single(x => x.OrderLineId == item.OrderLineId);
            var openAllocated = existingShipments
                .Where(s => s.Status == ShipmentStatus.Created)
                .SelectMany(s => s.Items)
                .Where(x => x.OrderLineId == item.OrderLineId)
                .Sum(x => x.Quantity);
            var pendingShipmentQty = shipment._items.Where(x => x.OrderLineId == item.OrderLineId).Sum(x => x.Quantity);
            if (remaining.QuantityPacked < remaining.QuantityShipped + openAllocated + pendingShipmentQty + item.Quantity)
            {
                throw new InvalidOperationException("تعداد محموله از باقیمانده بسته‌بندی‌شده بیشتر است.");
            }

            if (remaining.QuantityShipped + openAllocated + pendingShipmentQty + item.Quantity > remaining.QuantityOrdered)
            {
                throw new InvalidOperationException("تعداد محموله از باقیمانده سفارش بیشتر است.");
            }

            shipment._items.Add(ShipmentItem.Create(shipment.ShipmentId, item.OrderLineId, item.Quantity));
        }

        return shipment;
    }

    /// <summary>خطوط محموله بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedItems(IEnumerable<ShipmentItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    internal void AssignTracking(string trackingReference, DateTimeOffset now)
    {
        var normalized = trackingReference.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("مرجع ردیابی الزامی است.");
        }

        if (TrackingReference is not null
            && !string.Equals(TrackingReference, normalized, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("مرجع ردیابی قبلاً ثبت شده و قابل بازنویسی نیست.");
        }

        TrackingReference = normalized;
        _ = now;
    }

    internal void CorrectTracking(string trackingReference, DateTimeOffset now)
    {
        if (Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered
            || DispatchedAt is not null)
        {
            throw new InvalidOperationException("fulfillment.tracking.locked_after_dispatch");
        }

        if (Status != ShipmentStatus.Created)
        {
            throw new InvalidOperationException("fulfillment.tracking.invalid_state");
        }

        var normalized = trackingReference.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("مرجع ردیابی الزامی است.");
        }

        if (string.IsNullOrWhiteSpace(TrackingReference))
        {
            throw new InvalidOperationException("fulfillment.tracking.nothing_to_correct");
        }

        if (string.Equals(TrackingReference, normalized, StringComparison.Ordinal))
        {
            return;
        }

        PreviousTrackingReference = TrackingReference;
        TrackingReference = normalized;
        _ = now;
    }

    internal void EnsureDispatched()
    {
        if (Status == ShipmentStatus.Dispatched || Status == ShipmentStatus.InTransit || Status == ShipmentStatus.Delivered)
        {
            return;
        }

        if (Status != ShipmentStatus.Created)
        {
            throw new InvalidOperationException("dispatch از این وضعیت مجاز نیست.");
        }

        if (string.IsNullOrWhiteSpace(TrackingReference))
        {
            throw new InvalidOperationException("dispatch بدون tracking مجاز نیست.");
        }

        Status = ShipmentStatus.Dispatched;
        DispatchedAt = DateTimeOffset.UtcNow;
    }

    internal void EnsureDelivered(DateTimeOffset now)
    {
        if (Status == ShipmentStatus.Delivered)
        {
            return;
        }

        if (Status is not (ShipmentStatus.Dispatched or ShipmentStatus.InTransit))
        {
            throw new InvalidOperationException("تحویل از این وضعیت مجاز نیست.");
        }

        Status = ShipmentStatus.Delivered;
        DeliveredAt = now;
    }

    internal void CancelPreDispatch(DateTimeOffset now)
    {
        if (Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException("ابطال مرسوله پس از ارسال مجاز نیست.");
        }

        if (Status == ShipmentStatus.Cancelled)
        {
            return;
        }

        if (Status != ShipmentStatus.Created)
        {
            throw new InvalidOperationException("ابطال مرسوله از این وضعیت مجاز نیست.");
        }

        Status = ShipmentStatus.Cancelled;
        _ = now;
    }
}

/// <summary>
/// خط محموله.
/// </summary>
public sealed class ShipmentItem
{
    private ShipmentItem()
    {
    }

    /// <summary>شناسه خط محموله.</summary>
    public Guid ShipmentItemId { get; init; }

    /// <summary>شناسه shipment.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>خط سفارش.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>تعداد.</summary>
    public decimal Quantity { get; init; }

    internal static ShipmentItem Create(Guid shipmentId, Guid orderLineId, decimal quantity) =>
        new()
        {
            ShipmentItemId = Guid.NewGuid(),
            ShipmentId = shipmentId,
            OrderLineId = orderLineId,
            Quantity = quantity,
        };
}

/// <summary>رویداد ایجاد fulfillment.</summary>
public sealed class FulfillmentCreatedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public FulfillmentCreatedDomainEvent(Guid fulfillmentId, Guid sellerOrderId, Guid checkoutId)
    {
        FulfillmentId = fulfillmentId;
        SellerOrderId = sellerOrderId;
        CheckoutId = checkoutId;
        Metadata = EventMetadataFactory.ForDomain("fulfillment.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; }
}

/// <summary>رویداد بسته‌بندی خط/تعداد.</summary>
public sealed class FulfillmentLinePackedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public FulfillmentLinePackedDomainEvent(Guid fulfillmentId, Guid sellerOrderId, Guid orderLineId, decimal quantity)
    {
        FulfillmentId = fulfillmentId;
        SellerOrderId = sellerOrderId;
        OrderLineId = orderLineId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("fulfillment.line.packed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>خط سفارش.</summary>
    public Guid OrderLineId { get; }

    /// <summary>تعداد بسته‌بندی‌شده.</summary>
    public decimal Quantity { get; }
}

/// <summary>رویداد بازگشت از بسته‌بندی.</summary>
public sealed class FulfillmentLineUnpackedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public FulfillmentLineUnpackedDomainEvent(Guid fulfillmentId, Guid sellerOrderId, Guid orderLineId, decimal quantity)
    {
        FulfillmentId = fulfillmentId;
        SellerOrderId = sellerOrderId;
        OrderLineId = orderLineId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("fulfillment.line.unpacked.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>خط سفارش.</summary>
    public Guid OrderLineId { get; }

    /// <summary>تعداد بازگشتی از بسته‌بندی.</summary>
    public decimal Quantity { get; }
}

/// <summary>رویداد ایجاد محموله.</summary>
public sealed class ShipmentCreatedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ShipmentCreatedDomainEvent(Guid fulfillmentId, Guid shipmentId, Guid sellerOrderId)
    {
        FulfillmentId = fulfillmentId;
        ShipmentId = shipmentId;
        SellerOrderId = sellerOrderId;
        Metadata = EventMetadataFactory.ForDomain("shipment.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }
}

/// <summary>رویداد اصلاح کد رهگیری پیش از ارسال.</summary>
public sealed class ShipmentTrackingCorrectedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ShipmentTrackingCorrectedDomainEvent(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid sellerOrderId,
        string? previousTrackingReference,
        string? trackingReference)
    {
        FulfillmentId = fulfillmentId;
        ShipmentId = shipmentId;
        SellerOrderId = sellerOrderId;
        PreviousTrackingReference = previousTrackingReference;
        TrackingReference = trackingReference;
        Metadata = EventMetadataFactory.ForDomain("shipment.tracking.corrected.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>کد رهگیری قبلی.</summary>
    public string? PreviousTrackingReference { get; }

    /// <summary>کد رهگیری جدید.</summary>
    public string? TrackingReference { get; }
}

/// <summary>رویداد ابطال مرسوله پیش از ارسال.</summary>
public sealed class ShipmentCancelledDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ShipmentCancelledDomainEvent(Guid fulfillmentId, Guid shipmentId, Guid sellerOrderId)
    {
        FulfillmentId = fulfillmentId;
        ShipmentId = shipmentId;
        SellerOrderId = sellerOrderId;
        Metadata = EventMetadataFactory.ForDomain("shipment.cancelled.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }
}

/// <summary>رویداد dispatch محموله.</summary>
public sealed class ShipmentDispatchedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ShipmentDispatchedDomainEvent(Guid fulfillmentId, Guid shipmentId, Guid sellerOrderId)
    {
        FulfillmentId = fulfillmentId;
        ShipmentId = shipmentId;
        SellerOrderId = sellerOrderId;
        Metadata = EventMetadataFactory.ForDomain("shipment.dispatched.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }
}

/// <summary>رویداد تحویل محموله.</summary>
public sealed class ShipmentDeliveredDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ShipmentDeliveredDomainEvent(Guid fulfillmentId, Guid shipmentId, Guid sellerOrderId)
    {
        FulfillmentId = fulfillmentId;
        ShipmentId = shipmentId;
        SellerOrderId = sellerOrderId;
        Metadata = EventMetadataFactory.ForDomain("shipment.delivered.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }
}
