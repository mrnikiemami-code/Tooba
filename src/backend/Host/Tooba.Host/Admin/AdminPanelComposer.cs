using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Grid;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;

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
    private readonly ISettlementDirectory _settlement;
    private readonly AdminOrdersGridQueryEngine _ordersGrid;
    private readonly AdminSellersGridQueryEngine _sellersGrid;
    private readonly AdminCustomersGridQueryEngine _customersGrid;
    private readonly AdminPaymentsGridQueryEngine _paymentsGrid;

    /// <summary>
    /// ترکیب‌گر Host را با contextهای مستقل ماژول‌ها می‌سازد.
    /// </summary>
    public AdminPanelComposer(
        CatalogDbContext catalog,
        OfferDbContext offers,
        OrderDbContext orders,
        PartyDbContext parties,
        PaymentDbContext paymentDb,
        IPaymentAdminDirectory payments,
        ISettlementDirectory settlement)
    {
        _catalog = catalog;
        _offers = offers;
        _orders = orders;
        _parties = parties;
        _payments = payments;
        _settlement = settlement;
        _ordersGrid = new AdminOrdersGridQueryEngine(orders, parties);
        _sellersGrid = new AdminSellersGridQueryEngine(offers, parties, orders);
        _customersGrid = new AdminCustomersGridQueryEngine(orders);
        _paymentsGrid = new AdminPaymentsGridQueryEngine(paymentDb, orders);
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
        var sellerIds = groups.SelectMany(g => g.SellerOrders.Select(o => o.SellerPartyId)).Distinct().ToList();
        var sellerNames = await LoadSellerDisplayNamesAsync(sellerIds, cancellationToken);
        return groups.Select(group => MapOrderListItem(group, sellerNames)).ToList();
    }

    /// <summary>صفحه‌بندی server-side گرید سفارش‌های Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminOrderListItem>> QueryOrdersGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Orders.Normalize(request);
        return _ordersGrid.QueryAsync(q, cancellationToken);
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
        var listItem = MapOrderListItem(group, sellerNames);
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

        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var settlementByOrder = await _settlement.ListEntriesBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        var lineCount = group.SellerOrders.Sum(x => x.Lines.Sum(line => line.Quantity));
        var sellerCount = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().Count();
        var sellerFinancials = BuildSellerFinancials(group, sellerNames, settlementByOrder);
        var financialEvents = BuildFinancialEvents(group, sellerNames, paymentView, settlementByOrder);
        var financialSummary = BuildFinancialSummary(group, sellerFinancials, paymentView);

        return new AdminOrderDetailPage(
            group.CheckoutId,
            listItem.Reference,
            group.SubmittedAt,
            listItem.Status,
            listItem.PaymentState,
            lineCount,
            sellerCount,
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
            sellerFinancials,
            financialEvents,
            financialSummary,
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

    /// <summary>صفحه‌بندی server-side گرید فروشندگان Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminSellerListItem>> QuerySellersGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Sellers.Normalize(request);
        return _sellersGrid.QueryAsync(q, cancellationToken);
    }

    /// <summary>صفحه‌بندی server-side گرید مشتریان Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminCustomerListItem>> QueryCustomersGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Customers.Normalize(request);
        return _customersGrid.QueryAsync(q, cancellationToken);
    }

    /// <summary>صفحه‌بندی server-side گرید دریافت‌های Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminReceiptListItem>> QueryPaymentsGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Payments.Normalize(request);
        return _paymentsGrid.QueryAsync(q, cancellationToken);
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

    private async Task<IReadOnlyDictionary<Guid, string>> LoadSellerDisplayNamesAsync(
        IReadOnlyCollection<Guid> sellerIds,
        CancellationToken cancellationToken)
    {
        if (sellerIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var sellerRows = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName })
            .ToListAsync(cancellationToken);
        return sellerRows.ToDictionary(x => x.PartyId, x => x.DisplayName);
    }

    private static AdminOrderListItem MapOrderListItem(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames)
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
            FormatSellerDisplayNames(orders, sellerNames),
            orders.Sum(x => x.Lines.Sum(line => line.Quantity)),
            orders.Sum(x => x.GrandTotalSnapshot),
            orders.Select(x => x.Currency).FirstOrDefault() ?? "IRR",
            orders.Count > 0 && orders.All(x => x.Status == SellerOrderStatus.Paid) ? "Paid" : "PendingPayment",
            statuses.Count == 1 ? statuses[0].ToString() : "Mixed");
    }

    private static string FormatSellerDisplayNames(
        IEnumerable<SellerOrder> orders,
        IReadOnlyDictionary<Guid, string> sellerNames)
    {
        var sellerIds = orders.Select(o => o.SellerPartyId).Distinct().ToList();
        if (sellerIds.Count == 0)
        {
            return "—";
        }

        if (sellerIds.Count == 1)
        {
            return sellerNames.TryGetValue(sellerIds[0], out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "—";
        }

        return $"{sellerIds.Count} فروشنده";
    }

    private static string PaymentState(SellerOrderStatus status) =>
        status == SellerOrderStatus.Paid ? "Paid" : status == SellerOrderStatus.Cancelled ? "Cancelled" : "PendingPayment";

    private static IReadOnlyList<AdminSellerFinancialView> BuildSellerFinancials(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        IReadOnlyDictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>> settlementByOrder)
    {
        return group.SellerOrders.Select(order =>
        {
            sellerNames.TryGetValue(order.SellerPartyId, out var sellerName);
            settlementByOrder.TryGetValue(order.SellerOrderId, out var entries);
            var credit = entries?.FirstOrDefault(x => x.EntryType == EntryType.Credit);
            decimal gross;
            decimal commission;
            decimal payable;
            string settlementStatus;
            if (credit is not null)
            {
                gross = credit.GrossAmount;
                commission = credit.CommissionAmount;
                payable = credit.NetAmount;
                settlementStatus = "Settled";
            }
            else
            {
                gross = order.SubtotalSnapshot;
                commission = 0m;
                payable = order.GrandTotalSnapshot;
                settlementStatus = order.Status == SellerOrderStatus.Paid
                    ? "WaitingForSettlement"
                    : "NotSettled";
            }

            return new AdminSellerFinancialView(
                order.SellerOrderId,
                order.SellerPartyId,
                sellerName ?? "فروشنده",
                order.Lines.Sum(line => line.Quantity),
                gross,
                commission,
                payable,
                order.Currency,
                settlementStatus);
        }).ToList();
    }

    private static IReadOnlyList<AdminFinancialEventView> BuildFinancialEvents(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        AdminPaymentOpsView? payment,
        IReadOnlyDictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>> settlementByOrder)
    {
        var events = new List<AdminFinancialEventView>();
        if (payment is not null)
        {
            events.Add(new AdminFinancialEventView(
                payment.CompletedAt ?? payment.CreatedAt,
                "CustomerReceipt",
                payment.Amount,
                payment.Currency,
                string.IsNullOrWhiteSpace(group.RecipientName) ? "مشتری توبا" : group.RecipientName,
                payment.ProviderTransactionReference ?? payment.ProviderRequestReference ?? payment.PaymentId.ToString("N")[..12],
                payment.ProviderCode,
                payment.Status,
                "دریافت از مشتری"));
        }

        foreach (var order in group.SellerOrders)
        {
            if (!settlementByOrder.TryGetValue(order.SellerOrderId, out var entries))
            {
                continue;
            }

            sellerNames.TryGetValue(order.SellerPartyId, out var sellerName);
            foreach (var entry in entries)
            {
                events.Add(new AdminFinancialEventView(
                    entry.PostedAt,
                    entry.EntryType == EntryType.Credit ? "SellerSettlement" : "SettlementAdjustment",
                    entry.NetAmount,
                    entry.Currency,
                    sellerName ?? "فروشنده",
                    entry.EntryId.ToString("N")[..12],
                    entry.SourceType,
                    "Succeeded",
                    entry.EntryType == EntryType.Credit
                        ? $"تسویه سهم سفارش {order.OrderNumber}"
                        : $"تعدیل تسویه سفارش {order.OrderNumber}"));
            }
        }

        return events.OrderByDescending(x => x.OccurredAt).ToList();
    }

    private static AdminFinancialSummaryView BuildFinancialSummary(
        CheckoutGroup group,
        IReadOnlyList<AdminSellerFinancialView> sellerFinancials,
        AdminPaymentOpsView? payment)
    {
        var currency = group.SellerOrders.Select(x => x.Currency).FirstOrDefault() ?? "IRR";
        var totalSellerShare = sellerFinancials.Sum(x => x.GrossAmount);
        var totalCommission = sellerFinancials.Sum(x => x.CommissionAmount);
        var payableToSellers = sellerFinancials.Sum(x => x.PayableAmount);
        var customerGross = group.SellerOrders.Sum(x => x.SubtotalSnapshot);
        var shippingCost = 0m;
        var customerDiscounts = group.SellerOrders.Sum(x => x.DiscountSnapshot);
        var totalReceived = payment?.Amount ?? group.SellerOrders.Sum(x => x.GrandTotalSnapshot);
        return new AdminFinancialSummaryView(
            totalSellerShare,
            totalCommission,
            totalCommission,
            payableToSellers,
            customerGross,
            shippingCost,
            customerDiscounts,
            totalReceived,
            currency);
    }
}
