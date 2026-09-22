using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Ports;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Contracts.Admin;

namespace Tooba.Order.Infrastructure.Admin;

internal sealed class AdminOrderCompletenessStore(
    OrderDbContext db,
    ICheckoutDirectory checkout,
    IPaymentAdminGateway payments) : IAdminOrderCompletenessStore
{
    public Task<bool> ExistsAsync(Guid checkoutId, CancellationToken ct) =>
        db.Checkouts.AsNoTracking().AnyAsync(x => x.CheckoutId == checkoutId, ct);

    public async Task<IReadOnlyList<AdminOrderNoteView>> ListNotesAsync(Guid checkoutId, Guid actorUserId, CancellationToken ct)
    {
        await checkout.RecordAdminViewAsync(checkoutId, actorUserId, ct);
        var notes = await checkout.ListNotesAsync(checkoutId, actorUserId, 50, ct);
        return notes.Select(Map).ToList();
    }

    public async Task<AdminOrderNoteView> AddNoteAsync(Guid checkoutId, Guid actorUserId, string body, CancellationToken ct) =>
        Map(await checkout.AddNoteAsync(checkoutId, actorUserId, body, ct));

    public async Task<AdminOrderNoteDeleteOutcome> DeleteNoteAsync(Guid checkoutId, Guid noteId, Guid actorUserId, CancellationToken ct) =>
        await checkout.DeleteNoteAsync(checkoutId, noteId, actorUserId, ct) switch
        {
            CheckoutNoteDeleteOutcome.Deleted => AdminOrderNoteDeleteOutcome.Deleted,
            CheckoutNoteDeleteOutcome.Forbidden => AdminOrderNoteDeleteOutcome.Forbidden,
            CheckoutNoteDeleteOutcome.NotFound => AdminOrderNoteDeleteOutcome.NotFound,
            _ => throw new InvalidOperationException("Unknown checkout note deletion outcome.")
        };

    public async Task<AdminOrderOperationalHistoryPage?> GetHistoryAsync(Guid checkoutId, Guid actorUserId, int page, int pageSize, CancellationToken ct)
    {
        var group = await db.Checkouts.AsNoTracking().Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, ct);
        if (group is null) return null;
        await checkout.RecordAdminViewAsync(checkoutId, actorUserId, ct);
        var entries = new List<AdminOrderHistoryEntry>
        {
            new(group.SubmittedAt, "order_created", "ثبت سفارش", "Order created", "system", "سیستم", "توسط سیستم", "By system", null, $"Checkout {group.CheckoutId:N}"[..20])
        };
        entries.AddRange(group.SellerOrders.Where(x => x.Status == Domain.SellerOrderStatus.Cancelled)
            .Select(x => new AdminOrderHistoryEntry(group.SubmittedAt, "order_cancelled", "سفارش لغو شد", "Order cancelled", "system", "سیستم", "توسط سیستم", "By system", $"سفارش {x.OrderNumber}", $"Order {x.OrderNumber}")));
        var notes = await checkout.ListNotesAsync(checkoutId, Guid.Empty, 50, ct);
        entries.AddRange(notes.Select(x => new AdminOrderHistoryEntry(x.CreatedAt, "operational_note", "یادداشت داخلی", "Internal note", "user", "کاربر نامشخص", "توسط کاربر نامشخص", "By unknown user", Truncate(x.Body), Truncate(x.Body))));
        var ordered = entries.OrderByDescending(x => x.OccurredAt).ToList();
        return new(checkoutId, page, pageSize, ordered.Count, ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList());
    }

    public async Task<string?> GetInvoiceHtmlAsync(Guid checkoutId, CancellationToken ct)
    {
        var group = await db.Checkouts.AsNoTracking().Include(x => x.SellerOrders).ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, ct);
        if (group is null) return null;
        var reference = group.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault() ?? checkoutId.ToString("N")[..12];
        var subtotal = group.SellerOrders.Sum(x => x.SubtotalSnapshot);
        var tax = group.SellerOrders.Sum(x => x.TaxSnapshot);
        var duty = group.SellerOrders.Sum(x => x.TotalDutyAmount);
        var discount = group.SellerOrders.Sum(x => x.DiscountSnapshot);
        var net = group.SellerOrders.Sum(x => x.NetAmountBeforeTax);
        var taxAndDuty = group.SellerOrders.Sum(x => x.TotalTaxAndDutyAmount);
        var grand = group.SellerOrders.Sum(x => x.GrandTotalSnapshot);
        var currency = group.SellerOrders.Select(x => x.Currency).FirstOrDefault() ?? group.Currency;
        var payment = await payments.GetLatestOperationalForCheckoutAsync(checkoutId, ct);
        var lines = new StringBuilder();
        foreach (var line in group.SellerOrders.SelectMany(x => x.Lines))
        {
            lines.Append("<tr><td>کالا ").Append(line.CatalogVariantId.ToString("N")[..8]).Append("</td><td>")
                .Append(QuantityDisplay.Format(line.Quantity, line.QuantityDecimalPlacesSnapshot))
                .Append("</td><td dir=\"ltr\">").Append(Money(line.UnitPriceSnapshot, line.Currency))
                .Append("</td><td dir=\"ltr\">").Append(Money(line.LineTotalSnapshot, line.Currency))
                .Append("</td></tr>");
        }
        var recipient = string.Join(' ', new[] { group.RecipientFirstName, group.RecipientLastName }
            .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (string.IsNullOrWhiteSpace(recipient)) recipient = group.RecipientName;
        var itemCount = group.SellerOrders.Sum(x => x.Lines.Count);
        var body = new StringBuilder()
            .Append($"<p class=\"meta\">شماره سفارش: <strong dir=\"ltr\">{WebUtility.HtmlEncode(reference)}</strong></p>")
            .Append($"<p class=\"meta\">تاریخ: <span dir=\"ltr\">{group.SubmittedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC</span></p>")
            .Append($"<p class=\"meta\">گیرنده: {WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(recipient) ? "—" : recipient)} · {WebUtility.HtmlEncode(group.ContactMobile)}<br/>{WebUtility.HtmlEncode($"{group.ProvinceName}، {group.CityName}")}<br/>{WebUtility.HtmlEncode(group.PostalAddress)}</p>")
            .Append("<table><thead><tr><th>کالا</th><th>تعداد</th><th>قیمت واحد</th><th>جمع خط</th></tr></thead><tbody>")
            .Append(lines).Append("</tbody></table><div class=\"totals\">")
            .Append($"<p>تعداد اقلام: <strong dir=\"ltr\">{itemCount}</strong></p>")
            .Append($"<p>جمع قبل از تخفیف: <strong dir=\"ltr\">{Money(subtotal, currency)}</strong></p>")
            .Append($"<p>جمع تخفیفات: <strong dir=\"ltr\">{Money(discount, currency)}</strong></p>")
            .Append($"<p>مبلغ پس از تخفیف / قبل از مالیات: <strong dir=\"ltr\">{Money(net, currency)}</strong></p>")
            .Append($"<p>مالیات: <strong dir=\"ltr\">{Money(tax, currency)}</strong></p>")
            .Append($"<p>عوارض: <strong dir=\"ltr\">{Money(duty, currency)}</strong></p>")
            .Append($"<p>جمع مالیات و عوارض: <strong dir=\"ltr\">{Money(taxAndDuty, currency)}</strong></p>")
            .Append($"<p>مبلغ قابل پرداخت: <strong dir=\"ltr\">{Money(grand, currency)}</strong></p>")
            .Append($"<p>وضعیت پرداخت: {WebUtility.HtmlEncode(payment?.Status ?? "—")}</p></div>")
            .ToString();
        return Document("فاکتور فروش", body);
    }

    public async Task<string?> GetReceiptHtmlAsync(Guid checkoutId, CancellationToken ct)
    {
        var group = await db.Checkouts.AsNoTracking().Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, ct);
        if (group is null) return null;
        var payment = await payments.GetLatestOperationalForCheckoutAsync(checkoutId, ct);
        if (payment is null) return null;
        var reference = MaskPaymentReference(payment);
        var provider = HumanizeProvider(payment.ProviderCode);
        var orderReference = group.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault() ?? group.CheckoutId.ToString("N")[..12];
        return Document("رسید پرداخت",
            $"<p class=\"row\">مبلغ: <strong dir=\"ltr\">{Money(payment.Amount, payment.Currency)}</strong></p>" +
            $"<p class=\"row\">وضعیت: {WebUtility.HtmlEncode(payment.Status)}</p>" +
            $"<p class=\"row\">درگاه: {WebUtility.HtmlEncode(provider)}</p>" +
            $"<p class=\"row\">زمان تکمیل: <span dir=\"ltr\">{(payment.CompletedAt ?? payment.UpdatedAt).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC</span></p>" +
            $"<p class=\"row\">مرجع: <span dir=\"ltr\">{WebUtility.HtmlEncode(reference)}</span></p>" +
            $"<p class=\"row\">سفارش: <span dir=\"ltr\">{WebUtility.HtmlEncode(orderReference)}</span></p>");
    }

    private static AdminOrderNoteView Map(CheckoutOperationalNoteSnapshot x) =>
        new(x.NoteId, x.CheckoutId, x.Body, x.CreatedByUserId, x.CreatedAt,
            "user", "کاربر نامشخص", "توسط کاربر نامشخص", "By unknown user", x.CanDelete);
    private static string Truncate(string value) => value.Length <= 120 ? value : value[..119] + "…";
    private static string Money(decimal amount, string currency) =>
        string.Create(CultureInfo.InvariantCulture, $"{amount:0.####} {currency}");
    private static string HumanizeProvider(string? providerCode) => (providerCode ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "wallet" => "کیف پول",
        "fake" => "درگاه آزمایشی",
        "webhook" => "درگاه وب‌هوک",
        "fail-closed" => "درگاه غیرفعال",
        _ => string.IsNullOrWhiteSpace(providerCode) ? "—" : providerCode
    };
    private static string MaskPaymentReference(PaymentAdminOperationalSnapshot payment)
    {
        var tx = payment.ProviderTransactionReference?.Trim();
        if (!string.IsNullOrWhiteSpace(tx)) return tx.Length > 24 ? tx[..24] + "…" : tx;
        var request = payment.ProviderRequestReference?.Trim();
        if (string.IsNullOrWhiteSpace(request)) return payment.PaymentId.ToString("N")[..12];
        var parts = request.Split('|');
        if (parts.Length > 1 && parts[0].Equals("w", StringComparison.OrdinalIgnoreCase))
            return "wallet:" + (parts[1].Length > 8 ? parts[1][..8] + "…" : parts[1]);
        return request.Length > 24 ? request[..24] + "…" : request;
    }
    private static string Document(string title, string body) =>
        $"<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\"/><title>{title}</title><style>body{{font-family:Tahoma,Arial,sans-serif;margin:24px;color:#111}}table{{width:100%;border-collapse:collapse;margin-top:16px}}th,td{{border:1px solid #ccc;padding:8px;text-align:right}}h1{{font-size:20px;margin:0 0 8px}}.meta{{color:#555;font-size:13px}}.row{{margin:8px 0}}@media print{{button{{display:none}}}}</style></head><body><h1>{title}</h1>{body}</body></html>";
}
