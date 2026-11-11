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
    private readonly List<FulfillmentShippingGroup> _shippingGroups = [];
    private readonly List<ShipmentReturnReceipt> _returnReceipts = [];
    private readonly List<DamagedGoodsResolution> _damagedResolutions = [];

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

    /// <summary>گروه‌های ارسال پذیرفته‌شدهٔ این واحد؛ خالی برای سفارش‌های قدیمی.</summary>
    public IReadOnlyList<FulfillmentShippingGroup> ShippingGroups => _shippingGroups;

    /// <summary>TB-258: origin receipts of returned parcels of this unit.</summary>
    public IReadOnlyList<ShipmentReturnReceipt> ReturnReceipts => _returnReceipts;

    /// <summary>TB-262: durable damaged-goods decisions of this unit.</summary>
    public IReadOnlyList<DamagedGoodsResolution> DamagedResolutions => _damagedResolutions;

    /// <summary>آیا سفارش این واحد گروه‌بندی ارسال پذیرفته‌شده دارد.</summary>
    public bool HasShippingGroups => _shippingGroups.Count > 0;

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
        DateTimeOffset now,
        IReadOnlyList<FulfillmentShippingGroupPlan>? shippingGroups = null)
    {
        var lineArray = lines as IReadOnlyList<(Guid OrderLineId, decimal Quantity, Guid? ReservationId)>
            ?? lines.ToArray();
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

        var membership = ResolveShippingGroupMembership(
            lineArray,
            shippingGroups,
            fulfillmentId,
            sellerOrderId,
            sellerPartyId,
            unit._shippingGroups);

        foreach (var line in lineArray)
        {
            membership.TryGetValue(line.OrderLineId, out var shippingGroupId);
            unit._items.Add(FulfillmentItem.Create(
                newId(),
                unit.FulfillmentId,
                line.OrderLineId,
                line.Quantity,
                line.ReservationId,
                shippingGroupId == Guid.Empty ? null : shippingGroupId));
        }

        unit._domainEvents.Add(new FulfillmentCreatedDomainEvent(unit.FulfillmentId, unit.SellerOrderId, unit.CheckoutId));
        return unit;
    }

    /// <summary>
    /// عضویت گروه ارسال خطوط را از snapshot پذیرفته‌شدهٔ سفارش می‌سازد.
    /// هر خط دقیقاً یک گروه دارد؛ فروشندهٔ هر گروه باید فروشندهٔ این SellerOrder باشد.
    /// سفارش بدون گروه‌بندی (legacy) نقشهٔ خالی برمی‌گرداند.
    /// </summary>
    private static IReadOnlyDictionary<Guid, Guid> ResolveShippingGroupMembership(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity, Guid? ReservationId)> lines,
        IReadOnlyList<FulfillmentShippingGroupPlan>? shippingGroups,
        Guid fulfillmentId,
        Guid sellerOrderId,
        Guid sellerPartyId,
        List<FulfillmentShippingGroup> target)
    {
        if (shippingGroups is null || shippingGroups.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var orderedLineIds = lines.Select(x => x.OrderLineId).ToHashSet();
        var membership = new Dictionary<Guid, Guid>();
        var seenGroups = new HashSet<Guid>();
        foreach (var plan in shippingGroups)
        {
            var group = FulfillmentShippingGroup.Create(plan, fulfillmentId, sellerOrderId, sellerPartyId);
            if (!seenGroups.Add(group.ShippingGroupId))
            {
                throw new ContractOperationException(FulfillmentShippingGroupFault.GroupDuplicate);
            }

            foreach (var orderLineId in plan.OrderLineIds)
            {
                if (!orderedLineIds.Contains(orderLineId))
                {
                    throw new ContractOperationException(FulfillmentShippingGroupFault.LineUnknown);
                }

                if (!membership.TryAdd(orderLineId, group.ShippingGroupId))
                {
                    throw new ContractOperationException(FulfillmentShippingGroupFault.LineInMultipleGroups);
                }
            }

            target.Add(group);
        }

        if (membership.Count != orderedLineIds.Count)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.LineUncovered);
        }

        return membership;
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

    /// <summary>گروه‌های ارسال بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedShippingGroups(IEnumerable<FulfillmentShippingGroup> shippingGroups)
    {
        _shippingGroups.Clear();
        _shippingGroups.AddRange(shippingGroups);
    }

    /// <summary>TB-258: رسیدهای دریافت‌شدهٔ بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedReturnReceipts(IEnumerable<ShipmentReturnReceipt> receipts)
    {
        _returnReceipts.Clear();
        _returnReceipts.AddRange(receipts);
    }

    /// <summary>TB-262: تصمیم‌های آسیب‌دیدگی بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedDamagedResolutions(IEnumerable<DamagedGoodsResolution> resolutions)
    {
        _damagedResolutions.Clear();
        _damagedResolutions.AddRange(resolutions);
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
            int providerMetadataVersion = 0,
            Guid? shippingGroupId = null,
            string? shippingDispatchKey = null)
    {
        EnsureNotTerminal();
        if (Status is FulfillmentStatus.Cancelled or FulfillmentStatus.Failed or FulfillmentStatus.Delivered)
        {
            throw new ContractOperationException("fulfillment.shipment.create_terminal");
        }

        var normalized = NormalizeSelections(items);
        EnsureShippingGroupScope(normalized, shippingGroupId);
        EnsureShippingGroupMethod(shippingGroupId, shippingMethodCode);
        var normalizedKey = string.IsNullOrWhiteSpace(shippingDispatchKey) ? null : shippingDispatchKey.Trim();
        if (normalizedKey is not null)
        {
            var existing = _shipments.FirstOrDefault(s =>
                s.Status == ShipmentStatus.Created
                && string.Equals(s.ShippingDispatchKey, normalizedKey, StringComparison.Ordinal));
            if (existing is not null)
            {
                EnsureDispatchKeyLinesMatch(existing, normalized);
                return existing;
            }
        }

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
            providerMetadataVersion,
            shippingGroupId,
            normalizedKey);
        _shipments.Add(shipment);
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentCreatedDomainEvent(FulfillmentId, shipment.ShipmentId, SellerOrderId));
        return shipment;
    }

    private static void EnsureDispatchKeyLinesMatch(
        Shipment existing,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections)
    {
        if (existing.Items.Count != selections.Count)
        {
            throw new ContractOperationException("fulfillment.shipment.dispatch_key_conflict");
        }

        foreach (var selection in selections)
        {
            if (!existing.Items.Any(i => i.OrderLineId == selection.OrderLineId && i.Quantity == selection.Quantity))
            {
                throw new ContractOperationException("fulfillment.shipment.dispatch_key_conflict");
            }
        }
    }

    /// <summary>
    /// دامنهٔ گروه محموله را اعتبارسنجی می‌کند: سفارش گروه‌دار محمولهٔ گروه‌دار می‌خواهد،
    /// گروه باید در همین واحد باشد و هیچ خطی از گروه خودش جدا نشود.
    /// </summary>
    private void EnsureShippingGroupScope(
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> selections,
        Guid? shippingGroupId)
    {
        if (shippingGroupId == Guid.Empty)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.IdentityRequired);
        }

        if (!HasShippingGroups)
        {
            if (shippingGroupId is not null)
            {
                throw new ContractOperationException(FulfillmentShippingGroupFault.GroupNotApplicable);
            }

            return;
        }

        if (shippingGroupId is null)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.ShipmentGroupRequired);
        }

        if (_shippingGroups.All(x => x.ShippingGroupId != shippingGroupId.Value))
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.GroupNotFound);
        }

        foreach (var selection in selections)
        {
            var item = _items.SingleOrDefault(x => x.OrderLineId == selection.OrderLineId)
                ?? throw new ContractOperationException("fulfillment.order_line.not_found");
            var membership = item.ShippingGroupId ?? Guid.Empty;
            if (membership != shippingGroupId.Value)
            {
                throw new ContractOperationException(FulfillmentShippingGroupFault.ShipmentGroupMixed);
            }
        }
    }

    private void EnsureShippingGroupMethod(Guid? shippingGroupId, string? shippingMethodCode)
    {
        if (shippingGroupId is null || string.IsNullOrWhiteSpace(shippingMethodCode))
        {
            return;
        }

        var group = _shippingGroups.FirstOrDefault(x => x.ShippingGroupId == shippingGroupId.Value);
        if (group is null)
        {
            return;
        }

        // Captured checkout may persist method as "post\u001f" / "post\u001fstandard" (service+option
        // composite). CreateShipment registry code is the head token ("post"). Compare after stripping
        // both unit-separator and colon-prefixed catalogue forms.
        var offered = NormalizeShippingMethodIdentity(shippingMethodCode);
        var accepted = NormalizeShippingMethodIdentity(group.MethodCode);
        if (accepted.Length == 0)
        {
            return;
        }

        if (!string.Equals(offered, accepted, StringComparison.OrdinalIgnoreCase))
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.ShipmentMethodMismatch);
        }
    }

    /// <summary>Head method token from registry, colon catalogue, or captured service\\u001foption forms.</summary>
    private static string NormalizeShippingMethodIdentity(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var value = raw.Trim().Split('\u001f', 2)[0].Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        return value.Split(':', 2)[0].Trim();
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

    /// <summary>
    /// تعداد تخصیص باز در مرسوله‌های Created. TB-258: a Created resend parcel is funded by returned capacity, not
    /// by the packed remainder, so it is never counted here (otherwise it would shrink an unrelated fresh remainder).
    /// </summary>
    public decimal OpenAllocatedQuantity(Guid orderLineId) =>
        _shipments
            .Where(s => s.Status == ShipmentStatus.Created && !s.IsReturnFunded)
            .SelectMany(s => s.Items)
            .Where(x => x.OrderLineId == orderLineId)
            .Sum(x => x.Quantity);

    /// <summary>TB-258: quantity of one line actually delivered to the customer (final, derived from parcels).</summary>
    public decimal DeliveredQuantity(Guid orderLineId) =>
        _shipments
            .Where(s => s.Status == ShipmentStatus.Delivered)
            .SelectMany(s => s.Items)
            .Where(x => x.OrderLineId == orderLineId)
            .Sum(x => x.Quantity);

    /// <summary>
    /// TB-258-R1: physically received USABLE quantity of ONE source parcel's own origin receipt. Capacity belongs
    /// to the parcel that physically came back, so units received from parcel A can never fund a resend sourced
    /// from parcel B. Damaged units and non-delivery records contribute nothing.
    /// </summary>
    public decimal ReturnedUsableQuantity(Guid sourceShipmentId, Guid orderLineId) =>
        _returnReceipts
            .Where(r => r.ShipmentId == sourceShipmentId)
            .SelectMany(r => r.Items)
            .Where(x => x.OrderLineId == orderLineId && x.Condition == ReturnReceiptCondition.Usable)
            .Sum(x => x.Quantity);

    /// <summary>
    /// TB-258-R1: capacity of ONE source parcel already committed to its OWN non-cancelled resend parcels. A
    /// cancelled pre-dispatch resend releases only this source's capacity once; a dispatched/delivered resend
    /// keeps it consumed. A resend of a resend commits against its immediate source, so a returned resend funds
    /// exactly one next cycle.
    /// </summary>
    public decimal ResendCommittedQuantity(Guid sourceShipmentId, Guid orderLineId) =>
        _shipments
            .Where(s => s.IsResend
                && s.Status != ShipmentStatus.Cancelled
                && s.ResendOfShipmentId == sourceShipmentId)
            .SelectMany(s => s.Items)
            .Where(x => x.OrderLineId == orderLineId)
            .Sum(x => x.Quantity);

    /// <summary>TB-258-R1: remaining capacity of ONE source parcel available for a new resend of that line.</summary>
    public decimal ResendableQuantity(Guid sourceShipmentId, Guid orderLineId)
    {
        var remaining = ReturnedUsableQuantity(sourceShipmentId, orderLineId)
            - ResendCommittedQuantity(sourceShipmentId, orderLineId);
        return remaining > 0m ? remaining : 0m;
    }

    /// <summary>
    /// TB-258: physically received USABLE quantity of one line across every origin receipt of this unit. Damaged
    /// receipts contribute nothing and a non-delivery alone contributes nothing.
    /// </summary>
    public decimal ReturnedUsableQuantity(Guid orderLineId) =>
        _returnReceipts
            .SelectMany(r => r.Items)
            .Where(x => x.OrderLineId == orderLineId && x.Condition == ReturnReceiptCondition.Usable)
            .Sum(x => x.Quantity);

    /// <summary>
    /// TB-258: returned capacity already committed to resend parcels that were not cancelled. A cancelled
    /// pre-dispatch resend releases its own source capacity once; a dispatched/delivered resend keeps it consumed.
    /// </summary>
    public decimal ResendCommittedQuantity(Guid orderLineId) =>
        _shipments
            .Where(s => s.IsResend && s.Status != ShipmentStatus.Cancelled)
            .SelectMany(s => s.Items)
            .Where(x => x.OrderLineId == orderLineId)
            .Sum(x => x.Quantity);

    /// <summary>TB-258: remaining returned capacity of one line available for a new resend parcel.</summary>
    public decimal ResendableQuantity(Guid orderLineId)
    {
        var remaining = ReturnedUsableQuantity(orderLineId) - ResendCommittedQuantity(orderLineId);
        return remaining > 0m ? remaining : 0m;
    }

    /// <summary>TB-258: true when at least one line still carries resendable returned capacity.</summary>
    public bool HasResendableQuantity() => _items.Any(x => ResendableQuantity(x.OrderLineId) > 0m);

    /// <summary>TB-258-R1: true when ONE returned source parcel still carries resendable capacity.</summary>
    public bool HasResendableQuantity(Guid sourceShipmentId)
    {
        var source = _shipments.SingleOrDefault(x => x.ShipmentId == sourceShipmentId);
        return source is not null && source.Items.Any(x => ResendableQuantity(sourceShipmentId, x.OrderLineId) > 0m);
    }

    // ---------------------------------------------------------------------------------------------
    // TB-262: damaged-goods capacity and durable decisions.
    //
    // DAMAGED units are the exact complement of usable capacity: they are physically received, they never fund a
    // resend, they never raise saleable stock (the original sale already consumed them) and they are not a reason to
    // decrement stock a second time. Each damaged unit is resolved by exactly ONE durable decision so one unit can
    // never fund both a replacement and a refund/another replacement.
    // ---------------------------------------------------------------------------------------------

    /// <summary>TB-262: physically received DAMAGED quantity of ONE source parcel line.</summary>
    public decimal ReceiptDamagedQuantity(Guid sourceShipmentId, Guid orderLineId) =>
        _returnReceipts
            .Where(r => r.ShipmentId == sourceShipmentId)
            .SelectMany(r => r.Items)
            .Where(x => x.OrderLineId == orderLineId && x.Condition == ReturnReceiptCondition.Damaged)
            .Sum(x => x.Quantity);

    /// <summary>
    /// TB-262: damaged quantity of ONE source parcel line already bound to a non-cancelled decision. A cancelled
    /// decision releases its units back to unresolved; a recorded/replacement decision keeps them committed.
    /// </summary>
    public decimal DamagedCommittedQuantity(Guid sourceShipmentId, Guid orderLineId) =>
        _damagedResolutions
            .Where(r => r.ShipmentId == sourceShipmentId
                && r.OrderLineId == orderLineId
                && r.Status != DamagedResolutionStatus.Cancelled)
            .Sum(r => r.Quantity);

    /// <summary>TB-262: damaged quantity of ONE source parcel line still awaiting a decision.</summary>
    public decimal OutstandingDamagedQuantity(Guid sourceShipmentId, Guid orderLineId)
    {
        var remaining = ReceiptDamagedQuantity(sourceShipmentId, orderLineId)
            - DamagedCommittedQuantity(sourceShipmentId, orderLineId);
        return remaining > 0m ? remaining : 0m;
    }

    /// <summary>TB-262: outstanding damaged quantity of one line across every source parcel of this unit.</summary>
    public decimal OutstandingDamagedQuantity(Guid orderLineId) =>
        _returnReceipts
            .Select(r => r.ShipmentId)
            .Distinct()
            .Sum(shipmentId => OutstandingDamagedQuantity(shipmentId, orderLineId));

    /// <summary>TB-262: true when at least one returned parcel still owes a damaged-goods decision.</summary>
    public bool HasOutstandingDamagedQuantity(Guid sourceShipmentId) =>
        _returnReceipts
            .Where(r => r.ShipmentId == sourceShipmentId)
            .SelectMany(r => r.Items)
            .Any(x => x.Condition == ReturnReceiptCondition.Damaged
                && OutstandingDamagedQuantity(sourceShipmentId, x.OrderLineId) > 0m);

    /// <summary>TB-262: the durable decision with this identity, if any.</summary>
    public DamagedGoodsResolution? FindDamagedResolution(Guid resolutionId) =>
        _damagedResolutions.SingleOrDefault(x => x.DamagedGoodsResolutionId == resolutionId);

    /// <summary>
    /// TB-262: records the operator's reviewed decision for a damaged quantity of ONE source parcel line.
    ///
    /// A recorded occurrence answers only the EXACT same intent (source parcel, line, quantity, outcome): the retry
    /// after a lost response returns the recorded decision, a changed intent is a typed conflict with zero writes,
    /// and the recorded decision is never silently re-attributed or re-created. The quantity is bounded by THIS
    /// source parcel's own outstanding damaged quantity, so a sibling parcel's damaged units can never be borrowed.
    /// </summary>
    public DamagedGoodsResolution RecordDamagedResolution(
        Guid resolutionId,
        Guid shipmentId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome,
        string? occurrenceKey,
        Guid actorUserId,
        string? note,
        DateTimeOffset now)
    {
        EnsureNotTerminal();

        var key = string.IsNullOrWhiteSpace(occurrenceKey) ? null : occurrenceKey.Trim();
        if (key is null)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_occurrence_required");
        }

        var recorded = _damagedResolutions.FirstOrDefault(x =>
            string.Equals(x.OccurrenceKey, key, StringComparison.Ordinal));
        if (recorded is not null)
        {
            if (!recorded.AnswersIntent(shipmentId, orderLineId, quantity, outcome))
            {
                throw new ContractOperationException("fulfillment.damage.resolution_intent_conflict");
            }

            return recorded;
        }

        var shipment = RequireShipment(shipmentId);
        if (shipment.Status != ShipmentStatus.Returned)
        {
            throw new ContractOperationException("fulfillment.damage.source_not_returned");
        }

        var receipt = _returnReceipts.SingleOrDefault(x => x.ShipmentId == shipmentId)
            ?? throw new ContractOperationException("fulfillment.damage.receipt_missing");

        if (!shipment.Items.Any(x => x.OrderLineId == orderLineId))
        {
            throw new ContractOperationException("fulfillment.damage.line_foreign");
        }

        if (ReceiptDamagedQuantity(shipmentId, orderLineId) <= 0m)
        {
            throw new ContractOperationException("fulfillment.damage.no_damaged_quantity");
        }

        if (quantity > OutstandingDamagedQuantity(shipmentId, orderLineId))
        {
            throw new ContractOperationException("fulfillment.damage.exceeds_outstanding");
        }

        // The customer must still be owed these units: a decision can never manufacture an obligation that the paid
        // order no longer carries (ordered quantity already fully delivered to the customer).
        var item = RequireItem(orderLineId);
        var stillOwed = item.QuantityOrdered - DeliveredQuantity(orderLineId);
        if (quantity > stillOwed)
        {
            throw new ContractOperationException("fulfillment.damage.obligation_already_settled");
        }

        var resolution = DamagedGoodsResolution.Record(
            resolutionId,
            FulfillmentId,
            shipmentId,
            receipt.ShipmentReturnReceiptId,
            orderLineId,
            quantity,
            outcome,
            key,
            actorUserId,
            note,
            now);
        _damagedResolutions.Add(resolution);
        UpdatedAt = now;
        return resolution;
    }

    /// <summary>TB-262: links the replacement's OWN healthy-stock reservation to its decision.</summary>
    public void AttachDamagedResolutionReservation(Guid resolutionId, Guid reservationId, DateTimeOffset now)
    {
        var resolution = FindDamagedResolution(resolutionId)
            ?? throw new ContractOperationException("fulfillment.damage.resolution_not_found");
        resolution.AttachReservation(reservationId, now);
        UpdatedAt = now;
    }

    /// <summary>
    /// TB-262: creates the linked replacement parcel for an ALREADY reserved damaged-goods decision. The parcel is
    /// funded by the decision's own new reservation, keeps the accepted group/method of the damaged source and never
    /// touches returned capacity, the packed remainder or the original sale reservation.
    /// </summary>
    public Shipment CreateReplacementShipment(
        Guid shipmentId,
        Func<Guid> newId,
        Guid resolutionId,
        Guid actorUserId,
        string carrierDisplayName,
        DateTimeOffset now)
    {
        EnsureNotTerminal();

        var resolution = FindDamagedResolution(resolutionId)
            ?? throw new ContractOperationException("fulfillment.damage.resolution_not_found");
        if (resolution.Outcome != DamagedResolutionOutcome.Replacement)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_not_replacement");
        }

        if (resolution.Status == DamagedResolutionStatus.Cancelled)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_cancelled");
        }

        if (resolution.ReplacementShipmentId is { } existingId)
        {
            return _shipments.SingleOrDefault(x => x.ShipmentId == existingId)
                ?? throw new ContractOperationException("fulfillment.damage.replacement_missing");
        }

        if (resolution.ReplacementReservationId is not { } reservationId)
        {
            throw new ContractOperationException("fulfillment.damage.reservation_required");
        }

        var original = RequireShipment(resolution.ShipmentId);
        var items = new[] { (resolution.OrderLineId, resolution.Quantity) };

        // The parcel's dispatch key stays bound to the reviewed occurrence, but it is ATTEMPT-scoped: an exact replay
        // of the same occurrence keeps its key (so the recorded parcel is reused), while a retry after an explicit
        // pre-dispatch cancellation is genuinely a NEW parcel and must never collide with the cancelled one.
        var dispatchKey = resolution.Attempt == 1
            ? resolution.OccurrenceKey
            : $"attempt{resolution.Attempt}:{resolution.DamagedGoodsResolutionId:N}";

        var shipment = Shipment.CreateReplacement(
            shipmentId,
            newId,
            FulfillmentId,
            original.ShipmentId,
            resolution.DamagedGoodsResolutionId,
            reservationId,
            string.IsNullOrWhiteSpace(carrierDisplayName) ? original.CarrierDisplayName : carrierDisplayName,
            items,
            now,
            original.ShippingMethodCode,
            original.ShippingMethodLabel,
            original.ShippingGroupId,
            dispatchKey,
            actorUserId);

        _shipments.Add(shipment);
        resolution.MarkFulfilled(shipment.ShipmentId, now);
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentCreatedDomainEvent(FulfillmentId, shipment.ShipmentId, SellerOrderId));
        return shipment;
    }

    /// <summary>
    /// TB-262: cancelling a replacement before dispatch releases ONLY that parcel allocation. RETENTION RULE: the
    /// purpose-bound healthy-stock reservation stays attached to the ACTIVE ORDER, so nothing is released here and the
    /// next legitimate attempt rebinds the same held allocation. The damaged units stay non-saleable and unresolved.
    /// </summary>
    public void RetainReplacementStockForCancelledParcel(Guid replacementShipmentId, DateTimeOffset now)
    {
        var shipment = RequireShipment(replacementShipmentId);
        if (!shipment.IsReplacement || shipment.DamagedResolutionId is not { } resolutionId)
        {
            return;
        }

        var resolution = FindDamagedResolution(resolutionId);
        if (resolution is null || resolution.Status == DamagedResolutionStatus.Cancelled)
        {
            return;
        }

        resolution.RetainStockBeforeDispatch(now);
        UpdatedAt = now;
    }

    /// <summary>
    /// TB-262 RETENTION RULE: only the owning authorized ORDER cancellation releases the unconsumed healthy-stock
    /// reservations of this checkout's replacement decisions, exactly once. Returns the ids the caller must release.
    /// </summary>
    public IReadOnlyList<Guid> ReleaseReplacementStockForOrderCancel(DateTimeOffset now)
    {
        var released = new List<Guid>();
        foreach (var resolution in _damagedResolutions.ToList())
        {
            if (resolution.ReplacementReservationId is not { } reservationId
                || resolution.Status == DamagedResolutionStatus.Cancelled)
            {
                continue;
            }

            released.Add(reservationId);
            resolution.MarkOrderCancelled(now);
            UpdatedAt = now;
        }

        return released;
    }

    /// <summary>TB-262: read-only answer for one exact reviewed damaged-goods decision.</summary>
    public RecordedDamagedResolution ResolveRecordedDamagedResolution(
        string? occurrenceKey,
        Guid shipmentId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome)
    {
        var key = string.IsNullOrWhiteSpace(occurrenceKey) ? null : occurrenceKey.Trim();
        if (key is null)
        {
            return new RecordedDamagedResolution(RecordedDamagedResolutionState.NotRecorded, null);
        }

        var recorded = _damagedResolutions.FirstOrDefault(x =>
            string.Equals(x.OccurrenceKey, key, StringComparison.Ordinal));
        if (recorded is null)
        {
            return new RecordedDamagedResolution(RecordedDamagedResolutionState.NotRecorded, null);
        }

        if (!recorded.AnswersIntent(shipmentId, orderLineId, quantity, outcome))
        {
            return new RecordedDamagedResolution(RecordedDamagedResolutionState.Conflict, null);
        }

        return recorded.Status == DamagedResolutionStatus.Cancelled
            ? new RecordedDamagedResolution(RecordedDamagedResolutionState.ResolvedCancelled, recorded)
            : new RecordedDamagedResolution(RecordedDamagedResolutionState.Recorded, recorded);
    }

    /// <summary>TB-262: state of one recorded damaged-goods decision lookup.</summary>
    public enum RecordedDamagedResolutionState
    {
        /// <summary>No durable decision answers this exact intent.</summary>
        NotRecorded = 0,

        /// <summary>The exact reviewed decision is recorded.</summary>
        Recorded = 1,

        /// <summary>The exact reviewed decision was explicitly cancelled before dispatch.</summary>
        ResolvedCancelled = 2,

        /// <summary>A decision exists under this occurrence but answers a different intent.</summary>
        Conflict = 3,
    }

    /// <summary>TB-262: outcome of the read-only damaged-goods decision lookup.</summary>
    public sealed record RecordedDamagedResolution(
        RecordedDamagedResolutionState State,
        DamagedGoodsResolution? Resolution);

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
        var wasDispatched = shipment.DispatchedAt is not null;
        shipment.EnsureDispatched(now);

        // TB-258: a resend moves returned goods a second time for the same order line. It consumes returned
        // capacity (see ResendCommittedQuantity) and must never inflate the first-dispatch counter, otherwise the
        // lifetime shipped quantity would exceed the ordered quantity and every remaining-quantity reader breaks.
        // TB-262: a replacement parcel moves NEW healthy stock for units that already shipped once, so it must not
        // inflate that counter either.
        if (!shipment.IsReturnFunded)
        {
            foreach (var item in shipment.Items)
            {
                var fulfillmentItem = _items.Single(x => x.OrderLineId == item.OrderLineId);
                if (!wasDispatched)
                {
                    fulfillmentItem.ApplyShippedQuantity(item.Quantity);
                }
            }
        }

        Status = FulfillmentStatus.Dispatched;
        UpdatedAt = now;
        if (!wasDispatched)
        {
            _domainEvents.Add(new ShipmentDispatchedDomainEvent(FulfillmentId, shipmentId, SellerOrderId));
        }
    }

    /// <summary>محموله را Delivered علامت می‌زند.</summary>
    public void ApplyShipmentDelivered(Guid shipmentId, DateTimeOffset now)
    {
        var shipment = RequireShipment(shipmentId);
        shipment.EnsureDelivered(now);

        // TB-258: the unit is delivered only when every ordered unit was actually DELIVERED to the customer. A
        // merely dispatched, non-delivered, returning or resent-away parcel keeps the unit in transit.
        Status = AllItemsDelivered() ? FulfillmentStatus.Delivered : FulfillmentStatus.InTransit;
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentDeliveredDomainEvent(FulfillmentId, shipmentId, SellerOrderId));
    }

    /// <summary>TB-258: records a courier non-delivery attempt on a dispatched parcel.</summary>
    public void RecordShipmentNonDelivery(
        Guid shipmentId,
        string reason,
        string? note,
        Guid actorUserId,
        DateTimeOffset now)
    {
        var shipment = RequireShipment(shipmentId);
        shipment.RecordNonDelivery(reason, note, actorUserId, now);
        if (Status is not (FulfillmentStatus.Delivered or FulfillmentStatus.Cancelled or FulfillmentStatus.Failed))
        {
            Status = FulfillmentStatus.InTransit;
        }

        UpdatedAt = now;
        _domainEvents.Add(new ShipmentNonDeliveredDomainEvent(FulfillmentId, shipmentId, SellerOrderId, reason.Trim()));
    }

    /// <summary>TB-258: the carrier started bringing the failed parcel back to origin.</summary>
    public void StartShipmentReturn(Guid shipmentId, Guid actorUserId, DateTimeOffset now)
    {
        var shipment = RequireShipment(shipmentId);
        shipment.StartReturn(actorUserId, now);
        UpdatedAt = now;
    }

    /// <summary>
    /// TB-258: confirms the physical origin receipt of one returned parcel and records the exact per-line
    /// quantities with their condition. Only usable received quantity becomes resend capacity.
    /// </summary>
    public ShipmentReturnReceipt RecordReturnReceipt(
        Guid receiptId,
        Func<Guid> newId,
        Guid shipmentId,
        Guid actorUserId,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity, ReturnReceiptCondition Condition)> items,
        DateTimeOffset receivedAt,
        string? note,
        DateTimeOffset now)
    {
        EnsureNotTerminal();
        var shipment = RequireShipment(shipmentId);
        if (_returnReceipts.Any(x => x.ShipmentId == shipmentId))
        {
            throw new ContractOperationException("fulfillment.return.receipt_already_recorded");
        }

        if (shipment.Status == ShipmentStatus.Returned)
        {
            throw new ContractOperationException("fulfillment.return.receipt_already_recorded");
        }

        var receipt = ShipmentReturnReceipt.Create(
            receiptId,
            newId,
            shipmentId,
            FulfillmentId,
            actorUserId,
            receivedAt,
            now,
            note,
            items,
            shipment.Items);

        shipment.EnsureReturned(now);
        _returnReceipts.Add(receipt);
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentReturnedDomainEvent(
            FulfillmentId,
            shipmentId,
            SellerOrderId,
            receipt.ShipmentReturnReceiptId,
            receipt.Items.Where(x => x.Condition == ReturnReceiptCondition.Usable).Sum(x => x.Quantity)));
        return receipt;
    }

    /// <summary>
    /// TB-258-R1: creates a NEW parcel occurrence for goods physically received back at origin. The occurrence key
    /// is a prebound reviewed intent: replaying it returns the EXACT recorded parcel, while a changed source,
    /// group/method or quantity under the same key is a typed conflict instead of a different intent's parcel.
    /// Capacity is the physically received usable quantity of THIS source parcel, never another parcel's receipt.
    /// </summary>
    public Shipment CreateResendShipment(
        Guid shipmentId,
        Func<Guid> newId,
        Guid originalShipmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> items,
        DateTimeOffset now,
        string? shippingMethodCode,
        string? shippingMethodLabel,
        string? shippingDispatchKey)
    {
        EnsureNotTerminal();

        // A resend must carry a trustworthy occurrence: a blank key would make a retried request allocate again.
        var occurrenceKey = string.IsNullOrWhiteSpace(shippingDispatchKey) ? null : shippingDispatchKey.Trim();
        if (occurrenceKey is null)
        {
            throw new ContractOperationException("fulfillment.resend.occurrence_required");
        }

        var original = RequireShipment(originalShipmentId);
        if (original.Status != ShipmentStatus.Returned)
        {
            throw new ContractOperationException("fulfillment.resend.original_not_returned");
        }

        var normalized = NormalizeSelections(items);
        EnsureShippingGroupScope(normalized, original.ShippingGroupId);

        var effectiveMethodCode = string.IsNullOrWhiteSpace(shippingMethodCode)
            ? original.ShippingMethodCode
            : shippingMethodCode;
        EnsureShippingGroupMethod(original.ShippingGroupId, effectiveMethodCode);
        if (!string.Equals(
                NormalizeShippingMethodIdentity(original.ShippingMethodCode),
                NormalizeShippingMethodIdentity(effectiveMethodCode),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ContractOperationException("fulfillment.resend.method_mismatch");
        }

        // Owning replay BEFORE any capacity gate: the recorded parcel is the authority for this occurrence, so a
        // retry after the source units were already consumed still returns it, and a changed request never does.
        var recorded = _shipments.FirstOrDefault(s =>
            s.IsResend && string.Equals(s.ShippingDispatchKey, occurrenceKey, StringComparison.Ordinal));
        if (recorded is not null)
        {
            return ReplayRecordedResend(recorded, original, normalized, effectiveMethodCode);
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.resend.actor_required");
        }

        foreach (var line in normalized)
        {
            if (!original.Items.Any(x => x.OrderLineId == line.OrderLineId))
            {
                throw new ContractOperationException("fulfillment.resend.line_foreign");
            }

            // TB-258-R1: capacity belongs to THIS physically returned source parcel, so a sibling returned parcel's
            // units can never be borrowed to satisfy this source.
            if (line.Quantity > ResendableQuantity(original.ShipmentId, line.OrderLineId))
            {
                throw new ContractOperationException("fulfillment.resend.exceeds_returned");
            }
        }

        var shipment = Shipment.CreateResend(
            shipmentId,
            newId,
            FulfillmentId,
            original.ShipmentId,
            string.IsNullOrWhiteSpace(carrierDisplayName) ? original.CarrierDisplayName : carrierDisplayName,
            normalized,
            now,
            effectiveMethodCode,
            string.IsNullOrWhiteSpace(shippingMethodLabel) ? original.ShippingMethodLabel : shippingMethodLabel,
            original.ShippingGroupId,
            occurrenceKey,
            actorUserId);
        _shipments.Add(shipment);
        UpdatedAt = now;
        _domainEvents.Add(new ShipmentCreatedDomainEvent(FulfillmentId, shipment.ShipmentId, SellerOrderId));
        return shipment;
    }

    /// <summary>
    /// TB-258-R1/R3: an already recorded occurrence only answers the SAME reviewed intent. Identity is validated
    /// FIRST, so a different source parcel, group/method or quantity is a typed conflict (zero new writes) — never
    /// the old parcel under a new request, and never a "cancelled" answer for an intent that was never recorded.
    /// A cancelled occurrence is a RESULT STATE of an exact intent: it keeps its history and cannot be silently
    /// recreated by the same key.
    /// </summary>
    private static Shipment ReplayRecordedResend(
        Shipment recorded,
        Shipment original,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> requested,
        string effectiveMethodCode)
    {
        if (!RecordedResendAnswersIntent(recorded, original, requested, effectiveMethodCode))
        {
            throw new ContractOperationException("fulfillment.resend.intent_conflict");
        }

        if (recorded.Status == ShipmentStatus.Cancelled)
        {
            throw new ContractOperationException("fulfillment.resend.occurrence_cancelled");
        }

        return recorded;
    }

    /// <summary>
    /// TB-258-R2: ONE recorded-intent authority. A recorded occurrence answers a request only when its source
    /// parcel, accepted group/method and canonical per-line quantities are identical. Cancellation is a state,
    /// not an exemption: the caller decides what an identity match on a cancelled occurrence means.
    /// </summary>
    private static bool RecordedResendAnswersIntent(
        Shipment recorded,
        Shipment original,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> requested,
        string effectiveMethodCode)
    {
        var sameSource = recorded.ResendOfShipmentId == original.ShipmentId;
        var sameGroup = recorded.ShippingGroupId == original.ShippingGroupId;
        var sameMethod = string.Equals(
            NormalizeShippingMethodIdentity(recorded.ShippingMethodCode),
            NormalizeShippingMethodIdentity(effectiveMethodCode),
            StringComparison.OrdinalIgnoreCase);
        return sameSource && sameGroup && sameMethod && LinesMatch(recorded, requested);
    }

    // ---------------------------------------------------------------------------------------------
    // TB-258-R2: READ-ONLY exact recorded-intent resolution.
    //
    // The public admin command must answer an exact reviewed intent recovered after a lost response even when
    // the FRESH projection no longer offers that action. These resolvers answer only from durable facts of this
    // unit, never write, never consume capacity and never authorize anything: the caller still enforces the
    // current actor permission, the checkout/fulfillment/parcel relationship and (for a genuinely new command)
    // the current projection gate. When nothing is recorded, `NotRecorded` keeps the ordinary gate authoritative,
    // so an absent record can never turn a readiness failure into a new allocation.
    // ---------------------------------------------------------------------------------------------

    /// <summary>TB-258-R2: read-only outcome of an exact recorded post-dispatch intent lookup.</summary>
    public enum RecordedPostDispatchState
    {
        /// <summary>No durable fact answers this exact intent.</summary>
        NotRecorded = 0,

        /// <summary>The exact intent is already recorded; nothing may be written or repeated.</summary>
        Recorded = 1,

        /// <summary>A durable fact exists but answers a different intent (typed conflict, zero writes).</summary>
        Conflict = 2,

        /// <summary>A recorded occurrence exists and was cancelled; the retry resolves that history, writing nothing.</summary>
        ResolvedCancelled = 3,
    }

    /// <summary>TB-258-R2: exact recorded-intent lookup answer.</summary>
    public sealed record RecordedPostDispatchIntent(
        RecordedPostDispatchState State,
        Shipment? Shipment = null,
        string? ConflictCode = null);

    /// <summary>TB-258-R2: is this exact non-delivery attempt already the recorded operator fact of the parcel?</summary>
    public RecordedPostDispatchIntent ResolveRecordedNonDelivery(Guid shipmentId, string? reason, string? note)
    {
        var shipment = _shipments.FirstOrDefault(x => x.ShipmentId == shipmentId);
        if (shipment is null
            || shipment.Status is not (ShipmentStatus.NonDelivered or ShipmentStatus.Returning or ShipmentStatus.Returned))
        {
            return new RecordedPostDispatchIntent(RecordedPostDispatchState.NotRecorded);
        }

        var normalizedReason = (reason ?? string.Empty).Trim();
        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var sameFact = string.Equals(shipment.NonDeliveryReason, normalizedReason, StringComparison.Ordinal)
            && string.Equals(shipment.NonDeliveryNote, trimmedNote, StringComparison.Ordinal);
        return sameFact
            ? new RecordedPostDispatchIntent(RecordedPostDispatchState.Recorded, shipment)
            : new RecordedPostDispatchIntent(
                RecordedPostDispatchState.Conflict,
                null,
                "fulfillment.nondelivery.conflict");
    }

    /// <summary>TB-258-R2: has the physical return of this parcel already been started?</summary>
    public RecordedPostDispatchIntent ResolveRecordedReturnStart(Guid shipmentId)
    {
        var shipment = _shipments.FirstOrDefault(x => x.ShipmentId == shipmentId);
        return shipment is not null && shipment.Status is ShipmentStatus.Returning or ShipmentStatus.Returned
            ? new RecordedPostDispatchIntent(RecordedPostDispatchState.Recorded, shipment)
            : new RecordedPostDispatchIntent(RecordedPostDispatchState.NotRecorded);
    }

    /// <summary>TB-258-R2: is this exact origin receipt already recorded for the parcel?</summary>
    public RecordedPostDispatchIntent ResolveRecordedReturnReceipt(
        Guid shipmentId,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity, ReturnReceiptCondition Condition)>? items)
    {
        var shipment = _shipments.FirstOrDefault(x => x.ShipmentId == shipmentId);
        var recorded = _returnReceipts.FirstOrDefault(x => x.ShipmentId == shipmentId);
        if (recorded is not null)
        {
            return recorded.MatchesItems(items ?? [])
                ? new RecordedPostDispatchIntent(RecordedPostDispatchState.Recorded, shipment)
                : new RecordedPostDispatchIntent(
                    RecordedPostDispatchState.Conflict,
                    null,
                    "fulfillment.return.receipt_conflict");
        }

        return shipment?.Status == ShipmentStatus.Returned
            ? new RecordedPostDispatchIntent(
                RecordedPostDispatchState.Conflict,
                null,
                "fulfillment.return.receipt_already_recorded")
            : new RecordedPostDispatchIntent(RecordedPostDispatchState.NotRecorded);
    }

    /// <summary>TB-258-R2: is this exact resend occurrence already recorded for this source parcel?</summary>
    public RecordedPostDispatchIntent ResolveRecordedResendIntent(
        string? occurrenceKey,
        Guid originalShipmentId,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)>? items)
    {
        var key = string.IsNullOrWhiteSpace(occurrenceKey) ? null : occurrenceKey.Trim();
        if (key is null)
        {
            // A blank key is not a recorded occurrence. The owning command still refuses it as
            // `fulfillment.resend.occurrence_required`; it is never silently replayed as some other intent.
            return new RecordedPostDispatchIntent(RecordedPostDispatchState.NotRecorded);
        }

        var recorded = _shipments.FirstOrDefault(s =>
            s.IsResend && string.Equals(s.ShippingDispatchKey, key, StringComparison.Ordinal));
        var original = _shipments.FirstOrDefault(x => x.ShipmentId == originalShipmentId);
        if (recorded is null || original is null)
        {
            return new RecordedPostDispatchIntent(RecordedPostDispatchState.NotRecorded);
        }

        // TB-258-R3: exact identity is validated BEFORE choosing Recorded vs ResolvedCancelled. A changed source
        // or quantity under the key of a cancelled parcel is a typed conflict with zero writes — cancelled
        // history is a result state of an exact intent, never an exemption from identity validation.
        var requested = NormalizeSelections(items ?? []);
        var effectiveMethodCode = string.IsNullOrWhiteSpace(recorded.ShippingMethodCode)
            ? original.ShippingMethodCode
            : recorded.ShippingMethodCode;
        if (!RecordedResendAnswersIntent(recorded, original, requested, effectiveMethodCode))
        {
            return new RecordedPostDispatchIntent(
                RecordedPostDispatchState.Conflict,
                null,
                "fulfillment.resend.intent_conflict");
        }

        return recorded.Status == ShipmentStatus.Cancelled
            ? new RecordedPostDispatchIntent(RecordedPostDispatchState.ResolvedCancelled, recorded)
            : new RecordedPostDispatchIntent(RecordedPostDispatchState.Recorded, recorded);
    }

    private static bool LinesMatch(
        Shipment recorded,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> requested)
    {
        if (recorded.Items.Count != requested.Count)
        {
            return false;
        }

        var recordedMap = recorded.Items
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        foreach (var line in requested)
        {
            if (!recordedMap.TryGetValue(line.OrderLineId, out var quantity) || quantity != line.Quantity)
            {
                return false;
            }
        }

        return true;
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
        _items.Count > 0 && _items.All(x => DeliveredQuantity(x.OrderLineId) >= x.QuantityOrdered);

    private void EnsureNotTerminal()
    {
        if (Status is FulfillmentStatus.Cancelled or FulfillmentStatus.Failed)
        {
            throw new ContractOperationException("fulfillment.status.terminal");
        }
    }
}
