using System.Globalization;
using System.Net;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;

namespace Tooba.Order.Application.Admin.Completeness.Documents;

/// <summary>
/// HTML قابل‌چاپ فاکتور و رسید را فقط از snapshot سفارش می‌سازد — قیمت جاری Offer یا کمیسیون اینجا نیست.
/// </summary>
public static class AdminOrderDocumentRenderer
{
    /// <summary>فاکتور فروش از snapshot گروه checkout.</summary>
    public static string RenderInvoiceHtml(CheckoutGroup group, PaymentAdminOperationalSnapshot? payment)
    {
        ArgumentNullException.ThrowIfNull(group);
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
        var totalQuantity = group.SellerOrders.Sum(x => x.TotalQuantity);
        var showTotalQuantity = InvoiceHeaderSemantics.HasSharedUnit(group.SellerOrders);
        var paymentStatus = payment?.Status ?? "—";

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<title>فاکتور ").Append(WebUtility.HtmlEncode(reference)).Append("</title>");
        sb.Append("<style>body{font-family:Tahoma,Arial,sans-serif;margin:24px;color:#111}");
        sb.Append("table{width:100%;border-collapse:collapse;margin-top:16px}th,td{border:1px solid #ccc;padding:8px;text-align:right}");
        sb.Append("h1{font-size:20px;margin:0 0 8px}.meta{color:#555;font-size:13px}.totals{margin-top:16px}");
        sb.Append("@media print{button{display:none}}</style></head><body>");
        sb.Append("<h1>فاکتور فروش</h1>");
        sb.Append("<p class=\"meta\">شماره سفارش: <strong dir=\"ltr\">").Append(WebUtility.HtmlEncode(reference)).Append("</strong></p>");
        sb.Append("<p class=\"meta\">تاریخ: <span dir=\"ltr\">")
            .Append(group.SubmittedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
            .Append(" UTC</span></p>");
        var recipient = RecipientDisplay(group);
        sb.Append("<p class=\"meta\">گیرنده: ")
            .Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(recipient) ? "—" : recipient));
        sb.Append(" · ").Append(WebUtility.HtmlEncode(group.ContactMobile));
        sb.Append("<br/>").Append(WebUtility.HtmlEncode($"{group.ProvinceName}، {group.CityName}"));
        sb.Append("<br/>").Append(WebUtility.HtmlEncode(group.PostalAddress)).Append("</p>");
        sb.Append("<table><thead><tr><th>کالا</th><th>تعداد</th><th>قیمت واحد</th><th>جمع خط</th></tr></thead><tbody>");
        foreach (var line in group.SellerOrders.SelectMany(x => x.Lines))
        {
            sb.Append("<tr><td>")
                .Append(WebUtility.HtmlEncode($"کالا {line.CatalogVariantId.ToString("N")[..8]}"))
                .Append("</td>");
            sb.Append("<td>").Append(QuantityDisplay.Format(line.Quantity, line.QuantityDecimalPlacesSnapshot)).Append("</td>");
            sb.Append("<td dir=\"ltr\">").Append(FormatMoney(line.UnitPriceSnapshot, line.Currency)).Append("</td>");
            sb.Append("<td dir=\"ltr\">").Append(FormatMoney(line.LineTotalSnapshot, line.Currency)).Append("</td></tr>");
        }

        sb.Append("</tbody></table><div class=\"totals\">");
        sb.Append("<p>تعداد اقلام: <strong dir=\"ltr\">").Append(itemCount.ToString(CultureInfo.InvariantCulture)).Append("</strong></p>");
        if (showTotalQuantity)
        {
            sb.Append("<p>جمع مقدار: <strong dir=\"ltr\">").Append(QuantityDisplay.Format(totalQuantity, 6)).Append("</strong></p>");
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

    /// <summary>رسید پرداخت با مرجع ماسک‌شده.</summary>
    public static string RenderReceiptHtml(CheckoutGroup group, PaymentAdminOperationalSnapshot payment)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(payment);
        var reference = MaskPaymentReference(payment);
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<title>رسید پرداخت</title>");
        sb.Append("<style>body{font-family:Tahoma,Arial,sans-serif;margin:24px;color:#111}");
        sb.Append(".row{margin:8px 0}@media print{button{display:none}}</style></head><body>");
        sb.Append("<h1>رسید پرداخت</h1>");
        sb.Append("<p class=\"row\">مبلغ: <strong dir=\"ltr\">").Append(FormatMoney(payment.Amount, payment.Currency)).Append("</strong></p>");
        sb.Append("<p class=\"row\">وضعیت: ").Append(WebUtility.HtmlEncode(payment.Status)).Append("</p>");
        sb.Append("<p class=\"row\">درگاه: ").Append(WebUtility.HtmlEncode(HumanizeProvider(payment.ProviderCode))).Append("</p>");
        sb.Append("<p class=\"row\">زمان تکمیل: <span dir=\"ltr\">");
        sb.Append((payment.CompletedAt ?? payment.UpdatedAt).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        sb.Append(" UTC</span></p>");
        sb.Append("<p class=\"row\">مرجع: <span dir=\"ltr\">").Append(WebUtility.HtmlEncode(reference)).Append("</span></p>");
        sb.Append("<p class=\"row\">سفارش: <span dir=\"ltr\">");
        sb.Append(WebUtility.HtmlEncode(
            group.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault() ?? group.CheckoutId.ToString("N")[..12]));
        sb.Append("</span></p></body></html>");
        return sb.ToString();
    }

    /// <summary>مبلغ با واحد پول، بدون محلی‌سازی ارقام (مسیر چاپ ltr است).</summary>
    public static string FormatMoney(decimal amount, string currency) =>
        string.Create(CultureInfo.InvariantCulture, $"{amount:0.####} {currency}");

    /// <summary>نام انسانی درگاه پرداخت.</summary>
    public static string HumanizeProvider(string? providerCode) =>
        (providerCode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "wallet" => "کیف پول",
            "fake" => "درگاه آزمایشی",
            "webhook" => "درگاه وب‌هوک",
            "fail-closed" => "درگاه غیرفعال",
            _ => string.IsNullOrWhiteSpace(providerCode) ? "—" : providerCode,
        };

    /// <summary>مرجع پرداخت را کوتاه و ماسک می‌کند تا راز درگاه در رسید چاپ نشود.</summary>
    public static string MaskPaymentReference(PaymentAdminOperationalSnapshot payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        var transaction = payment.ProviderTransactionReference?.Trim();
        if (!string.IsNullOrWhiteSpace(transaction))
        {
            return transaction.Length > 24 ? transaction[..24] + "…" : transaction;
        }

        var request = payment.ProviderRequestReference?.Trim();
        if (string.IsNullOrWhiteSpace(request))
        {
            return payment.PaymentId.ToString("N")[..12];
        }

        if (request.Contains('|', StringComparison.Ordinal))
        {
            var parts = request.Split('|');
            if (parts.Length > 1 && parts[0].Equals("w", StringComparison.OrdinalIgnoreCase))
            {
                var id = parts[1];
                return "wallet:" + (id.Length > 8 ? id[..8] + "…" : id);
            }
        }

        return request.Length > 24 ? request[..24] + "…" : request;
    }

    /// <summary>
    /// نام گیرنده: First+Last اگر هر دو موجود باشند، وگرنه RecipientName قدیمی بدون شکستن حدسی.
    /// </summary>
    public static string RecipientDisplay(CheckoutGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        var first = group.RecipientFirstName?.Trim() ?? string.Empty;
        var last = group.RecipientLastName?.Trim() ?? string.Empty;
        return first.Length > 0 && last.Length > 0
            ? $"{first} {last}"
            : group.RecipientName?.Trim() ?? string.Empty;
    }
}
