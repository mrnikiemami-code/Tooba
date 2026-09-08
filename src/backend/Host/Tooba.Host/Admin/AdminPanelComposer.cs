using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Grid;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;
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
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly ReturnsDbContext _returns;
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
        ISettlementDirectory settlement,
        IFulfillmentDirectory fulfillment,
        ReturnsDbContext returns)
    {
        _catalog = catalog;
        _offers = offers;
        _orders = orders;
        _parties = parties;
        _payments = payments;
        _settlement = settlement;
        _fulfillment = fulfillment;
        _returns = returns;
        _ordersGrid = new AdminOrdersGridQueryEngine(orders, parties, returns);
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
        var items = new List<AdminOrderListItem>(groups.Count);
        foreach (var group in groups)
        {
            items.Add(await MapOrderListItemAsync(group, sellerNames, cancellationToken));
        }

        return items;
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
    public async Task<AdminOrderDetailPage?> GetOrderAsync(
        Guid checkoutId,
        Guid viewerUserId,
        CancellationToken cancellationToken)
    {
        var group = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        if (viewerUserId != Guid.Empty)
        {
            _orders.AdminViewAcks.Add(CheckoutAdminViewAck.Create(checkoutId, viewerUserId, DateTimeOffset.UtcNow));
            await _orders.SaveChangesAsync(cancellationToken);
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

        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var fulfillmentBySeller = fulfillments
            .GroupBy(x => x.SellerOrderId)
            .ToDictionary(g => g.Key, g => g.First());

        var sellerOrders = group.SellerOrders.Select(order =>
        {
            sellerNames.TryGetValue(order.SellerPartyId, out var sellerName);
            fulfillmentBySeller.TryGetValue(order.SellerOrderId, out var fulfillment);
            var shippedByLine = fulfillment?.Items.ToDictionary(x => x.OrderLineId, x => x.QuantityShipped)
                ?? new Dictionary<Guid, int>();
            var packedByLine = fulfillment?.Items.ToDictionary(x => x.OrderLineId, x => x.QuantityPacked)
                ?? new Dictionary<Guid, int>();

            var lines = order.Lines.Select(line =>
            {
                titles.TryGetValue(line.CatalogVariantId, out var title);
                shippedByLine.TryGetValue(line.LineId, out var shipped);
                packedByLine.TryGetValue(line.LineId, out var packed);
                var openAllocated = fulfillment?.Shipments
                    .Where(s => s.Status == Tooba.Fulfillment.Domain.ShipmentStatus.Created)
                    .SelectMany(s => s.Items)
                    .Where(i => i.OrderLineId == line.LineId)
                    .Sum(i => i.Quantity) ?? 0;
                var deliverySlices = Array.Empty<(int Quantity, DateTimeOffset DeliveredAt)>();
                if (fulfillment is not null)
                {
                    deliverySlices = fulfillment.Shipments
                        .Where(s => s.DeliveredAt is not null)
                        .SelectMany(s => s.Items
                            .Where(i => i.OrderLineId == line.LineId && i.Quantity > 0)
                            .Select(i => (i.Quantity, DeliveredAt: s.DeliveredAt!.Value)))
                        .ToArray();
                }

                var returnUi = BuildReturnDeadlineUi(line, deliverySlices);
                return new AdminOrderLineView(
                    line.OfferId,
                    string.IsNullOrWhiteSpace(title) ? "کالای سفارش" : title,
                    line.Quantity,
                    line.UnitPriceSnapshot,
                    line.LineTotalSnapshot + line.TaxAmountSnapshot - line.DiscountAmountSnapshot,
                    line.Currency,
                    line.LineId,
                    fulfillment is null ? null : shipped,
                    null,
                    LineOperationalStatus(fulfillment, packed, line.Quantity),
                    fulfillment is null ? null : packed,
                    fulfillment is null ? null : openAllocated + shipped,
                    line.IsReturnableSnapshot,
                    line.ReturnWindowDaysSnapshot,
                    line.ReturnPolicyLabelSnapshot,
                    returnUi.DeadlineDisplay,
                    returnUi.RemainingDisplay,
                    returnUi.StatusCode);
            }).ToList();
            var shipments = fulfillment?.Shipments.Select(s => new AdminShipmentView(
                s.ShipmentId,
                s.Status.ToString(),
                s.CarrierDisplayName,
                s.TrackingReference,
                s.Items.Sum(i => i.Quantity),
                s.Items.Select(i => new AdminShipmentLineView(i.OrderLineId, i.Quantity)).ToList(),
                string.IsNullOrWhiteSpace(s.ShippingMethodCode) ? null : s.ShippingMethodCode,
                string.IsNullOrWhiteSpace(s.ShippingMethodLabel) ? null : s.ShippingMethodLabel)).ToList()
                ?? (IReadOnlyList<AdminShipmentView>)Array.Empty<AdminShipmentView>();
            return new AdminSellerOrderView(
                order.SellerOrderId,
                order.OrderNumber,
                order.SellerPartyId,
                sellerName ?? "فروشنده",
                order.Status.ToString(),
                PaymentState(order.Status),
                order.GrandTotalSnapshot,
                order.Currency,
                lines,
                fulfillment?.FulfillmentId,
                fulfillment?.Status.ToString(),
                shipments);
        }).ToList();
        var listItem = await MapOrderListItemAsync(group, sellerNames, cancellationToken);
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
                paymentOps.ReconcileEligible,
                paymentOps.ConfirmDepositEligible,
                paymentOps.RejectDepositEligible);

        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var settlementByOrder = await _settlement.ListEntriesBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        var lineCount = group.SellerOrders.Sum(x => x.Lines.Sum(line => line.Quantity));
        var sellerCount = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().Count();
        var sellerFinancials = BuildSellerFinancials(group, sellerNames, settlementByOrder);
        var financialEvents = await BuildFinancialEventsAsync(
            group, sellerNames, paymentView, settlementByOrder, cancellationToken);
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

    private async Task<AdminOrderListItem> MapOrderListItemAsync(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        CancellationToken cancellationToken)
    {
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .ToListAsync(cancellationToken);
        var returnsLookup = returns
            .GroupBy(x => x.SellerOrderId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ReturnRequest>)g.ToList());
        return AdminOrdersGridQueryEngine.MapOrderListItem(group, sellerNames, returnsLookup);
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

    private static (string DeadlineDisplay, string RemainingDisplay, string StatusCode) BuildReturnDeadlineUi(
        OrderLine line,
        IReadOnlyList<(int Quantity, DateTimeOffset DeliveredAt)> deliverySlices)
    {
        if (!line.IsReturnableSnapshot)
        {
            return ("غیرقابل مرجوعی", "غیرقابل مرجوعی", "non_returnable");
        }

        var windowDays = line.ReturnWindowDaysSnapshot < 0 ? 0 : line.ReturnWindowDaysSnapshot;
        var policyLabel = string.IsNullOrWhiteSpace(line.ReturnPolicyLabelSnapshot)
            ? $"{ToPersianDigits(windowDays)} روز پس از تحویل"
            : line.ReturnPolicyLabelSnapshot!;

        var deliveredQty = deliverySlices.Sum(x => x.Quantity);
        if (deliveredQty <= 0)
        {
            return (policyLabel, policyLabel, "before_delivery");
        }

        var undeliveredQty = Math.Max(0, line.Quantity - deliveredQty);
        var now = DateTimeOffset.UtcNow;
        var sliceParts = new List<string>();
        var remainingParts = new List<string>();
        var anyEligible = false;
        var allExpired = true;
        foreach (var slice in deliverySlices.OrderBy(x => x.DeliveredAt))
        {
            var deadline = slice.DeliveredAt.AddDays(windowDays);
            var remainingDays = (int)Math.Ceiling((deadline - now).TotalDays);
            if (remainingDays < 0)
            {
                sliceParts.Add($"تحویل‌شده {ToPersianDigits(slice.Quantity)}: مهلت تمام شده");
                remainingParts.Add($"تحویل‌شده {ToPersianDigits(slice.Quantity)}: منقضی");
            }
            else
            {
                anyEligible = true;
                allExpired = false;
                var deadlineFa = FormatPersianDate(deadline);
                sliceParts.Add($"تحویل‌شده {ToPersianDigits(slice.Quantity)}: تا {deadlineFa}");
                remainingParts.Add($"تحویل‌شده {ToPersianDigits(slice.Quantity)}: {ToPersianDigits(remainingDays)} روز باقی‌مانده");
            }
        }

        if (undeliveredQty > 0)
        {
            sliceParts.Add($"تحویل‌نشده {ToPersianDigits(undeliveredQty)}: ساعت مرجوعی شروع نشده");
            remainingParts.Add($"تحویل‌نشده {ToPersianDigits(undeliveredQty)}: قبل از تحویل");
            allExpired = false;
        }

        var deadlineDisplay = string.Join(" · ", sliceParts);
        var remainingDisplay = string.Join(" · ", remainingParts);
        if (undeliveredQty > 0)
        {
            return (deadlineDisplay, remainingDisplay, anyEligible ? "partial_eligible" : "before_delivery");
        }

        if (!anyEligible && allExpired)
        {
            return ("مهلت مرجوعی تمام شده", "مهلت مرجوعی تمام شده", "expired");
        }

        return (deadlineDisplay, remainingDisplay, "eligible");
    }

    private static string FormatPersianDate(DateTimeOffset value)
    {
        try
        {
            var calendar = new System.Globalization.PersianCalendar();
            var y = calendar.GetYear(value.UtcDateTime);
            var m = calendar.GetMonth(value.UtcDateTime);
            var d = calendar.GetDayOfMonth(value.UtcDateTime);
            return $"{ToPersianDigits(y)}/{ToPersianDigits(m).PadLeft(2, '۰')}/{ToPersianDigits(d).PadLeft(2, '۰')}";
        }
        catch
        {
            return value.UtcDateTime.ToString("yyyy/MM/dd");
        }
    }

    private static string ToPersianDigits(int value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            .Replace('0', '۰').Replace('1', '۱').Replace('2', '۲').Replace('3', '۳').Replace('4', '۴')
            .Replace('5', '۵').Replace('6', '۶').Replace('7', '۷').Replace('8', '۸').Replace('9', '۹');

    private static string HumanizeProviderCode(string? providerCode) =>
        providerCode?.Trim().ToLowerInvariant() switch
        {
            "wallet" => "کیف پول",
            "fake" => "درگاه آزمایشی",
            "manual" => "کارت به کارت",
            "webhook" => "درگاه وب‌هوک",
            "fail-closed" => "درگاه غیرفعال",
            null or "" => "—",
            _ => providerCode!
        };

    private static bool IsSuccessfulPaymentStatus(string status) =>
        string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Captured", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase);

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

    internal static string? LineOperationalStatus(FulfillmentSnapshot? fulfillment, int packed, int ordered)
    {
        if (fulfillment is null)
        {
            return null;
        }

        if (fulfillment.Status is FulfillmentStatus.Dispatched
            or FulfillmentStatus.InTransit
            or FulfillmentStatus.Delivered
            or FulfillmentStatus.Cancelled
            or FulfillmentStatus.Failed)
        {
            return fulfillment.Status.ToString();
        }

        if (ordered > 0 && packed >= ordered)
        {
            return "Packed";
        }

        if (fulfillment.Status == FulfillmentStatus.ReadyToFulfill)
        {
            return "ReadyToFulfill";
        }

        return "Processing";
    }

    /// <summary>
    /// حرکات مالی واقعی قابل‌انتساب به همین سفارش (بدون مبلغ کل batch payout).
    /// </summary>
    internal static IReadOnlyList<AdminFinancialEventView> BuildFinancialEvents(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        AdminPaymentOpsView? payment,
        IReadOnlyDictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>> settlementByOrder,
        IReadOnlyList<OrderFinancialRefundInput> succeededRefunds)
    {
        var events = new List<AdminFinancialEventView>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void AddOnce(string key, AdminFinancialEventView row)
        {
            if (!seen.Add(key))
            {
                return;
            }

            events.Add(row);
        }

        if (payment is not null && IsSuccessfulPaymentStatus(payment.Status))
        {
            AddOnce(
                $"receipt:{payment.PaymentId:N}",
                new AdminFinancialEventView(
                    payment.CompletedAt ?? payment.CreatedAt,
                    "CustomerReceipt",
                    payment.Amount,
                    payment.Currency,
                    string.IsNullOrWhiteSpace(group.RecipientName) ? "مشتری توبا" : group.RecipientName,
                    payment.ProviderTransactionReference
                        ?? payment.ProviderRequestReference
                        ?? payment.PaymentId.ToString("N")[..12],
                    HumanizeProviderCode(payment.ProviderCode),
                    "Succeeded",
                    "دریافت از مشتری"));
        }

        // یک حرکت بازگشت وجه موفق به ازای هر ReturnRequest (retries/idempotency تکراری نمی‌شوند).
        foreach (var refund in succeededRefunds
                     .GroupBy(x => x.ReturnRequestId)
                     .Select(g => g.OrderByDescending(x => x.OccurredAt).First()))
        {
            var expected = refund.ExpectedRefundAmount > 0 ? refund.ExpectedRefundAmount : refund.Amount;
            var description = refund.Amount < expected
                ? "بازگشت وجه جزئی به مشتری"
                : "بازگشت وجه به مشتری";
            AddOnce(
                $"refund:{refund.ReturnRequestId:N}",
                new AdminFinancialEventView(
                    refund.OccurredAt,
                    "CustomerRefund",
                    refund.Amount,
                    refund.Currency,
                    string.IsNullOrWhiteSpace(group.RecipientName) ? "مشتری توبا" : group.RecipientName,
                    string.IsNullOrWhiteSpace(refund.Reference)
                        ? refund.RefundAttemptId.ToString("N")[..12]
                        : refund.Reference!,
                    "بازگشت وجه",
                    "Succeeded",
                    description));
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
                // فقط مبلغ خالص همین SellerOrder — نه مبلغ کل درخواست payout چندسفارشی.
                var isRefundAdj = string.Equals(entry.SourceType, "refund", StringComparison.OrdinalIgnoreCase)
                                  || entry.EntryType == EntryType.Debit;
                AddOnce(
                    $"settlement:{entry.EntryId:N}",
                    new AdminFinancialEventView(
                        entry.PostedAt,
                        isRefundAdj ? "SellerRefundAdjustment" : "SellerPayout",
                        entry.NetAmount,
                        entry.Currency,
                        sellerName ?? "فروشنده",
                        entry.EntryId.ToString("N")[..12],
                        isRefundAdj ? "تعدیل مرجوعی" : "تسویه سفارش",
                        "Succeeded",
                        isRefundAdj
                            ? "کسر از حساب فروشنده بابت بازگشت وجه"
                            : "واریز سهم فروشنده"));
            }
        }

        return events
            .OrderByDescending(x => x.OccurredAt)
            .ThenBy(x => x.EventType, StringComparer.Ordinal)
            .ThenBy(x => x.Reference, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>ورودی محدود برای projection بازگشت وجه سفارش (بدون افشای payload درگاه).</summary>
    internal sealed record OrderFinancialRefundInput(
        Guid ReturnRequestId,
        Guid RefundAttemptId,
        decimal Amount,
        string Currency,
        DateTimeOffset OccurredAt,
        decimal ExpectedRefundAmount,
        string? Reference = null);

    private async Task<IReadOnlyList<AdminFinancialEventView>> BuildFinancialEventsAsync(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        AdminPaymentOpsView? payment,
        IReadOnlyDictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>> settlementByOrder,
        CancellationToken cancellationToken)
    {
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = sellerOrderIds.Count == 0
            ? []
            : await _returns.ReturnRequests.AsNoTracking()
                .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
                .ToListAsync(cancellationToken);
        var returnIds = returns.Select(x => x.ReturnRequestId).ToList();
        var refundAttempts = returnIds.Count == 0
            ? []
            : await _returns.RefundAttempts.AsNoTracking()
                .Where(x => returnIds.Contains(x.ReturnRequestId)
                            && x.Status == RefundAttemptStatus.Succeeded)
                .ToListAsync(cancellationToken);
        var returnById = returns.ToDictionary(x => x.ReturnRequestId);
        var succeeded = refundAttempts
            .Where(a => returnById.ContainsKey(a.ReturnRequestId))
            .Select(a =>
            {
                var ret = returnById[a.ReturnRequestId];
                return new OrderFinancialRefundInput(
                    ret.ReturnRequestId,
                    a.RefundAttemptId,
                    a.Amount,
                    a.Currency,
                    a.CompletedAt ?? a.CreatedAt,
                    ret.RefundAmount,
                    string.IsNullOrWhiteSpace(a.ProviderReference) ? null : a.ProviderReference);
            })
            .ToList();
        return BuildFinancialEvents(group, sellerNames, payment, settlementByOrder, succeeded);
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
