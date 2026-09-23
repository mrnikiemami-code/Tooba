using Tooba.Catalog.Contracts;
using Tooba.Order.Application.Customer.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Domain;
using Tooba.Party.Contracts;
using Tooba.Payment.Contracts.Customer;

namespace Tooba.Order.Application.Customer;

/// <summary>
/// ترکیب read-model سفارش مشتری از Order + Contracts بیگانه (Catalog/Party/Payment).
/// </summary>
public sealed class CustomerOrderComposer
{
    private readonly IPartyLookup _parties;
    private readonly ICatalogVariantLookup _catalog;
    private readonly IPaymentCustomerGateway _payments;

    public CustomerOrderComposer(
        IPartyLookup parties,
        ICatalogVariantLookup catalog,
        IPaymentCustomerGateway payments)
    {
        _parties = parties;
        _catalog = catalog;
        _payments = payments;
    }

    /// <summary>آیتم فهرست را از Checkout و وضعیت پرداخت می‌سازد.</summary>
    public async Task<CustomerOrderListItem> MapListItemAsync(
        CheckoutGroup group,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetLatestForCheckoutAsync(
            group.CheckoutId,
            actorUserId,
            group.BuyerPartyId,
            cancellationToken);
        return MapListItem(group, PaymentState(payment));
    }

    /// <summary>صفحهٔ جزئیات را برای Checkout متعلق به Actor می‌سازد.</summary>
    public async Task<CustomerOrderDetailPage> ComposeDetailAsync(
        CheckoutGroup group,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetLatestForCheckoutAsync(
            group.CheckoutId,
            actorUserId,
            group.BuyerPartyId,
            cancellationToken);
        var paymentState = PaymentState(payment);

        var variantIds = group.SellerOrders
            .SelectMany(x => x.Lines)
            .Select(x => x.CatalogVariantId)
            .Distinct()
            .ToList();
        var titles = variantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _catalog.GetVariantTitlesAsync(variantIds, cancellationToken);

        var sellerIds = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerNames = sellerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _parties.GetDisplayNamesAsync(sellerIds, cancellationToken);

        var sellerViews = new List<CustomerSellerOrderView>();
        foreach (var sellerOrder in group.SellerOrders)
        {
            sellerNames.TryGetValue(sellerOrder.SellerPartyId, out var sellerName);
            sellerName = string.IsNullOrWhiteSpace(sellerName) ? "فروشنده" : sellerName;
            var lineViews = sellerOrder.Lines.Select(line =>
            {
                titles.TryGetValue(line.CatalogVariantId, out var title);
                return new CustomerOrderLineView(
                    line.OfferId,
                    string.IsNullOrWhiteSpace(title) ? "کالای سفارش" : title,
                    sellerName,
                    line.Quantity,
                    line.UnitPriceSnapshot,
                    line.LineTotalSnapshot + line.TaxAmountSnapshot - line.DiscountAmountSnapshot,
                    line.Currency);
            }).ToList();
            sellerViews.Add(new CustomerSellerOrderView(
                sellerOrder.SellerOrderId,
                sellerOrder.OrderNumber,
                sellerOrder.SellerPartyId,
                sellerName,
                sellerOrder.Status.ToString(),
                PaymentState(payment, sellerOrder.SellerOrderId),
                sellerOrder.GrandTotalSnapshot,
                sellerOrder.Currency,
                lineViews));
        }

        var listItem = MapListItem(group, paymentState);
        return new CustomerOrderDetailPage(
            group.CheckoutId,
            listItem.Reference,
            group.SubmittedAt,
            listItem.Status,
            listItem.PaymentState,
            group.SellerOrders.Sum(x => x.SubtotalSnapshot),
            group.SellerOrders.Sum(x => x.TaxSnapshot),
            group.SellerOrders.Sum(x => x.DiscountSnapshot),
            group.SellerOrders.Sum(x => x.GrandTotalSnapshot),
            group.SellerOrders.Select(x => x.Currency).FirstOrDefault() ?? "IRR",
            StorefrontRecipientNames.Display(group.RecipientFirstName, group.RecipientLastName, group.RecipientName),
            group.ContactMobile,
            group.ProvinceName,
            group.CityName,
            group.PostalAddress,
            group.PostalCode,
            group.ShippingMethodLabel,
            sellerViews,
            payment?.PaymentId,
            payment?.Status == "Expired");
    }

    /// <summary>خلاصهٔ داشبورد را از فهرست آیتم‌ها می‌سازد.</summary>
    public static CustomerOrderDashboardSummary BuildDashboardSummary(
        IReadOnlyList<CustomerOrderListItem> orders,
        CheckoutGroup? latest)
    {
        var recipient = latest is null
            ? null
            : StorefrontRecipientNames.Display(
                latest.RecipientFirstName,
                latest.RecipientLastName,
                latest.RecipientName);
        if (string.IsNullOrWhiteSpace(recipient))
        {
            recipient = null;
        }

        return new CustomerOrderDashboardSummary(
            orders.Count,
            orders.Count(x =>
                !string.Equals(x.PaymentState, "Paid", StringComparison.Ordinal)
                && !string.Equals(x.PaymentState, "Cancelled", StringComparison.Ordinal)),
            orders.Count(x => string.Equals(x.PaymentState, "Paid", StringComparison.Ordinal)),
            orders.Take(5).ToList(),
            recipient,
            FormatShippingAddress(latest),
            string.IsNullOrWhiteSpace(latest?.ContactMobile) ? null : latest!.ContactMobile);
    }

    private static CustomerOrderListItem MapListItem(CheckoutGroup group, string payment)
    {
        var orders = group.SellerOrders;
        var statuses = orders.Select(x => x.Status).Distinct().ToList();
        var status = statuses.Count == 1 ? statuses[0].ToString() : "Mixed";
        var paymentState = orders.Count > 0 && orders.All(x => x.Status == SellerOrderStatus.Cancelled)
            ? "Cancelled"
            : payment;
        var references = orders.Select(x => x.OrderNumber).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        return new CustomerOrderListItem(
            group.CheckoutId,
            references.Count == 0 ? group.CheckoutId.ToString("N")[..12] : string.Join(" / ", references),
            group.SubmittedAt,
            orders.Count,
            orders.Sum(x => x.TotalItemCount),
            orders.Sum(x => x.GrandTotalSnapshot),
            orders.Select(x => x.Currency).FirstOrDefault() ?? "IRR",
            paymentState,
            status);
    }

    private static string? FormatShippingAddress(CheckoutGroup? latest)
    {
        if (latest is null)
        {
            return null;
        }

        var address = string.Join("، ", new[] { latest.ProvinceName, latest.CityName, latest.PostalAddress }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(address) ? null : address;
    }

    private static string PaymentState(PaymentCustomerSnapshot? payment) =>
        payment?.Status switch
        {
            "Succeeded"
                or "RefundPending"
                or "Refunded"
                or "RefundFailed" => "Paid",
            "Failed" or "Cancelled" => "Failed",
            "Expired" => "PaymentExpired",
            _ => "PendingPayment",
        };

    private static string PaymentState(PaymentCustomerSnapshot? payment, Guid sellerOrderId) =>
        payment is null || payment.Allocations.Any(x => x.SellerOrderId == sellerOrderId)
            ? PaymentState(payment)
            : "PendingPayment";
}
