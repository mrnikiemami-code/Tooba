using Tooba.Fulfillment.Contracts.History;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Admin.Completeness.History;

public static partial class AdminOrderHistoryComposer
{
    private static readonly string[] PackedOrLater =
    [
        FulfillmentHistoryStatuses.Packed,
        FulfillmentHistoryStatuses.Dispatched,
        FulfillmentHistoryStatuses.InTransit,
        FulfillmentHistoryStatuses.Delivered,
    ];

    private static void AppendFulfillments(
        List<AdminOrderHistoryDraft> entries,
        CheckoutGroup group,
        AdminOrderHistoryScope scope,
        IReadOnlyList<FulfillmentHistoryRecord> fulfillments)
    {
        foreach (var record in fulfillments)
        {
            var sellerLabel = scope.SellerName(record.SellerPartyId);
            var packedQty = record.Items.Sum(x => x.QuantityPacked > 0 ? x.QuantityPacked : 0);
            if (packedQty <= 0)
            {
                packedQty = record.Items.Sum(x => x.QuantityOrdered);
            }

            if (record.Status == FulfillmentHistoryStatuses.Processing || PackedOrLater.Contains(record.Status))
            {
                entries.Add(Draft(
                    record.CreatedAt == default ? record.UpdatedAt : record.CreatedAt,
                    "fulfillment_processing",
                    "آماده‌سازی",
                    "Processing",
                    null,
                    sellerLabel,
                    sellerLabel));
            }

            if (PackedOrLater.Contains(record.Status))
            {
                entries.Add(Draft(
                    record.UpdatedAt == default ? record.CreatedAt : record.UpdatedAt,
                    "fulfillment_packed",
                    "بسته‌بندی",
                    "Packed",
                    null,
                    AdminOrderHistoryFormatting.FormatPackScopeFa(sellerLabel, packedQty),
                    $"{sellerLabel} — {packedQty} items"));
            }

            foreach (var shipment in record.Shipments)
            {
                AppendShipment(entries, group, scope, record, shipment);
            }
        }
    }

    private static void AppendShipment(
        List<AdminOrderHistoryDraft> entries,
        CheckoutGroup group,
        AdminOrderHistoryScope scope,
        FulfillmentHistoryRecord record,
        FulfillmentHistoryShipment shipment)
    {
        var created = shipment.CreatedAt == default ? record.UpdatedAt : shipment.CreatedAt;
        if (created == default)
        {
            created = record.CreatedAt;
        }

        var methodLabel = string.IsNullOrWhiteSpace(shipment.ShippingMethodLabel)
            ? (string.IsNullOrWhiteSpace(shipment.CarrierDisplayName) ? "مرسوله" : shipment.CarrierDisplayName)
            : shipment.ShippingMethodLabel;
        var shipQty = shipment.Items.Sum(x => x.Quantity);
        var lineScope = scope.FormatLineQtyScope(
            shipment.Items.Select(x => (x.OrderLineId, x.Quantity)).ToList());

        entries.Add(Draft(
            created,
            "shipment_created",
            "ایجاد مرسوله",
            "Shipment created",
            null,
            $"{methodLabel} — {AdminOrderHistoryFormatting.ToFaDigits(shipQty)} قلم",
            $"{methodLabel} — {shipQty} items"));

        if (!string.IsNullOrWhiteSpace(shipment.TrackingReference))
        {
            entries.Add(Draft(
                shipment.DispatchedAt ?? created,
                "tracking_assigned",
                "ثبت رهگیری",
                "Tracking assigned",
                null,
                $"کد رهگیری {shipment.TrackingReference}",
                $"Tracking {shipment.TrackingReference}"));
        }

        if (!string.IsNullOrWhiteSpace(shipment.PreviousTrackingReference)
            && !string.IsNullOrWhiteSpace(shipment.TrackingReference))
        {
            entries.Add(Draft(
                created,
                "tracking_corrected",
                "اصلاح کد رهگیری",
                "Tracking corrected",
                null,
                $"از {shipment.PreviousTrackingReference} به {shipment.TrackingReference}",
                $"{shipment.PreviousTrackingReference} → {shipment.TrackingReference}"));
        }

        if (shipment.Status == FulfillmentHistoryStatuses.Cancelled)
        {
            var preDispatchAbort = group.SellerOrders.Any(x =>
                x.SellerOrderId == record.SellerOrderId && x.Status == SellerOrderStatus.Cancelled);
            var cancelledAt = record.UpdatedAt == default ? created : record.UpdatedAt;
            entries.Add(Draft(
                cancelledAt,
                "shipment_cancelled",
                preDispatchAbort ? "مرسوله پیش از ارسال ابطال شد" : "عدم پذیرش مرسوله",
                preDispatchAbort ? "Pre-dispatch shipment cancelled" : "Shipment rejected",
                null,
                string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    ? methodLabel
                    : $"کد رهگیری {shipment.TrackingReference}",
                shipment.TrackingReference ?? methodLabel));
            if (preDispatchAbort)
            {
                entries.Add(Draft(
                    cancelledAt,
                    "allocation_released",
                    "تخصیص اقلام آزاد شد",
                    "Line allocation released",
                    null,
                    $"{methodLabel} — {AdminOrderHistoryFormatting.ToFaDigits(shipQty)} قلم",
                    $"{methodLabel} — {shipQty} items"));
            }
        }

        if (shipment.DispatchedAt is { } dispatched)
        {
            entries.Add(Draft(
                dispatched,
                "shipment_dispatched",
                "ارسال مرسوله",
                "Shipment dispatched",
                null,
                string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    ? $"{methodLabel} — {AdminOrderHistoryFormatting.ToFaDigits(shipQty)} قلم"
                    : $"کد رهگیری {shipment.TrackingReference}",
                shipment.TrackingReference ?? methodLabel));
        }

        if (shipment.DeliveredAt is { } delivered)
        {
            entries.Add(Draft(
                delivered,
                "shipment_delivered",
                "تحویل",
                "Shipment delivered",
                null,
                string.IsNullOrWhiteSpace(lineScope)
                    ? AdminOrderHistoryFormatting.FormatProductQtyScopeFa(methodLabel, shipQty)
                    : lineScope,
                lineScope));
        }
    }

    private static void AppendConsolidatedPackages(
        List<AdminOrderHistoryDraft> entries,
        AdminOrderHistoryScope scope,
        IReadOnlyList<ConsolidatedPackageHistoryRecord> packages)
    {
        foreach (var package in packages)
        {
            var memberCountFa = AdminOrderHistoryFormatting.ToFaDigits(package.Members.Count);
            entries.Add(Draft(
                package.CreatedAt,
                "consolidated_package_created",
                "بسته تجمیعی ایجاد شد",
                "Consolidated package created",
                package.CreatedBy,
                $"{package.PackageNumber} — {memberCountFa} مرسوله",
                $"{package.PackageNumber} — {package.Members.Count} shipments"));

            foreach (var member in package.Members)
            {
                entries.Add(Draft(
                    member.JoinedAt == default ? package.CreatedAt : member.JoinedAt,
                    "consolidated_package_member_added",
                    $"مرسوله فروشنده {scope.SellerName(member.SellerPartyId)} به بسته تجمیعی اضافه شد",
                    "Seller shipment added to consolidated package",
                    package.CreatedBy,
                    package.PackageNumber,
                    package.PackageNumber));
            }

            if (package.CancelledAt is { } cancelledAt)
            {
                entries.Add(Draft(
                    cancelledAt,
                    "consolidated_package_cancelled",
                    "بسته تجمیعی باطل شد",
                    "Consolidated package cancelled",
                    null,
                    package.PackageNumber,
                    package.PackageNumber));
                entries.Add(Draft(
                    cancelledAt,
                    "consolidated_package_members_released",
                    "عضویت مرسوله‌ها آزاد شد",
                    "Consolidated package memberships released",
                    null,
                    package.PackageNumber,
                    package.PackageNumber));
            }

            if (package.DispatchedAt is { } packageDispatched)
            {
                entries.Add(Draft(
                    packageDispatched,
                    "consolidated_package_dispatched",
                    "بسته تجمیعی ارسال شد",
                    "Consolidated package dispatched",
                    null,
                    package.PackageNumber,
                    package.PackageNumber));
                entries.Add(Draft(
                    packageDispatched,
                    "consolidated_package_members_dispatched",
                    "مرسوله‌های عضو ارسال شدند",
                    "Member shipments dispatched",
                    null,
                    $"{package.PackageNumber} — {memberCountFa} مرسوله",
                    $"{package.PackageNumber} — {package.Members.Count} shipments"));
            }

            if (package.DeliveredAt is { } packageDelivered)
            {
                entries.Add(Draft(
                    packageDelivered,
                    "consolidated_package_delivered",
                    "بسته تجمیعی تحویل شد",
                    "Consolidated package delivered",
                    null,
                    package.PackageNumber,
                    package.PackageNumber));
                entries.Add(Draft(
                    packageDelivered,
                    "consolidated_package_members_delivered",
                    "مرسوله‌های عضو تحویل شدند",
                    "Member shipments delivered",
                    null,
                    $"{package.PackageNumber} — {memberCountFa} مرسوله",
                    $"{package.PackageNumber} — {package.Members.Count} shipments"));
            }
        }
    }
}
