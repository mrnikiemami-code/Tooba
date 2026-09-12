using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Identity.Application;
using Tooba.OperatorProfile.Application;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// ترکیب یادداشت داخلی، تاریخچهٔ عملیاتی، و HTML فاکتور/رسید از منابع موجود — بدون event store جدید.
/// </summary>
public sealed class AdminOrderCompletenessComposer
{
    private readonly OrderDbContext _orders;
    private readonly ReturnsDbContext _returns;
    private readonly CatalogDbContext _catalog;
    private readonly ICheckoutDirectory _checkout;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly IPaymentAdminDirectory _payments;
    private readonly ISettlementDirectory _settlement;
    private readonly IAccessControlDirectory _access;
    private readonly IOperatorProfileDirectory _profiles;
    private readonly IIdentityContactLookup _contacts;
    private readonly ICurrentTenant _tenant;
    private readonly IPartyLookupGateway _parties;

    /// <summary>ترکیب‌گر را به ماژول‌های موجود وصل می‌کند.</summary>
    public AdminOrderCompletenessComposer(
        OrderDbContext orders,
        ReturnsDbContext returns,
        CatalogDbContext catalog,
        ICheckoutDirectory checkout,
        IFulfillmentDirectory fulfillment,
        IPaymentAdminDirectory payments,
        ISettlementDirectory settlement,
        IAccessControlDirectory access,
        IOperatorProfileDirectory profiles,
        IIdentityContactLookup contacts,
        ICurrentTenant tenant,
        IPartyLookupGateway parties)
    {
        _orders = orders;
        _returns = returns;
        _catalog = catalog;
        _checkout = checkout;
        _fulfillment = fulfillment;
        _payments = payments;
        _settlement = settlement;
        _access = access;
        _profiles = profiles;
        _contacts = contacts;
        _tenant = tenant;
        _parties = parties;
    }

    /// <summary>یادداشت‌های داخلی را فهرست می‌کند (order.view).</summary>
    public async Task<IReadOnlyList<AdminOrderNoteView>> ListNotesAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(actorUserId, "order.view", cancellationToken);
        await EnsureCheckoutExistsAsync(checkoutId, cancellationToken);
        await _checkout.RecordAdminViewAsync(checkoutId, actorUserId, cancellationToken);
        var notes = await _checkout.ListNotesAsync(checkoutId, actorUserId, 50, cancellationToken);
        var labels = await ResolveActorLabelsAsync(notes.Select(x => (Guid?)x.CreatedByUserId), cancellationToken);
        return notes.Select(n => MapNote(n, labels)).ToList();
    }

    /// <summary>یادداشت داخلی append-only می‌افزاید (order.handle).</summary>
    public async Task<AdminOrderNoteView> AddNoteAsync(
        Guid checkoutId,
        Guid actorUserId,
        string? body,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(actorUserId, "order.handle", cancellationToken);
        await EnsureCheckoutExistsAsync(checkoutId, cancellationToken);
        try
        {
            var note = await _checkout.AddNoteAsync(checkoutId, actorUserId, body ?? string.Empty, cancellationToken);
            var labels = await ResolveActorLabelsAsync(new Guid?[] { note.CreatedByUserId }, cancellationToken);
            return MapNote(note, labels);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "order.note.invalid");
        }
    }

    /// <summary>یادداشت را طبق قاعدهٔ نویسنده/قفل مشاهده حذف می‌کند (order.handle).</summary>
    public async Task DeleteNoteAsync(
        Guid checkoutId,
        Guid noteId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(actorUserId, "order.handle", cancellationToken);
        await EnsureCheckoutExistsAsync(checkoutId, cancellationToken);
        try
        {
            await _checkout.DeleteNoteAsync(checkoutId, noteId, actorUserId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "order.note.delete.forbidden");
        }
    }

    /// <summary>تاریخچهٔ عملیاتی ترکیبی را صفحه می‌کند (order.view).</summary>
    public async Task<AdminOperationalHistoryPage> ListOperationalHistoryAsync(
        Guid checkoutId,
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(actorUserId, "order.view", cancellationToken);
        await _checkout.RecordAdminViewAsync(checkoutId, actorUserId, cancellationToken);
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");

        var drafts = await ComposeHistoryDraftsAsync(group, cancellationToken);
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 50);
        var total = drafts.Count;
        var pageDrafts = drafts
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToList();
        var labels = await ResolveActorLabelsAsync(pageDrafts.Select(x => x.ActorUserId), cancellationToken);
        var items = pageDrafts.Select(d => ToEntry(d, labels)).ToList();
        return new AdminOperationalHistoryPage(checkoutId, safePage, safeSize, total, items);
    }

    /// <summary>HTML قابل‌چاپ فاکتور از snapshot سفارش (order.view).</summary>
    public async Task<string> BuildInvoiceHtmlAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(actorUserId, "order.view", cancellationToken);
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "فاکتور در دسترس نیست.", "order.invoice.unavailable");
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken);
        return RenderInvoiceHtml(group, payment);
    }

    /// <summary>HTML رسید پرداخت در صورت وجود پرداخت (order.view).</summary>
    public async Task<string> BuildReceiptHtmlAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(actorUserId, "order.view", cancellationToken);
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "رسید در دسترس نیست.", "order.receipt.unavailable");
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "رسید پرداخت در دسترس نیست.", "order.receipt.unavailable");
        return RenderReceiptHtml(group, payment);
    }

    private async Task<IReadOnlyList<HistoryDraft>> ComposeHistoryDraftsAsync(
        CheckoutGroup group,
        CancellationToken cancellationToken)
    {
        var entries = new List<HistoryDraft>
        {
            Draft(
                group.SubmittedAt,
                "order_created",
                "ثبت سفارش",
                "Order created",
                actorUserId: null,
                $"Checkout {group.CheckoutId:N}"[..20],
                $"Checkout {group.CheckoutId:N}"[..20]),
        };

        foreach (var order in group.SellerOrders.Where(x => x.Status == SellerOrderStatus.Cancelled))
        {
            entries.Add(Draft(
                group.SubmittedAt,
                "order_cancelled",
                "سفارش لغو شد",
                "Order cancelled",
                null,
                $"سفارش {order.OrderNumber}",
                $"Order {order.OrderNumber}"));
            entries.Add(Draft(
                group.SubmittedAt,
                "inventory_released",
                "رزرو موجودی آزاد شد",
                "Inventory reservation released",
                null,
                $"سفارش {order.OrderNumber}",
                $"Order {order.OrderNumber}"));
        }

        foreach (var order in group.SellerOrders.Where(x => x.LastRestoredAt is not null))
        {
            entries.Add(Draft(
                order.LastRestoredAt!.Value,
                "order_restored",
                "بازگردانی سفارش لغوشده",
                "Cancelled order restored",
                null,
                $"سفارش {order.OrderNumber}",
                $"Order {order.OrderNumber}"));
        }

        var payment = await _payments.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (payment is not null)
        {
            entries.Add(Draft(
                payment.CreatedAt,
                "payment_created",
                "ایجاد پرداخت",
                "Payment created",
                null,
                $"{payment.Amount:0} {payment.Currency}",
                $"{payment.Amount:0} {payment.Currency}"));

            switch (payment.Status)
            {
                case PaymentStatus.Pending:
                    entries.Add(Draft(
                        payment.UpdatedAt == default ? payment.CreatedAt : payment.UpdatedAt,
                        "payment_pending",
                        "پرداخت در انتظار",
                        "Payment pending",
                        null));
                    if (payment.HasManualDepositRejection)
                    {
                        entries.Add(Draft(
                            payment.UpdatedAt == default ? payment.CreatedAt : payment.UpdatedAt,
                            "payment_deposit_restored",
                            "بازگرداندن به انتظار تأیید واریز",
                            "Deposit restored to pending confirmation",
                            null));
                    }

                    break;
                case PaymentStatus.Succeeded:
                    entries.Add(Draft(
                        payment.CompletedAt ?? payment.UpdatedAt,
                        "payment_succeeded",
                        "پرداخت موفق",
                        "Payment succeeded",
                        null,
                        $"{payment.Amount:0} {payment.Currency}",
                        $"{payment.Amount:0} {payment.Currency}"));
                    break;
                case PaymentStatus.Failed:
                    entries.Add(Draft(
                        payment.UpdatedAt,
                        payment.HasManualDepositRejection || payment.LastFailureCode == "MANUAL_DEPOSIT_REJECTED"
                            ? "payment_deposit_rejected"
                            : "payment_failed",
                        payment.HasManualDepositRejection || payment.LastFailureCode == "MANUAL_DEPOSIT_REJECTED"
                            ? "رد واریز"
                            : "پرداخت ناموفق",
                        payment.HasManualDepositRejection || payment.LastFailureCode == "MANUAL_DEPOSIT_REJECTED"
                            ? "Deposit rejected"
                            : "Payment failed",
                        null,
                        payment.LastFailureCode,
                        payment.LastFailureCode));
                    break;
                case PaymentStatus.Cancelled:
                    entries.Add(Draft(
                        payment.UpdatedAt,
                        "payment_cancelled",
                        "لغو پرداخت",
                        "Payment cancelled",
                        null));
                    break;
                case PaymentStatus.Expired:
                    entries.Add(Draft(
                        payment.UpdatedAt,
                        "payment_expired",
                        "انقضای پرداخت",
                        "Payment expired",
                        null));
                    break;
                case PaymentStatus.RefundPending:
                    entries.Add(Draft(
                        payment.UpdatedAt,
                        "payment_refund_pending",
                        group.SellerOrders.Any(x => x.Status == SellerOrderStatus.Cancelled)
                            ? "بازگشت وجه آغاز شد"
                            : "بازگشت وجه در انتظار",
                        "Refund pending",
                        null));
                    break;
                case PaymentStatus.Refunded:
                    entries.Add(Draft(
                        payment.UpdatedAt,
                        "payment_refunded",
                        "بازگشت وجه",
                        "Refunded",
                        null,
                        $"{payment.Amount:0} {payment.Currency}",
                        $"{payment.Amount:0} {payment.Currency}"));
                    break;
                case PaymentStatus.RefundFailed:
                    entries.Add(Draft(
                        payment.UpdatedAt,
                        "payment_refund_failed",
                        "شکست بازگشت وجه",
                        "Refund failed",
                        null,
                        payment.LastFailureCode,
                        payment.LastFailureCode));
                    break;
            }
        }

        var sellerPartyIds = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerNameMap = new Dictionary<Guid, string>();
        foreach (var partyId in sellerPartyIds)
        {
            var party = await _parties.FindByIdAsync(partyId, cancellationToken);
            sellerNameMap[partyId] = string.IsNullOrWhiteSpace(party?.DisplayName)
                ? "فروشنده"
                : party!.DisplayName;
        }

        string SellerName(Guid sellerPartyId) =>
            sellerNameMap.TryGetValue(sellerPartyId, out var n) ? n : "فروشنده";

        var lineById = group.SellerOrders
            .SelectMany(o => o.Lines)
            .ToDictionary(l => l.LineId);
        var titlesByVariant = await LoadVariantTitlesAsync(
            lineById.Values.Select(l => l.CatalogVariantId).Distinct().ToList(),
            cancellationToken);
        string LineTitle(Guid orderLineId) =>
            lineById.TryGetValue(orderLineId, out var line)
            && titlesByVariant.TryGetValue(line.CatalogVariantId, out var title)
            && !string.IsNullOrWhiteSpace(title)
                ? title
                : "کالای سفارش";

        string FormatLineQtyScope(IReadOnlyList<(Guid OrderLineId, decimal Quantity)> items)
        {
            var list = items.Where(x => x.Quantity > 0).ToList();
            if (list.Count == 0)
            {
                return string.Empty;
            }

            if (list.Count == 1)
            {
                var only = list[0];
                return $"{LineTitle(only.OrderLineId)} — تعداد {ToFaDigits(only.Quantity)}";
            }

            var total = list.Sum(x => x.Quantity);
            return $"{LineTitle(list[0].OrderLineId)} و {ToFaDigits(list.Count - 1)} کالای دیگر — تعداد {ToFaDigits(total)}";
        }

        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        foreach (var f in fulfillments)
        {
            var sellerLabel = SellerName(f.SellerPartyId);
            var packedQty = f.Items.Sum(i => i.QuantityPacked > 0 ? i.QuantityPacked : 0);
            if (packedQty <= 0)
            {
                packedQty = f.Items.Sum(i => i.QuantityOrdered);
            }

            if (f.Status is FulfillmentStatus.Processing or FulfillmentStatus.Packed
                or FulfillmentStatus.Dispatched or FulfillmentStatus.InTransit or FulfillmentStatus.Delivered)
            {
                entries.Add(Draft(
                    f.CreatedAt == default ? f.UpdatedAt : f.CreatedAt,
                    "fulfillment_processing",
                    "آماده‌سازی",
                    "Processing",
                    null,
                    $"{sellerLabel}",
                    sellerLabel));
            }

            if (f.Status is FulfillmentStatus.Packed or FulfillmentStatus.Dispatched
                or FulfillmentStatus.InTransit or FulfillmentStatus.Delivered)
            {
                entries.Add(Draft(
                    f.UpdatedAt == default ? f.CreatedAt : f.UpdatedAt,
                    "fulfillment_packed",
                    "بسته‌بندی",
                    "Packed",
                    null,
                    $"{sellerLabel} — {ToFaDigits(packedQty)} قلم",
                    $"{sellerLabel} — {packedQty} items"));
            }

            foreach (var shipment in f.Shipments)
            {
                var created = shipment.CreatedAt == default ? f.UpdatedAt : shipment.CreatedAt;
                var methodLabel = string.IsNullOrWhiteSpace(shipment.ShippingMethodLabel)
                    ? (string.IsNullOrWhiteSpace(shipment.CarrierDisplayName) ? "مرسوله" : shipment.CarrierDisplayName)
                    : shipment.ShippingMethodLabel;
                var shipQty = shipment.Items.Sum(i => i.Quantity);
                var lineScope = FormatLineQtyScope(
                    shipment.Items.Select(i => (i.OrderLineId, i.Quantity)).ToList());
                entries.Add(Draft(
                    created == default ? f.CreatedAt : created,
                    "shipment_created",
                    "ایجاد مرسوله",
                    "Shipment created",
                    null,
                    $"{methodLabel} — {ToFaDigits(shipQty)} قلم",
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

                if (shipment.Status == ShipmentStatus.Cancelled)
                {
                    var preDispatchAbort = group.SellerOrders.Any(x =>
                        x.SellerOrderId == f.SellerOrderId && x.Status == SellerOrderStatus.Cancelled);
                    entries.Add(Draft(
                        f.UpdatedAt == default ? created : f.UpdatedAt,
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
                            f.UpdatedAt == default ? created : f.UpdatedAt,
                            "allocation_released",
                            "تخصیص اقلام آزاد شد",
                            "Line allocation released",
                            null,
                            $"{methodLabel} — {ToFaDigits(shipQty)} قلم",
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
                            ? $"{methodLabel} — {ToFaDigits(shipQty)} قلم"
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
                            ? $"{methodLabel} — تعداد {ToFaDigits(shipQty)}"
                            : lineScope,
                        lineScope));
                }
            }
        }

        var packages = await _fulfillment.GetPackagesForCheckoutAsync(group.CheckoutId, cancellationToken);
        foreach (var package in packages)
        {
            entries.Add(Draft(
                package.CreatedAt,
                "consolidated_package_created",
                "بسته تجمیعی ایجاد شد",
                "Consolidated package created",
                package.CreatedBy,
                $"{package.PackageNumber} — {ToFaDigits(package.Members.Count)} مرسوله",
                $"{package.PackageNumber} — {package.Members.Count} shipments"));
            foreach (var member in package.Members)
            {
                var sellerLabel = SellerName(member.SellerPartyId);
                entries.Add(Draft(
                    member.JoinedAt == default ? package.CreatedAt : member.JoinedAt,
                    "consolidated_package_member_added",
                    $"مرسوله فروشنده {sellerLabel} به بسته تجمیعی اضافه شد",
                    $"Seller shipment added to consolidated package",
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
                    $"{package.PackageNumber} — {ToFaDigits(package.Members.Count)} مرسوله",
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
                    $"{package.PackageNumber} — {ToFaDigits(package.Members.Count)} مرسوله",
                    $"{package.PackageNumber} — {package.Members.Count} shipments"));
            }
        }

        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var orderByReturnSeller = group.SellerOrders.ToDictionary(x => x.SellerOrderId);
        var returns = await _returns.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var returnIds = returns.Select(x => x.ReturnRequestId).ToList();
        var returnItems = returnIds.Count == 0
            ? []
            : await _returns.ReturnItems.AsNoTracking()
                .Where(x => returnIds.Contains(x.ReturnRequestId))
                .ToListAsync(cancellationToken);
        var itemsByReturn = returnItems
            .GroupBy(x => x.ReturnRequestId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<(Guid OrderLineId, decimal Quantity)>)g
                .Select(i => (i.OrderLineId, i.Quantity))
                .ToList());
        var refundAttempts = returnIds.Count == 0
            ? []
            : await _returns.RefundAttempts.AsNoTracking()
                .Where(x => returnIds.Contains(x.ReturnRequestId))
                .ToListAsync(cancellationToken);
        var attemptsByReturn = refundAttempts.GroupBy(x => x.ReturnRequestId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var ret in returns)
        {
            orderByReturnSeller.TryGetValue(ret.SellerOrderId, out var so);
            var sellerLabel = so is null ? "فروشنده" : SellerName(so.SellerPartyId);
            itemsByReturn.TryGetValue(ret.ReturnRequestId, out var retLines);
            var returnScope = retLines is { Count: > 0 }
                ? FormatLineQtyScope(retLines)
                : string.Empty;
            var summaryFa = string.IsNullOrWhiteSpace(returnScope) ? sellerLabel : returnScope;
            entries.Add(Draft(
                ret.CreatedAt,
                "return_requested",
                "مرجوعی",
                "Return requested",
                ret.RequestedByUserId == Guid.Empty ? null : ret.RequestedByUserId,
                summaryFa,
                summaryFa));
            if (ret.Status is ReturnRequestStatus.Approved or ReturnRequestStatus.RefundProcessing
                or ReturnRequestStatus.Completed or ReturnRequestStatus.RefundFailed)
            {
                entries.Add(Draft(
                    ret.UpdatedAt,
                    "return_approved",
                    "تأیید مرجوعی",
                    "Return approved",
                    null,
                    sellerLabel,
                    sellerLabel));
            }

            if (ret.Status == ReturnRequestStatus.Rejected)
            {
                entries.Add(Draft(
                    ret.UpdatedAt,
                    "return_rejected",
                    "رد مرجوعی",
                    "Return rejected",
                    null,
                    sellerLabel,
                    sellerLabel));
            }

            if (!attemptsByReturn.TryGetValue(ret.ReturnRequestId, out var attempts))
            {
                continue;
            }

            foreach (var attempt in attempts)
            {
                if (attempt.Status == RefundAttemptStatus.Succeeded)
                {
                    entries.Add(Draft(
                        attempt.CompletedAt ?? attempt.CreatedAt,
                        "refund_completed",
                        "بازگشت وجه",
                        "Refund completed",
                        null,
                        $"{FormatMoneyFa(attempt.Amount)} ریال",
                        $"{attempt.Amount:0} {attempt.Currency}"));
                }
                else if (attempt.Status == RefundAttemptStatus.Failed)
                {
                    entries.Add(Draft(
                        attempt.CompletedAt ?? attempt.CreatedAt,
                        "refund_failed",
                        "شکست بازگشت وجه",
                        "Refund failed",
                        null));
                }
                else if (attempt.Status == RefundAttemptStatus.Pending
                         && ret.Status == ReturnRequestStatus.RefundFailed)
                {
                    entries.Add(Draft(
                        attempt.CreatedAt,
                        "refund_retried",
                        "تلاش مجدد بازگشت وجه",
                        "Refund retried",
                        null));
                }
            }
        }

        var settlementByOrder = await _settlement.ListEntriesBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        foreach (var (sellerOrderId, settlementEntries) in settlementByOrder)
        {
            var orderNumber = group.SellerOrders.FirstOrDefault(x => x.SellerOrderId == sellerOrderId)?.OrderNumber
                ?? sellerOrderId.ToString("N")[..8];
            foreach (var entry in settlementEntries)
            {
                if (string.Equals(entry.SourceType, "order_cancel", StringComparison.OrdinalIgnoreCase))
                {
                    entries.Add(Draft(
                        entry.PostedAt,
                        "settlement_cancel_adjustment",
                        "تعدیل سهم فروشنده ثبت شد",
                        "Seller cancel adjustment",
                        null,
                        $"سفارش {orderNumber}",
                        $"Order {orderNumber}"));
                }
                else if (string.Equals(entry.SourceType, "refund", StringComparison.OrdinalIgnoreCase)
                    || entry.EntryType == EntryType.Debit)
                {
                    entries.Add(Draft(
                        entry.PostedAt,
                        "settlement_adjustment",
                        "تعدیل تسویه (مرجوعی)",
                        "Seller refund adjustment",
                        null,
                        $"سفارش {orderNumber}",
                        $"Order {orderNumber}"));
                }
                else
                {
                    entries.Add(Draft(
                        entry.PostedAt,
                        "settlement_accrual",
                        "ثبت تسویه فروشنده",
                        "Seller settlement accrual",
                        null,
                        $"سفارش {orderNumber}",
                        $"Order {orderNumber}"));
                }
            }
        }

        var notes = await _checkout.ListNotesAsync(group.CheckoutId, Guid.Empty, 50, cancellationToken);
        foreach (var note in notes)
        {
            if (TryMapInventoryRecoveryNote(note.Body, out var kind, out var labelFa, out var labelEn))
            {
                entries.Add(Draft(
                    note.CreatedAt,
                    kind,
                    labelFa,
                    labelEn,
                    note.CreatedByUserId == Guid.Empty ? null : note.CreatedByUserId,
                    Truncate(note.Body, 120),
                    Truncate(note.Body, 120)));
                continue;
            }

            entries.Add(Draft(
                note.CreatedAt,
                "operational_note",
                "یادداشت داخلی",
                "Internal note",
                note.CreatedByUserId == Guid.Empty ? null : note.CreatedByUserId,
                Truncate(note.Body, 120),
                Truncate(note.Body, 120)));
        }

        return entries
            .OrderByDescending(x => x.OccurredAt)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ToList();
    }

    private static bool TryMapInventoryRecoveryNote(
        string body,
        out string kind,
        out string labelFa,
        out string labelEn)
    {
        kind = string.Empty;
        labelFa = string.Empty;
        labelEn = string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        if (body.StartsWith(OrderInventoryRecoveryComposer.NotePrefixRequested, StringComparison.Ordinal))
        {
            kind = "inventory_recovery_requested";
            labelFa = "درخواست بازیابی موجودی";
            labelEn = "Inventory Recovery Requested";
            return true;
        }

        if (body.StartsWith(OrderInventoryRecoveryComposer.NotePrefixSucceeded, StringComparison.Ordinal))
        {
            kind = "inventory_recovery_succeeded";
            labelFa = "بازیابی موجودی موفق";
            labelEn = "Inventory Recovery Succeeded";
            return true;
        }

        if (body.StartsWith(OrderInventoryRecoveryComposer.NotePrefixFailed, StringComparison.Ordinal))
        {
            kind = "inventory_recovery_failed_insufficient";
            labelFa = "شکست بازیابی موجودی — کمبود موجودی";
            labelEn = "Inventory Recovery Failed — Insufficient Inventory";
            return true;
        }

        if (body.StartsWith(OrderInventoryRecoveryComposer.NotePrefixManual, StringComparison.Ordinal))
        {
            kind = "inventory_recovery_manual_review";
            labelFa = "بازیابی موجودی نیازمند بررسی دستی";
            labelEn = "Inventory Recovery Requires Manual Review";
            return true;
        }

        return false;
    }

    private async Task<IReadOnlyDictionary<Guid, ActorLabel>> ResolveActorLabelsAsync(
        IEnumerable<Guid?> actorUserIds,
        CancellationToken cancellationToken)
    {
        var ids = actorUserIds
            .Where(x => x is Guid g && g != Guid.Empty)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, ActorLabel>();
        }

        var profiles = await _profiles.GetManyAsync(ids, cancellationToken);
        var contacts = await _contacts.GetContactsAsync(ids, cancellationToken);
        var map = new Dictionary<Guid, ActorLabel>(ids.Length);
        foreach (var id in ids)
        {
            profiles.TryGetValue(id, out var profile);
            contacts.TryGetValue(id, out var contact);
            var display = FirstNonEmpty(
                Usable(profile?.DisplayName),
                Usable(JoinName(profile?.FirstName, profile?.LastName)),
                Usable(contact?.Email),
                Usable(contact?.Mobile));
            map[id] = display is null
                ? ActorLabel.MissingUser()
                : ActorLabel.User(display);
        }

        return map;
    }

    private static AdminOrderNoteView MapNote(
        CheckoutOperationalNoteSnapshot note,
        IReadOnlyDictionary<Guid, ActorLabel> labels)
    {
        var label = ResolveLabel(note.CreatedByUserId, labels);
        return new AdminOrderNoteView(
            note.NoteId,
            note.CheckoutId,
            note.Body,
            note.CreatedByUserId,
            note.CreatedAt,
            label.Kind,
            label.DisplayName,
            label.DisplayFa,
            label.DisplayEn,
            note.CanDelete);
    }

    private static AdminOperationalHistoryEntry ToEntry(
        HistoryDraft draft,
        IReadOnlyDictionary<Guid, ActorLabel> labels)
    {
        var label = ResolveLabel(draft.ActorUserId, labels);
        return new AdminOperationalHistoryEntry(
            draft.OccurredAt,
            draft.Kind,
            draft.LabelFa,
            draft.LabelEn,
            label.Kind,
            label.DisplayName,
            label.DisplayFa,
            label.DisplayEn,
            string.IsNullOrWhiteSpace(draft.SummaryFa) ? null : draft.SummaryFa,
            string.IsNullOrWhiteSpace(draft.SummaryEn) ? null : draft.SummaryEn);
    }

    /// <summary>برچسب نمایشی Actor برای تست و ترکیب Host.</summary>
    internal static ActorLabel ResolveLabel(Guid? actorUserId, IReadOnlyDictionary<Guid, ActorLabel> labels)
    {
        if (actorUserId is null || actorUserId == Guid.Empty)
        {
            return ActorLabel.System();
        }

        return labels.TryGetValue(actorUserId.Value, out var found)
            ? found
            : ActorLabel.MissingUser();
    }

    private static HistoryDraft Draft(
        DateTimeOffset occurredAt,
        string kind,
        string labelFa,
        string labelEn,
        Guid? actorUserId,
        string? summaryFa = null,
        string? summaryEn = null) =>
        new(occurredAt, kind, labelFa, labelEn, actorUserId, summaryFa, summaryEn);

    private static string ToFaDigits(decimal value)
    {
        var s = Tooba.BuildingBlocks.QuantityDisplay.Format(value, 6);
        var map = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        return string.Concat(s.Select(ch => ch is >= '0' and <= '9' ? map[ch - '0'] : ch));
    }

    private static string FormatMoneyFa(decimal amount)
    {
        var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        var withSep = rounded.ToString("#,##0", CultureInfo.InvariantCulture);
        return ToFaDigitsString(withSep);
    }

    private static string ToFaDigitsString(string value)
    {
        var map = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        return string.Concat(value.Select(ch => ch is >= '0' and <= '9' ? map[ch - '0'] : ch));
    }

    /// <summary>خلاصهٔ محدوده برای تست و ترکیب تاریخچه (فروشنده / قلم / تعداد).</summary>
    internal static string FormatPackScopeFa(string sellerDisplayName, decimal quantity) =>
        $"{(string.IsNullOrWhiteSpace(sellerDisplayName) ? "فروشنده" : sellerDisplayName)} — {ToFaDigits(quantity)} قلم";

    /// <summary>خلاصهٔ کالایی تعداددار برای تحویل/مرجوعی.</summary>
    internal static string FormatProductQtyScopeFa(string productTitle, decimal quantity) =>
        $"{(string.IsNullOrWhiteSpace(productTitle) ? "کالای سفارش" : productTitle)} — تعداد {ToFaDigits(quantity)}";

    private async Task<Dictionary<Guid, string>> LoadVariantTitlesAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return [];
        }

        var variants = await _catalog.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToList();
        var names = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && productIds.Contains(x.OwnerId)
                && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var productNames = names.GroupBy(x => x.OwnerId).ToDictionary(
            x => x.Key,
            x => x.OrderBy(row => row.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
        return variants.Where(x => productNames.ContainsKey(x.ProductId))
            .ToDictionary(x => x.VariantId, x => productNames[x.ProductId]);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? Usable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        // رد کردن نام‌های خراب‌شدهٔ encoding که فقط '?' هستند
        if (trimmed.All(ch => ch == '?' || char.IsWhiteSpace(ch)))
        {
            return null;
        }

        return trimmed;
    }

    private static string? JoinName(string? first, string? last)
    {
        var parts = new[] { first, last }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
        return parts.Length == 0 ? null : string.Join(' ', parts);
    }

    private sealed record HistoryDraft(
        DateTimeOffset OccurredAt,
        string Kind,
        string LabelFa,
        string LabelEn,
        Guid? ActorUserId,
        string? SummaryFa,
        string? SummaryEn);

    /// <summary>برچسب انسانی Actor بدون شناسهٔ فنی در متن اصلی UI.</summary>
    internal readonly record struct ActorLabel(string Kind, string DisplayName, string DisplayFa, string DisplayEn)
    {
        public static ActorLabel System() =>
            new("system", "سیستم", "توسط سیستم", "By system");

        public static ActorLabel User(string displayName) =>
            new("user", displayName, $"توسط {displayName}", $"By {displayName}");

        public static ActorLabel MissingUser() =>
            new("user", "کاربر نامشخص", "توسط کاربر نامشخص", "By unknown user");
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";

    private async Task EnsureCheckoutExistsAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var exists = await _orders.Checkouts.AsNoTracking()
            .AnyAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (!exists)
        {
            throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");
        }
    }

    private async Task<CheckoutGroup?> LoadCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);

    private async Task EnsurePermissionAsync(Guid actorUserId, string permissionId, CancellationToken cancellationToken)
    {
        var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
        if (!AdminOrderOperationsComposer.Has(effective, permissionId))
        {
            throw new PlatformHttpException(403, "مجوز انجام این عملیات وجود ندارد.", "order.operation.denied");
        }
    }

    private async Task<EffectiveAccessDto> LoadEffectiveAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.Current?.TenantId.Value;
        var scope = new AccessOwnerScope(AccessOwnerScopeKind.Platform, null, tenantId);
        return await _access.GetEffectiveAccessAsync(actorUserId, scope, cancellationToken);
    }

    internal static string RenderInvoiceHtml(CheckoutGroup group, PaymentOperationalSnapshot? payment)
    {
        var reference = group.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault()
            ?? group.CheckoutId.ToString("N")[..12];
        var currency = group.SellerOrders.Select(x => x.Currency).FirstOrDefault() ?? group.Currency;
        var subtotal = group.SellerOrders.Sum(x => x.SubtotalSnapshot);
        var tax = group.SellerOrders.Sum(x => x.TaxSnapshot);
        var duty = group.SellerOrders.Sum(x => x.TotalDutyAmount);
        var discount = group.SellerOrders.Sum(x => x.DiscountSnapshot);
        var net = group.SellerOrders.Sum(x => x.NetAmountBeforeTax);
        var taxAndDuty = group.SellerOrders.Sum(x => x.TotalTaxAndDutyAmount);
        var grand = group.SellerOrders.Sum(x => x.GrandTotalSnapshot);
        var itemCount = InvoiceHeaderSemantics.LineCount(group.SellerOrders);
        var totalQty = group.SellerOrders.Sum(x => x.TotalQuantity);
        var showTotalQuantity = InvoiceHeaderSemantics.HasSharedUnit(group.SellerOrders);
        var paymentStatus = payment?.Status.ToString() ?? "—";
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<title>فاکتور ").Append(WebUtility.HtmlEncode(reference)).Append("</title>");
        sb.Append("<style>body{font-family:Tahoma,Arial,sans-serif;margin:24px;color:#111}");
        sb.Append("table{width:100%;border-collapse:collapse;margin-top:16px}th,td{border:1px solid #ccc;padding:8px;text-align:right}");
        sb.Append("h1{font-size:20px;margin:0 0 8px}.meta{color:#555;font-size:13px}.totals{margin-top:16px}");
        sb.Append("@media print{button{display:none}}</style></head><body>");
        sb.Append("<h1>فاکتور فروش</h1>");
        sb.Append("<p class=\"meta\">شماره سفارش: <strong dir=\"ltr\">").Append(WebUtility.HtmlEncode(reference)).Append("</strong></p>");
        sb.Append("<p class=\"meta\">تاریخ: <span dir=\"ltr\">").Append(group.SubmittedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append(" UTC</span></p>");
        sb.Append("<p class=\"meta\">گیرنده: ").Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(group.RecipientName) ? "—" : group.RecipientName));
        sb.Append(" · ").Append(WebUtility.HtmlEncode(group.ContactMobile));
        sb.Append("<br/>").Append(WebUtility.HtmlEncode($"{group.ProvinceName}، {group.CityName}"));
        sb.Append("<br/>").Append(WebUtility.HtmlEncode(group.PostalAddress)).Append("</p>");
        sb.Append("<table><thead><tr><th>کالا</th><th>تعداد</th><th>قیمت واحد</th><th>جمع خط</th></tr></thead><tbody>");
        foreach (var order in group.SellerOrders)
        {
            foreach (var line in order.Lines)
            {
                var title = $"کالا {line.CatalogVariantId.ToString("N")[..8]}";
                sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(title)).Append("</td>");
                sb.Append("<td>").Append(QuantityDisplay.Format(line.Quantity, line.QuantityDecimalPlacesSnapshot)).Append("</td>");
                sb.Append("<td dir=\"ltr\">").Append(FormatMoney(line.UnitPriceSnapshot, line.Currency)).Append("</td>");
                sb.Append("<td dir=\"ltr\">").Append(FormatMoney(line.LineTotalSnapshot, line.Currency)).Append("</td></tr>");
            }
        }

        sb.Append("</tbody></table><div class=\"totals\">");
        sb.Append("<p>تعداد اقلام: <strong dir=\"ltr\">").Append(itemCount.ToString(CultureInfo.InvariantCulture)).Append("</strong></p>");
        if (showTotalQuantity)
        {
            sb.Append("<p>جمع مقدار: <strong dir=\"ltr\">").Append(QuantityDisplay.Format(totalQty, 6)).Append("</strong></p>");
        }
        sb.Append("<p>جمع قبل از تخفیف: <strong dir=\"ltr\">").Append(FormatMoney(subtotal, currency)).Append("</strong></p>");
        sb.Append("<p>جمع تخفیفات: <strong dir=\"ltr\">").Append(FormatMoney(discount, currency)).Append("</strong></p>");
        sb.Append("<p>مبلغ پس از تخفیف / قبل از مالیات: <strong dir=\"ltr\">").Append(FormatMoney(net, currency)).Append("</strong></p>");
        sb.Append("<p>مالیات: <strong dir=\"ltr\">").Append(FormatMoney(tax, currency)).Append("</strong></p>");
        sb.Append("<p>عوارض: <strong dir=\"ltr\">").Append(FormatMoney(duty, currency)).Append("</strong></p>");
        sb.Append("<p>جمع مالیات و عوارض: <strong dir=\"ltr\">").Append(FormatMoney(taxAndDuty, currency)).Append("</strong></p>");
        sb.Append("<p>مبلغ قابل پرداخت: <strong dir=\"ltr\">").Append(FormatMoney(grand, currency)).Append("</strong></p>");
        sb.Append("<p>وضعیت پرداخت: ").Append(WebUtility.HtmlEncode(paymentStatus)).Append("</p>");
        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    internal static string RenderReceiptHtml(CheckoutGroup group, PaymentOperationalSnapshot payment)
    {
        var reference = MaskPaymentReference(payment);
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<title>رسید پرداخت</title>");
        sb.Append("<style>body{font-family:Tahoma,Arial,sans-serif;margin:24px;color:#111}");
        sb.Append(".row{margin:8px 0}@media print{button{display:none}}</style></head><body>");
        sb.Append("<h1>رسید پرداخت</h1>");
        sb.Append("<p class=\"row\">مبلغ: <strong dir=\"ltr\">").Append(FormatMoney(payment.Amount, payment.Currency)).Append("</strong></p>");
        sb.Append("<p class=\"row\">وضعیت: ").Append(WebUtility.HtmlEncode(payment.Status.ToString())).Append("</p>");
        sb.Append("<p class=\"row\">درگاه: ").Append(WebUtility.HtmlEncode(HumanizeProvider(payment.ProviderCode))).Append("</p>");
        sb.Append("<p class=\"row\">زمان تکمیل: <span dir=\"ltr\">");
        sb.Append((payment.CompletedAt ?? payment.UpdatedAt).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        sb.Append(" UTC</span></p>");
        sb.Append("<p class=\"row\">مرجع: <span dir=\"ltr\">").Append(WebUtility.HtmlEncode(reference)).Append("</span></p>");
        sb.Append("<p class=\"row\">سفارش: <span dir=\"ltr\">");
        sb.Append(WebUtility.HtmlEncode(group.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault() ?? group.CheckoutId.ToString("N")[..12]));
        sb.Append("</span></p></body></html>");
        return sb.ToString();
    }

    internal static string FormatMoney(decimal amount, string currency) =>
        string.Create(CultureInfo.InvariantCulture, $"{amount:0.####} {currency}");

    internal static string HumanizeProvider(string? providerCode)
    {
        var code = (providerCode ?? string.Empty).Trim().ToLowerInvariant();
        return code switch
        {
            "wallet" => "کیف پول",
            "fake" => "درگاه آزمایشی",
            "webhook" => "درگاه وب‌هوک",
            "fail-closed" => "درگاه غیرفعال",
            _ => string.IsNullOrWhiteSpace(providerCode) ? "—" : providerCode,
        };
    }

    internal static string MaskPaymentReference(PaymentOperationalSnapshot payment)
    {
        var tx = payment.ProviderTransactionReference?.Trim();
        if (!string.IsNullOrWhiteSpace(tx))
        {
            return tx.Length > 24 ? tx[..24] + "…" : tx;
        }

        var req = payment.ProviderRequestReference?.Trim();
        if (string.IsNullOrWhiteSpace(req))
        {
            return payment.PaymentId.ToString("N")[..12];
        }

        if (req.Contains('|', StringComparison.Ordinal))
        {
            var parts = req.Split('|');
            if (parts.Length > 1 && parts[0].Equals("w", StringComparison.OrdinalIgnoreCase))
            {
                var id = parts[1];
                return "wallet:" + (id.Length > 8 ? id[..8] + "…" : id);
            }
        }

        return req.Length > 24 ? req[..24] + "…" : req;
    }
}
