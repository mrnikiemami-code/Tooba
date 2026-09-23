using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Detail.Models;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Order.Application.Admin.Supply.Services;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Domain;
using Tooba.Party.Contracts;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.Operations;
using Tooba.Settlement.Contracts.Operations;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Admin.Detail;

/// <summary>
/// ترکیب جزئیات سفارش مدیر از Order + Contracts بیگانه؛ بدون DbContext بیگانه.
/// </summary>
public sealed class AdminOrderDetailComposer
{
    private readonly IPartyLookup _parties;
    private readonly ICatalogVariantLookup _catalog;
    private readonly IFulfillmentAdminOperations _fulfillment;
    private readonly IPaymentAdminGateway _payments;
    private readonly ISettlementAdminOrderDetailReader _settlement;
    private readonly IReturnAdminOperations _returns;
    private readonly OrderSupplyService _supply;
    private readonly IReservationCycleDirectory _cycles;
    private readonly IClock _clock;

    public AdminOrderDetailComposer(
        IPartyLookup parties,
        ICatalogVariantLookup catalog,
        IFulfillmentAdminOperations fulfillment,
        IPaymentAdminGateway payments,
        ISettlementAdminOrderDetailReader settlement,
        IReturnAdminOperations returns,
        OrderSupplyService supply,
        IReservationCycleDirectory cycles,
        IClock clock)
    {
        _parties = parties;
        _catalog = catalog;
        _fulfillment = fulfillment;
        _payments = payments;
        _settlement = settlement;
        _returns = returns;
        _supply = supply;
        _cycles = cycles;
        _clock = clock;
    }

    /// <summary>صفحهٔ جزئیات را برای Checkout بارگذاری‌شده می‌سازد.</summary>
    public async Task<Result<AdminOrderDetailPage>> ComposeAsync(
        CheckoutGroup group,
        CancellationToken cancellationToken)
    {
        var checkoutId = group.CheckoutId;
        var sellerIds = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerNames = await _parties.GetDisplayNamesAsync(sellerIds, cancellationToken);
        var variantIds = group.SellerOrders.SelectMany(x => x.Lines)
            .Select(x => x.CatalogVariantId).Distinct().ToList();
        var titles = await _catalog.GetVariantTitlesAsync(variantIds, cancellationToken);

        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var packages = await _fulfillment.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
        var allShipmentIds = fulfillments.SelectMany(f => f.Shipments.Select(s => s.ShipmentId)).Distinct().ToArray();
        var memberships = await _fulfillment.GetActiveMembershipByShipmentIdsAsync(allShipmentIds, cancellationToken);
        var membershipByShipment = memberships
            .Where(m => m.PackageStatus is ConsolidatedPackageOperationStatus.Created
                or ConsolidatedPackageOperationStatus.Dispatched)
            .ToDictionary(m => m.ShipmentId);
        var memberOfAnyPackage = memberships.ToDictionary(m => m.ShipmentId);
        var multiSeller = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().Count() >= 2;
        var fulfillmentBySeller = fulfillments
            .GroupBy(x => x.SellerOrderId)
            .ToDictionary(g => g.Key, g => g.First());

        var now = _clock.UtcNow;
        var sellerOrders = group.SellerOrders.Select(order =>
        {
            sellerNames.TryGetValue(order.SellerPartyId, out var sellerName);
            fulfillmentBySeller.TryGetValue(order.SellerOrderId, out var fulfillment);
            var shippedByLine = fulfillment?.Items.ToDictionary(x => x.OrderLineId, x => x.QuantityShipped)
                ?? new Dictionary<Guid, decimal>();
            var packedByLine = fulfillment?.Items.ToDictionary(x => x.OrderLineId, x => x.QuantityPacked)
                ?? new Dictionary<Guid, decimal>();
            var processingByLine = fulfillment?.Items.ToDictionary(x => x.OrderLineId, x => x.QuantityProcessing)
                ?? new Dictionary<Guid, decimal>();

            var lines = order.Lines.Select(line =>
            {
                titles.TryGetValue(line.CatalogVariantId, out var title);
                shippedByLine.TryGetValue(line.LineId, out var shipped);
                packedByLine.TryGetValue(line.LineId, out var packed);
                processingByLine.TryGetValue(line.LineId, out var processing);
                var openAllocated = fulfillment?.Shipments
                    .Where(s => s.Status == ShipmentOperationStatus.Created)
                    .SelectMany(s => s.Items)
                    .Where(i => i.OrderLineId == line.LineId)
                    .Sum(i => i.Quantity) ?? 0;
                var deliverySlices = Array.Empty<(decimal Quantity, DateTimeOffset DeliveredAt)>();
                if (fulfillment is not null)
                {
                    deliverySlices = fulfillment.Shipments
                        .Where(s => s.DeliveredAt is not null)
                        .SelectMany(s => s.Items
                            .Where(i => i.OrderLineId == line.LineId && i.Quantity > 0)
                            .Select(i => (i.Quantity, DeliveredAt: s.DeliveredAt!.Value)))
                        .ToArray();
                }

                var returnUi = BuildReturnDeadlineUi(line, deliverySlices, now);
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
                    AdminOrderDetailFulfillmentStatus.LineOperationalStatus(
                        order.Status, fulfillment, packed, line.Quantity, processing, shipped),
                    fulfillment is null ? null : packed,
                    fulfillment is null ? null : openAllocated + shipped,
                    line.IsReturnableSnapshot,
                    line.ReturnWindowDaysSnapshot,
                    line.ReturnPolicyLabelSnapshot,
                    returnUi.DeadlineDisplay,
                    returnUi.RemainingDisplay,
                    returnUi.StatusCode,
                    fulfillment is null ? null : processing);
            }).ToList();
            var shipments = fulfillment?.Shipments.Select(s =>
            {
                membershipByShipment.TryGetValue(s.ShipmentId, out var membership);
                var canAdd = multiSeller
                    && s.Status == ShipmentOperationStatus.Created
                    && s.DispatchedAt is null
                    && !string.IsNullOrWhiteSpace(s.ShippingMethodCode)
                    && !memberOfAnyPackage.ContainsKey(s.ShipmentId);
                return new AdminShipmentView(
                    s.ShipmentId,
                    s.Status.ToString(),
                    s.CarrierDisplayName,
                    s.TrackingReference,
                    s.Items.Sum(i => i.Quantity),
                    s.Items.Select(i => new AdminShipmentLineView(i.OrderLineId, i.Quantity)).ToList(),
                    string.IsNullOrWhiteSpace(s.ShippingMethodCode) ? null : s.ShippingMethodCode,
                    string.IsNullOrWhiteSpace(s.ShippingMethodLabel) ? null : s.ShippingMethodLabel,
                    membership?.ConsolidatedPackageId,
                    membership?.PackageNumber,
                    membership is null
                        ? null
                        : $"این مرسوله عضو بسته تجمیعی {membership.PackageNumber} است و عملیات ارسال از طریق بسته تجمیعی انجام می‌شود.",
                    canAdd);
            }).ToList()
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
                fulfillment is null ? null : AdminOrderDetailFulfillmentStatus.ComposeOperationalStatus(fulfillment),
                shipments);
        }).ToList();

        var listItem = await MapOrderListItemAsync(group, sellerNames, cancellationToken);
        var paymentOps = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken);
        var supplyStatusResult = await _supply.GetStatusAsync(checkoutId, cancellationToken);
        if (supplyStatusResult.IsFailure)
        {
            return Result.Failure<AdminOrderDetailPage>(
                new SemanticError(supplyStatusResult.FirstError.Code));
        }

        var supplyStatus = supplyStatusResult.Value;
        var cycleProjection = await _cycles.GetProjectionAsync(
            checkoutId,
            now,
            supplyStatus.Status.ToString(),
            cancellationToken);
        var cycleEvents = await _cycles.ListEventsAsync(checkoutId, cancellationToken);
        var reservationAudit = AdminOrderReservationCycleMapper.ToAudit(cycleProjection, cycleEvents, supplyStatus);
        var reservationSummary = AdminOrderReservationCycleMapper.ToSummary(cycleProjection);
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
                paymentOps.RejectDepositEligible,
                paymentOps.CustomerTransferReference,
                paymentOps.ProofMediaAssetId,
                paymentOps.EvidenceSubmittedAt,
                reservationSummary.CompactLabelFa,
                reservationSummary.CompactLabelEn,
                reservationSummary.State,
                reservationSummary.CycleNumber,
                reservationSummary.RetryPossible,
                reservationSummary.NeedsReacquire,
                reservationSummary.RetryLimitReached);

        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var settlementByOrder = await _settlement.ListEntriesBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        var lineCount = InvoiceHeaderSemantics.LineCount(group.SellerOrders);
        var sellerCount = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().Count();
        var sellerFinancials = AdminOrderDetailFinancials.BuildSellerFinancials(group, sellerNames, settlementByOrder);
        var financialEvents = await BuildFinancialEventsAsync(
            group, sellerNames, paymentView, settlementByOrder, cancellationToken);
        var financialSummary = AdminOrderDetailFinancials.BuildFinancialSummary(group, sellerFinancials, paymentView);
        var packageViews = packages.Select(p => new AdminConsolidatedPackageView(
            p.ConsolidatedPackageId,
            p.PackageNumber,
            p.Status.ToString(),
            p.ShippingMethodCode,
            p.ShippingMethodLabel,
            p.TrackingReference,
            p.Note,
            p.Members.Select(m => m.SellerPartyId).Distinct().Count(),
            p.Members.Count,
            p.CreatedAt,
            p.DispatchedAt,
            p.DeliveredAt,
            p.CancelledAt,
            p.Members.Select(m => new AdminConsolidatedPackageMemberView(
                m.ShipmentId,
                m.SellerPartyId,
                m.FulfillmentId,
                m.JoinedAt,
                m.ReleasedAt)).ToList())).ToList();

        return Result.Success(new AdminOrderDetailPage(
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
            StorefrontRecipientNames.Display(group.RecipientFirstName, group.RecipientLastName, group.RecipientName),
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
            paymentView,
            packageViews,
            reservationAudit));
    }

    private async Task<OrdersGrid.Models.AdminOrderListItem> MapOrderListItemAsync(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        CancellationToken cancellationToken)
    {
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        var returnsLookup = returns
            .GroupBy(x => x.SellerOrderId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ReturnSnapshot>)g.ToList());
        return AdminOrdersGridProjection.MapOrderListItem(group, sellerNames, returnsLookup);
    }

    private async Task<IReadOnlyList<AdminFinancialEventView>> BuildFinancialEventsAsync(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        AdminPaymentOpsView? payment,
        IReadOnlyDictionary<Guid, IReadOnlyList<SettlementAdminOrderEntrySnapshot>> settlementByOrder,
        CancellationToken cancellationToken)
    {
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = sellerOrderIds.Count == 0
            ? Array.Empty<ReturnSnapshot>()
            : await _returns.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        var succeeded = returns
            .SelectMany(ret => ret.RefundAttempts
                .Where(a => a.Status == RefundAttemptOperationStatus.Succeeded)
                .Select(a => new AdminOrderDetailFinancials.OrderFinancialRefundInput(
                    ret.ReturnRequestId,
                    a.RefundAttemptId,
                    a.Amount,
                    a.Currency,
                    a.CompletedAt ?? a.CreatedAt,
                    ret.RefundAmount,
                    string.IsNullOrWhiteSpace(a.ProviderReference) ? null : a.ProviderReference)))
            .ToList();
        return AdminOrderDetailFinancials.BuildFinancialEvents(group, sellerNames, payment, settlementByOrder, succeeded);
    }

    private static string PaymentState(SellerOrderStatus status) =>
        status == SellerOrderStatus.Paid ? "Paid" : status == SellerOrderStatus.Cancelled ? "Cancelled" : "PendingPayment";

    private static (string DeadlineDisplay, string RemainingDisplay, string StatusCode) BuildReturnDeadlineUi(
        OrderLine line,
        IReadOnlyList<(decimal Quantity, DateTimeOffset DeliveredAt)> deliverySlices,
        DateTimeOffset now)
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
            return (policyLabel, "", "before_delivery");
        }

        var undeliveredQty = Math.Max(0m, line.Quantity - deliveredQty);
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

    private static string ToPersianDigits(decimal value) =>
        BuildingBlocks.QuantityDisplay.Format(value, 6)
            .Replace('0', '۰').Replace('1', '۱').Replace('2', '۲').Replace('3', '۳').Replace('4', '۴')
            .Replace('5', '۵').Replace('6', '۶').Replace('7', '۷').Replace('8', '۸').Replace('9', '۹');
}
