using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Payment.Application;

namespace Tooba.Host.Admin;

/// <summary>
/// read model باریک مدیر را با پرس‌وجوی مستقل هر DbContext و ترکیب در حافظه می‌سازد.
/// هیچ JOIN بین schemaها یا دسترسی مستقیم frontend به پایگاه داده وجود ندارد.
/// </summary>
public sealed class AdminPanelComposer
{
    private readonly CatalogDbContext _catalog;
    private readonly OfferDbContext _offers;
    private readonly OrderDbContext _orders;
    private readonly PartyDbContext _parties;
    private readonly IPaymentAdminDirectory _payments;

    /// <summary>
    /// ترکیب‌گر Host را با contextهای مستقل ماژول‌ها می‌سازد.
    /// </summary>
    public AdminPanelComposer(
        CatalogDbContext catalog,
        OfferDbContext offers,
        OrderDbContext orders,
        PartyDbContext parties,
        IPaymentAdminDirectory payments)
    {
        _catalog = catalog;
        _offers = offers;
        _orders = orders;
        _parties = parties;
        _payments = payments;
    }

    /// <summary>
    /// خلاصهٔ واقعی داشبورد را بدون نمودار یا درآمد ساختگی برمی‌گرداند.
    /// </summary>
    public async Task<AdminDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var publishedProducts = await _catalog.Products.AsNoTracking()
            .CountAsync(x => x.Status == CatalogPublicationStatus.Published, cancellationToken);
        var activeOffers = await _offers.Offers.AsNoTracking()
            .CountAsync(x => x.Status == OfferStatus.Active, cancellationToken);
        var statuses = await _orders.SellerOrders.AsNoTracking()
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var sellerIds = await _offers.Offers.AsNoTracking()
            .Select(x => x.SellerPartyId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var customers = await _orders.Checkouts.AsNoTracking()
            .Select(x => x.PlacedByUserId)
            .Distinct()
            .CountAsync(cancellationToken);
        var paid = statuses.Count(x => x == SellerOrderStatus.Paid);
        var pending = statuses.Count(x => x is SellerOrderStatus.PendingPayment or SellerOrderStatus.Submitted);
        var open = statuses.Count(x => x is not SellerOrderStatus.Paid and not SellerOrderStatus.Cancelled);
        return new AdminDashboardSummary(
            publishedProducts,
            activeOffers,
            open,
            paid,
            pending,
            sellerIds.Count,
            customers);
    }

    /// <summary>
    /// Checkoutها را به ردیف‌های سفارش مدیر با snapshot مشتری و مبلغ تبدیل می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<AdminOrderListItem>> ListOrdersAsync(CancellationToken cancellationToken)
    {
        var groups = await LoadOrderGroupsAsync(cancellationToken);
        return groups.Select(MapOrderListItem).ToList();
    }

    /// <summary>
    /// جزئیات Checkout را از Order می‌خواند و عنوان Catalog و نام Party را جداگانه ترکیب می‌کند.
    /// </summary>
    public async Task<AdminOrderDetailPage?> GetOrderAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var group = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        var sellerIds = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerRows = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName })
            .ToListAsync(cancellationToken);
        var sellerNames = sellerRows.ToDictionary(x => x.PartyId, x => x.DisplayName);
        var variantIds = group.SellerOrders.SelectMany(x => x.Lines)
            .Select(x => x.CatalogVariantId).Distinct().ToList();
        var titles = await LoadVariantTitlesAsync(variantIds, cancellationToken);

        var sellerOrders = group.SellerOrders.Select(order =>
        {
            sellerNames.TryGetValue(order.SellerPartyId, out var sellerName);
            var lines = order.Lines.Select(line =>
            {
                titles.TryGetValue(line.CatalogVariantId, out var title);
                return new AdminOrderLineView(
                    line.OfferId,
                    string.IsNullOrWhiteSpace(title) ? "کالای سفارش" : title,
                    line.Quantity,
                    line.UnitPriceSnapshot,
                    line.LineTotalSnapshot + line.TaxAmountSnapshot - line.DiscountAmountSnapshot,
                    line.Currency);
            }).ToList();
            return new AdminSellerOrderView(
                order.SellerOrderId,
                order.OrderNumber,
                order.SellerPartyId,
                sellerName ?? "فروشنده",
                order.Status.ToString(),
                PaymentState(order.Status),
                order.GrandTotalSnapshot,
                order.Currency,
                lines);
        }).ToList();
        var listItem = MapOrderListItem(group);
        var paymentOps = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken);
        AdminPaymentOpsView? paymentView = paymentOps is null
            ? null
            : new AdminPaymentOpsView(
                paymentOps.PaymentId,
                paymentOps.CheckoutId,
                paymentOps.Status.ToString(),
                paymentOps.Amount,
                paymentOps.Currency,
                paymentOps.ProviderCode,
                paymentOps.ProviderRequestReference,
                paymentOps.ProviderTransactionReference,
                paymentOps.CreatedAt,
                paymentOps.UpdatedAt,
                paymentOps.CompletedAt,
                paymentOps.LastFailureCode,
                paymentOps.ReconcileEligible);
        return new AdminOrderDetailPage(
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
            group.RecipientName,
            group.ContactMobile,
            group.ProvinceName,
            group.CityName,
            group.PostalAddress,
            group.PostalCode,
            group.ShippingMethodLabel,
            sellerOrders,
            paymentView);
    }

    /// <summary>
    /// فروشندگان دارای Offer را با وضعیت Party و شمارنده‌های مستقل فهرست می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<AdminSellerListItem>> ListSellersAsync(CancellationToken cancellationToken)
    {
        var offers = await _offers.Offers.AsNoTracking()
            .Select(x => new { x.SellerPartyId, x.Status })
            .ToListAsync(cancellationToken);
        var sellerIds = offers.Select(x => x.SellerPartyId).Distinct().ToList();
        var parties = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName, x.Status })
            .ToListAsync(cancellationToken);
        var orderCounts = await _orders.SellerOrders.AsNoTracking()
            .Where(x => sellerIds.Contains(x.SellerPartyId))
            .GroupBy(x => x.SellerPartyId)
            .Select(x => new { SellerPartyId = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);
        var orderMap = orderCounts.ToDictionary(x => x.SellerPartyId, x => x.Count);
        return parties.Select(party => new AdminSellerListItem(
            party.PartyId,
            party.DisplayName,
            party.Status.ToString(),
            offers.Count(x => x.SellerPartyId == party.PartyId && x.Status == OfferStatus.Active),
            orderMap.GetValueOrDefault(party.PartyId))).ToList();
    }

    /// <summary>
    /// مشتریان را فقط از User ثبت‌کنندهٔ سفارش و آخرین snapshot گیرنده استخراج می‌کند؛ CRM اختراع نمی‌شود.
    /// </summary>
    public async Task<IReadOnlyList<AdminCustomerListItem>> ListCustomersAsync(CancellationToken cancellationToken)
    {
        var rows = await _orders.Checkouts.AsNoTracking()
            .Select(x => new { x.PlacedByUserId, x.RecipientName, x.ContactMobile, x.SubmittedAt })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.PlacedByUserId)
            .Select(group =>
            {
                var latest = group.OrderByDescending(x => x.SubmittedAt).First();
                return new AdminCustomerListItem(
                    group.Key,
                    string.IsNullOrWhiteSpace(latest.RecipientName) ? "مشتری توبا" : latest.RecipientName,
                    string.IsNullOrWhiteSpace(latest.ContactMobile) ? null : latest.ContactMobile,
                    group.Count(),
                    latest.SubmittedAt,
                    "Active");
            })
            .OrderByDescending(x => x.LastOrderAt)
            .ToList();
    }

    private async Task<IReadOnlyList<CheckoutGroup>> LoadOrderGroupsAsync(CancellationToken cancellationToken) =>
        await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

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

    private static AdminOrderListItem MapOrderListItem(CheckoutGroup group)
    {
        var orders = group.SellerOrders;
        var references = orders.Select(x => x.OrderNumber).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var statuses = orders.Select(x => x.Status).Distinct().ToList();
        return new AdminOrderListItem(
            group.CheckoutId,
            references.Count == 0 ? group.CheckoutId.ToString("N")[..12] : string.Join(" / ", references),
            group.SubmittedAt,
            string.IsNullOrWhiteSpace(group.RecipientName) ? "مشتری توبا" : group.RecipientName,
            orders.Count,
            orders.Sum(x => x.Lines.Sum(line => line.Quantity)),
            orders.Sum(x => x.GrandTotalSnapshot),
            orders.Select(x => x.Currency).FirstOrDefault() ?? "IRR",
            orders.Count > 0 && orders.All(x => x.Status == SellerOrderStatus.Paid) ? "Paid" : "PendingPayment",
            statuses.Count == 1 ? statuses[0].ToString() : "Mixed");
    }

    private static string PaymentState(SellerOrderStatus status) =>
        status == SellerOrderStatus.Paid ? "Paid" : status == SellerOrderStatus.Cancelled ? "Cancelled" : "PendingPayment";
}
