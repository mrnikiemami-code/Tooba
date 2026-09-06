using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Identity.Application;
using Tooba.OperatorProfile.Application;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
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
    private readonly ICheckoutDirectory _checkout;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly IPaymentAdminDirectory _payments;
    private readonly ISettlementDirectory _settlement;
    private readonly IAccessControlDirectory _access;
    private readonly IOperatorProfileDirectory _profiles;
    private readonly IIdentityContactLookup _contacts;
    private readonly ICurrentTenant _tenant;

    /// <summary>ترکیب‌گر را به ماژول‌های موجود وصل می‌کند.</summary>
    public AdminOrderCompletenessComposer(
        OrderDbContext orders,
        ReturnsDbContext returns,
        ICheckoutDirectory checkout,
        IFulfillmentDirectory fulfillment,
        IPaymentAdminDirectory payments,
        ISettlementDirectory settlement,
        IAccessControlDirectory access,
        IOperatorProfileDirectory profiles,
        IIdentityContactLookup contacts,
        ICurrentTenant tenant)
    {
        _orders = orders;
        _returns = returns;
        _checkout = checkout;
        _fulfillment = fulfillment;
        _payments = payments;
        _settlement = settlement;
        _access = access;
        _profiles = profiles;
        _contacts = contacts;
        _tenant = tenant;
    }

    /// <summary>یادداشت‌های داخلی را فهرست می‌کند (order.view).</summary>
    public async Task<IReadOnlyList<AdminOrderNoteView>> ListNotesAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(actorUserId, "order.view", cancellationToken);
        await EnsureCheckoutExistsAsync(checkoutId, cancellationToken);
        var notes = await _checkout.ListNotesAsync(checkoutId, 50, cancellationToken);
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

    /// <summary>تاریخچهٔ عملیاتی ترکیبی را صفحه می‌کند (order.view).</summary>
    public async Task<AdminOperationalHistoryPage> ListOperationalHistoryAsync(
        Guid checkoutId,
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(actorUserId, "order.view", cancellationToken);
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
                "لغو سفارش",
                "Order cancelled",
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
                        "payment_failed",
                        "پرداخت ناموفق",
                        "Payment failed",
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
            }
        }

        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        foreach (var f in fulfillments)
        {
            if (f.Status is FulfillmentStatus.Processing or FulfillmentStatus.Packed
                or FulfillmentStatus.Dispatched or FulfillmentStatus.InTransit or FulfillmentStatus.Delivered)
            {
                entries.Add(Draft(
                    f.CreatedAt == default ? f.UpdatedAt : f.CreatedAt,
                    "fulfillment_processing",
                    "آماده‌سازی",
                    "Processing",
                    null));
            }

            if (f.Status is FulfillmentStatus.Packed or FulfillmentStatus.Dispatched
                or FulfillmentStatus.InTransit or FulfillmentStatus.Delivered)
            {
                entries.Add(Draft(
                    f.UpdatedAt == default ? f.CreatedAt : f.UpdatedAt,
                    "fulfillment_packed",
                    "بسته‌بندی",
                    "Packed",
                    null));
            }

            foreach (var shipment in f.Shipments)
            {
                var created = shipment.CreatedAt == default ? f.UpdatedAt : shipment.CreatedAt;
                entries.Add(Draft(
                    created == default ? f.CreatedAt : created,
                    "shipment_created",
                    "ایجاد محموله",
                    "Shipment created",
                    null,
                    shipment.CarrierDisplayName,
                    shipment.CarrierDisplayName));
                if (!string.IsNullOrWhiteSpace(shipment.TrackingReference))
                {
                    entries.Add(Draft(
                        shipment.DispatchedAt ?? created,
                        "tracking_assigned",
                        "ثبت رهگیری",
                        "Tracking assigned",
                        null,
                        shipment.TrackingReference,
                        shipment.TrackingReference));
                }

                if (shipment.DispatchedAt is { } dispatched)
                {
                    entries.Add(Draft(
                        dispatched,
                        "shipment_dispatched",
                        "ارسال محموله",
                        "Shipment dispatched",
                        null));
                }

                if (shipment.DeliveredAt is { } delivered)
                {
                    entries.Add(Draft(
                        delivered,
                        "shipment_delivered",
                        "تحویل محموله",
                        "Shipment delivered",
                        null));
                }
            }
        }

        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var returnIds = returns.Select(x => x.ReturnRequestId).ToList();
        var refundAttempts = returnIds.Count == 0
            ? []
            : await _returns.RefundAttempts.AsNoTracking()
                .Where(x => returnIds.Contains(x.ReturnRequestId))
                .ToListAsync(cancellationToken);
        var attemptsByReturn = refundAttempts.GroupBy(x => x.ReturnRequestId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var ret in returns)
        {
            entries.Add(Draft(
                ret.CreatedAt,
                "return_requested",
                "درخواست مرجوعی",
                "Return requested",
                ret.RequestedByUserId == Guid.Empty ? null : ret.RequestedByUserId,
                ret.Reason,
                ret.Reason));
            if (ret.Status is ReturnRequestStatus.Approved or ReturnRequestStatus.RefundProcessing
                or ReturnRequestStatus.Completed or ReturnRequestStatus.RefundFailed)
            {
                entries.Add(Draft(
                    ret.UpdatedAt,
                    "return_approved",
                    "تأیید مرجوعی",
                    "Return approved",
                    null));
            }

            if (ret.Status == ReturnRequestStatus.Rejected)
            {
                entries.Add(Draft(
                    ret.UpdatedAt,
                    "return_rejected",
                    "رد مرجوعی",
                    "Return rejected",
                    null));
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
                        $"{attempt.Amount:0} {attempt.Currency}",
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
                if (string.Equals(entry.SourceType, "refund", StringComparison.OrdinalIgnoreCase)
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

        var notes = await _checkout.ListNotesAsync(group.CheckoutId, 50, cancellationToken);
        foreach (var note in notes)
        {
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
            label.DisplayEn);
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
        var discount = group.SellerOrders.Sum(x => x.DiscountSnapshot);
        var grand = group.SellerOrders.Sum(x => x.GrandTotalSnapshot);
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
                sb.Append("<td>").Append(line.Quantity.ToString(CultureInfo.InvariantCulture)).Append("</td>");
                sb.Append("<td dir=\"ltr\">").Append(FormatMoney(line.UnitPriceSnapshot, line.Currency)).Append("</td>");
                sb.Append("<td dir=\"ltr\">").Append(FormatMoney(line.LineTotalSnapshot, line.Currency)).Append("</td></tr>");
            }
        }

        sb.Append("</tbody></table><div class=\"totals\">");
        sb.Append("<p>جمع جزء: <strong dir=\"ltr\">").Append(FormatMoney(subtotal, currency)).Append("</strong></p>");
        sb.Append("<p>مالیات: <strong dir=\"ltr\">").Append(FormatMoney(tax, currency)).Append("</strong></p>");
        sb.Append("<p>تخفیف: <strong dir=\"ltr\">").Append(FormatMoney(discount, currency)).Append("</strong></p>");
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
