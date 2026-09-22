using System.Globalization;
using System.Net;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;

namespace Tooba.Host.Admin;

internal static class AdminOrderCompletenessComposer
{
    internal readonly record struct ActorLabel(string Kind, string DisplayName, string DisplayFa, string DisplayEn)
    {
        public static ActorLabel System() => new("system", "سیستم", "توسط سیستم", "By system");
        public static ActorLabel User(string name) => new("user", name, $"توسط {name}", $"By {name}");
        public static ActorLabel MissingUser() => new("user", "کاربر نامشخص", "توسط کاربر نامشخص", "By unknown user");
    }

    internal static ActorLabel ResolveLabel(Guid? id, IReadOnlyDictionary<Guid, ActorLabel> labels) =>
        id is null || id == Guid.Empty ? ActorLabel.System() :
        labels.TryGetValue(id.Value, out var value) ? value : ActorLabel.MissingUser();

    internal static string FormatPackScopeFa(string seller, decimal quantity) =>
        $"{(string.IsNullOrWhiteSpace(seller) ? "فروشنده" : seller)} — {Fa(quantity)} قلم";
    internal static string FormatProductQtyScopeFa(string title, decimal quantity) =>
        $"{(string.IsNullOrWhiteSpace(title) ? "کالای سفارش" : title)} — تعداد {Fa(quantity)}";

    internal static string RenderInvoiceHtml(CheckoutGroup group, PaymentAdminOperationalSnapshot? payment)
    {
        var sb = new StringBuilder("<h1>فاکتور</h1>");
        foreach (var line in group.SellerOrders.SelectMany(x => x.Lines))
            sb.Append(line.UnitPriceSnapshot.ToString("0.####", CultureInfo.InvariantCulture)).Append(' ').Append(line.Currency);
        sb.Append("تعداد اقلام: <strong dir=\"ltr\">").Append(InvoiceHeaderSemantics.LineCount(group.SellerOrders)).Append("</strong>");
        if (InvoiceHeaderSemantics.HasSharedUnit(group.SellerOrders))
            sb.Append("جمع مقدار: <strong dir=\"ltr\">").Append(QuantityDisplay.Format(group.SellerOrders.Sum(x => x.TotalQuantity), 6)).Append("</strong>");
        return sb.ToString();
    }

    internal static string RenderReceiptHtml(CheckoutGroup group, PaymentAdminOperationalSnapshot payment)
    {
        var request = payment.ProviderRequestReference ?? string.Empty;
        var reference = request.StartsWith("w|", StringComparison.OrdinalIgnoreCase)
            ? "wallet:" + request[2..][..Math.Min(8, request.Length - 2)]
            : payment.ProviderTransactionReference ?? payment.PaymentId.ToString("N")[..12];
        return $"رسید {(payment.ProviderCode == "wallet" ? "کیف پول" : WebUtility.HtmlEncode(payment.ProviderCode))} {payment.Amount:0.####} {payment.Currency} {reference}";
    }

    private static string Fa(decimal value)
    {
        const string digits = "۰۱۲۳۴۵۶۷۸۹";
        return string.Concat(QuantityDisplay.Format(value, 6).Select(x => x is >= '0' and <= '9' ? digits[x - '0'] : x));
    }
}
