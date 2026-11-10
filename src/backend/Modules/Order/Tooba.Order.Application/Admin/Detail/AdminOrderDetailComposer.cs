using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Detail.Models;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Order.Application.Admin.Supply.Services;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain;
using Tooba.Party.Contracts.Ports;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.Operations;
using Tooba.Settlement.Contracts.Operations;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
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
    private readonly IOrderShippingGroupHistoryReader _shippingGroupHistory;
    private readonly IOrderCommittedCarrierShipmentFactsReader _committedShippingGroups;
    private readonly IOrderAdminEffectiveAccessReader _access;

    public AdminOrderDetailComposer(
        IPartyLookup parties,
        ICatalogVariantLookup catalog,
        IFulfillmentAdminOperations fulfillment,
        IPaymentAdminGateway payments,
        ISettlementAdminOrderDetailReader settlement,
        IReturnAdminOperations returns,
        OrderSupplyService supply,
        IReservationCycleDirectory cycles,
        IClock clock,
        IOrderShippingGroupHistoryReader shippingGroupHistory,
        IOrderCommittedCarrierShipmentFactsReader committedShippingGroups,
        IOrderAdminEffectiveAccessReader access)
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
        _shippingGroupHistory = shippingGroupHistory;
        _committedShippingGroups = committedShippingGroups;
        _access = access;
    }

    /// <summary>صفحهٔ جزئیات را برای Checkout بارگذاری‌شده می‌سازد.</summary>
    public async Task<Result<AdminOrderDetailPage>> ComposeAsync(
        CheckoutGroup group,
        Guid viewerUserId,
        CancellationToken cancellationToken)
    {
        var checkoutId = group.CheckoutId;
        var sellerIds = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().ToList();
        var sellerNames = await _parties.GetDisplayNamesAsync(sellerIds, cancellationToken);
        var variantIds = group.SellerOrders.SelectMany(x => x.Lines)
            .Select(x => x.CatalogVariantId).Distinct().ToList();
        var titles = await _catalog.GetVariantTitlesAsync(variantIds, cancellationToken);
        var lineTitles = group.SellerOrders
            .SelectMany(x => x.Lines)
            .GroupBy(x => x.LineId)
            .ToDictionary(
                g => g.Key,
                g => titles.TryGetValue(g.First().CatalogVariantId, out var title) ? title : "کالای سفارش");

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
            // TB-258: returned capacity is its own authority; never derived from packed/shipped here.
            var returnedUsableByLine = fulfillment?.Items.ToDictionary(x => x.OrderLineId, x => x.ReturnedUsableQuantity)
                ?? new Dictionary<Guid, decimal>();
            var resendableByLine = fulfillment?.Items.ToDictionary(x => x.OrderLineId, x => x.ResendableQuantity)
                ?? new Dictionary<Guid, decimal>();
            var deliveredByLine = fulfillment?.Items.ToDictionary(x => x.OrderLineId, x => x.DeliveredQuantity)
                ?? new Dictionary<Guid, decimal>();

            var lines = order.Lines.Select(line =>
            {
                titles.TryGetValue(line.CatalogVariantId, out var title);
                shippedByLine.TryGetValue(line.LineId, out var shipped);
                packedByLine.TryGetValue(line.LineId, out var packed);
                processingByLine.TryGetValue(line.LineId, out var processing);
                // TB-257: ONE owning allocation reading instead of a fourth local formula.
                var openAllocated = fulfillment is null
                    ? 0m
                    : FulfillmentPackedQuantityAuthority.OpenAllocatedQuantity(fulfillment, line.LineId);
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
                    fulfillment is null ? null : processing,
                    fulfillment is null ? null : (returnedUsableByLine.TryGetValue(line.LineId, out var returnedUsable) ? returnedUsable : 0m),
                    fulfillment is null ? null : (resendableByLine.TryGetValue(line.LineId, out var resendable) ? resendable : 0m),
                    fulfillment is null ? null : (deliveredByLine.TryGetValue(line.LineId, out var delivered) ? delivered : 0m));
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
                    canAdd,
                    s.ShippingGroupId,
                    string.IsNullOrWhiteSpace(s.TrackingReference)
                        ? FulfillmentInternalTrackingReference.ForShipment(s.ShipmentId)
                        : null,
                    s.ResendOfShipmentId,
                    s.NonDeliveryReason,
                    s.NonDeliveryNote,
                    s.NonDeliveredAt,
                    s.ReturnStartedAt,
                    s.ReturnedAt,
                    s.ReturnReceipt is null
                        ? null
                        : s.ReturnReceipt.Items
                            .Select(i => new AdminReturnReceiptLineView(
                                i.OrderLineId,
                                i.Quantity,
                                i.Condition.ToString()))
                            .ToList());
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

        var historyGroups = await _shippingGroupHistory.GetForCheckoutAsync(checkoutId, cancellationToken);
        var factsByGroup = (await _committedShippingGroups.ListByCheckoutAsync(checkoutId, cancellationToken))
            .ToDictionary(x => x.ShippingGroupId);
        var destinationFingerprint = MarketplaceOperationalDeliveryGroupAuthority.BuildDestinationFingerprint(
            group.ProvinceName,
            group.CityName,
            group.PostalCode,
            group.PostalAddress);
        var operationalFacts = new List<MarketplaceOperationalDeliveryGroupAuthority.GroupFact>(historyGroups.Count);
        var shippingGroups = historyGroups.Select(h =>
        {
            factsByGroup.TryGetValue(h.ShippingGroupId, out var facts);
            var transportMode = facts?.TransportMode
                ?? (h.Evidence?.CarrierQuote is not null ? "CarrierApi" : "LegacyIncomplete");
            var isCarrierApi = string.Equals(transportMode, "CarrierApi", StringComparison.Ordinal);
            // Generic parcel modal is Manual/Free only; CarrierApi uses dedicated Shipping execution UI.
            var eligible = facts is not null
                && !isCarrierApi
                && !facts.IsCancelled
                && !facts.PaymentLocked
                && facts.ExecutableLines.Count > 0;
            string? blockFa = null;
            if (historyGroups.Count == 0)
            {
                blockFa = null;
            }
            else if (facts is null)
            {
                blockFa = "شاهد اجرایی این گروه در دسترس نیست.";
            }
            else if (isCarrierApi)
            {
                blockFa = "این گروه حامل API است؛ ایجاد مرسوله فقط از پنل اجرای حامل همین گروه انجام می‌شود.";
            }
            else if (facts.IsCancelled)
            {
                blockFa = "سفارش لغو شده است؛ ایجاد مرسوله ممکن نیست.";
            }
            else if (facts.PaymentLocked)
            {
                blockFa = "پرداخت/وضعیت سفارش اجازهٔ ایجاد مرسوله نمی‌دهد.";
            }
            else if (facts.ExecutableLines.Count == 0)
            {
                blockFa = "مقدار قابل ارسال باقی‌مانده‌ای در این گروه وجود ندارد.";
            }

            // Display-only fallback (not used for operational merge identity).
            var displayDate = h.Evidence?.ReceivingDate
                ?? h.RequestedDeliveryDate
                ?? (DateOnly?)h.MinimumDeliveryDate;
            var window = FormatReceivingWindow(h.Evidence)
                ?? h.RequestedDeliveryTimeWindow;
            var fee = h.Evidence?.PayableAtCheckoutAmount ?? h.ShippingChargeSnapshot;
            operationalFacts.Add(OperationalDeliveryGroupFactMapper.Map(
                checkoutId,
                facts,
                h,
                destinationFingerprint,
                eligible,
                blockFa));
            return new AdminAcceptedShippingGroupView(
                h.ShippingGroupId,
                h.SellerOrderId,
                h.SellerPartyId,
                h.MethodCode,
                h.MethodLabel,
                h.ServiceCode,
                h.OptionCode,
                h.ShippingChargeSnapshot,
                h.Currency,
                displayDate,
                window,
                h.Evidence?.ReceivingTimeZone,
                h.Evidence?.OwnerKind,
                h.Evidence?.OwnerTenantKey,
                h.Evidence?.OwnerSellerPartyId,
                h.OrderLineIds,
                eligible,
                blockFa,
                fee,
                h.Evidence?.CarrierCollectAmount ?? 0m,
                transportMode);
        }).ToList();

        var eligibleShipmentGroupIds = sellerOrders
            .SelectMany(s => s.Shipments ?? Array.Empty<AdminShipmentView>())
            .Where(s => s.ShippingGroupId is Guid
                && string.Equals(s.Status, "Created", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(s.ActivePackageNumber)
                && string.IsNullOrWhiteSpace(s.PackageLockedReasonFa)
                && !string.IsNullOrWhiteSpace(s.ShippingMethodCode))
            .Select(s => s.ShippingGroupId!.Value)
            .ToHashSet();
        var paymentLockedAny = factsByGroup.Values.Any(f => f.PaymentLocked);
        var historyByGroupId = historyGroups.ToDictionary(h => h.ShippingGroupId);

        // TB-253-R2: readiness must reflect the actual actor, not a hard-coded true.
        var actorMayManage = false;
        if (viewerUserId != Guid.Empty)
        {
            var effective = await _access.GetAsync(viewerUserId, cancellationToken);
            actorMayManage = AdminOrderOperationsOrchestrator.Has(effective, "order.handle")
                || AdminOrderOperationsOrchestrator.Has(effective, "fulfillment.manage");
        }

        var operationalDeliveryGroups = MarketplaceOperationalDeliveryGroupAuthority
            .Project(operationalFacts)
            .Select(op =>
            {
                var ready = MarketplaceOperationalDeliveryGroupAuthority.WithGroupWorkflowReadiness(
                    op,
                    eligibleShipmentGroupIds,
                    actorMayManage,
                    paymentLockedAny);
                return new AdminOperationalDeliveryGroupView(
                    ready.OperationalGroupKey,
                    ready.MethodLabel,
                    ready.MethodCode,
                    ready.ServiceCode,
                    ready.OptionCode,
                    ready.DeliveryDate,
                    ready.DeliveryTimeWindow,
                    ready.ResponsibilityLabelFa,
                    ready.IsCrossSellerMarketplace,
                    ready.SeparateReasonFa,
                    ready.ConsolidateEligible,
                    ready.ConsolidateBlockReasonFa,
                    ready.CombinedFeeTotal,
                    ready.Currency,
                    ready.Members.Select(m => new AdminOperationalDeliveryMemberView(
                        m.ShippingGroupId,
                        m.SellerOrderId,
                        m.SellerPartyId,
                        sellerNames.TryGetValue(m.SellerPartyId, out var name) ? name : "فروشنده",
                        m.OrderLineIds,
                        m.CreateParcelEligible,
                        m.CreateParcelBlockReasonFa,
                        m.TransportMode,
                        m.FeeAmount,
                        m.Currency,
                        factsByGroup.TryGetValue(m.ShippingGroupId, out var memberFacts)
                            ? memberFacts.ExecutableLines
                                .Select(line => new AdminOperationalDeliveryLineView(
                                    line.OrderLineId,
                                    lineTitles.TryGetValue(line.OrderLineId, out var lineTitle)
                                        ? lineTitle
                                        : "کالای سفارش",
                                    line.RemainingPackedQuantity,
                                    line.PackedQuantity))
                                .ToList()
                            : Array.Empty<AdminOperationalDeliveryLineView>())).ToList(),
                    ready.Members.Select(m => new AdminOperationalFeeBreakdownView(
                        m.ShippingGroupId,
                        sellerNames.TryGetValue(m.SellerPartyId, out var feeName) ? feeName : "فروشنده",
                        m.FeeAmount,
                        m.Currency)).ToList(),
                    actorMayManage,
                    ResolvePlannedDispatch(ready.Members, historyByGroupId));
            })
            .ToList();

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
            reservationAudit,
            shippingGroups,
            operationalDeliveryGroups));
    }

    private static string? FormatReceivingWindow(OrderShippingGroupHistoryEvidence? evidence)
    {
        if (evidence is null)
        {
            return null;
        }

        var open = TimeSpan.FromTicks(evidence.ReceivingOpenTicks);
        var close = TimeSpan.FromTicks(evidence.ReceivingCloseTicks);
        return $"{open:hh\\:mm}–{close:hh\\:mm}";
    }

    /// <summary>
    /// TB-256-R1: موعد ارسال کارت عملیاتی فقط از برنامهٔ پذیرفته‌شده خوانده می‌شود (هیچ بازمحاسبه با
    /// تنظیمات جاری). وقتی اعضا موعدهای متفاوت دارند، زودترین موعد نمایش داده می‌شود و تعداد برنامه‌های
    /// متفاوت هم گزارش می‌شود تا یک واقعیت جعلی ساخته نشود.
    /// </summary>
    private static AdminPlannedDispatchView? ResolvePlannedDispatch(
        IReadOnlyList<MarketplaceOperationalDeliveryGroupAuthority.GroupFact> members,
        IReadOnlyDictionary<Guid, OrderShippingGroupHistoryRecord> historyByGroupId)
    {
        var plans = new List<OrderShippingGroupHistoryDispatchPlan>();
        foreach (var member in members)
        {
            if (!historyByGroupId.TryGetValue(member.ShippingGroupId, out var history)
                || history.Evidence?.PlannedDispatch is not { } plan)
            {
                continue;
            }

            plans.Add(plan);
        }

        if (plans.Count == 0)
        {
            return null;
        }

        var distinct = plans
            .GroupBy(
                plan => string.Join(
                    '\u001f',
                    plan.HandoffDate.ToString("yyyy-MM-dd"),
                    plan.OpenTicks,
                    plan.CloseTicks,
                    plan.TimeZone),
                StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(plan => plan.HandoffDate)
            .ThenBy(plan => plan.OpenTicks)
            .ToList();

        var chosen = distinct[0];
        var open = TimeSpan.FromTicks(chosen.OpenTicks);
        var close = TimeSpan.FromTicks(chosen.CloseTicks);

        return new AdminPlannedDispatchView(
            chosen.HandoffDate,
            $"{open:hh\\:mm}–{close:hh\\:mm}",
            chosen.TimeZone,
            chosen.TransportWorkingDays,
            chosen.PolicyVersion,
            distinct.Count);
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
