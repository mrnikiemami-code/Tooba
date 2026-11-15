using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;

namespace Tooba.Order.Application.Admin.Operations.Services;

/// <summary>
/// TB-270-R2 — the OWN dispatch plan of an existing <c>Created</c> resend/replacement parcel.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two-stage invariant.</b> Fulfillment created the physical parcel and its source allocation through its existing
/// occurrence mechanism; this boundary only confirms when that exact parcel must be prepared, leave and reach the
/// carrier. Between the two stages the parcel is «نیازمند زمان‌بندی» and is not dispatch-authorized.
/// </para>
/// <para>
/// <b>No invention.</b> The captured inputs come from the seller's OWN accepted evidence through the read-only
/// Shipping lookup, the calendar/deadlines come from the ONE existing scheduling authority, and readiness is proven
/// by the owning Fulfillment facts (origin receipt, or the replacement's own hold/decision). A missing capture, an
/// unresolvable zone, an unreachable date or a stock-pending decision produces a typed blocker, never a plausible
/// date or a borrowed original plan.
/// </para>
/// </remarks>
public sealed partial class AdminOrderOperationsOrchestrator
{
    /// <summary>عرضهٔ تقویم مشتری برای برنامهٔ یک مرسوله؛ کران صریح و کوچک است.</summary>
    public const int ShipmentPlanPreviewHorizonDates = 10;

    /// <summary>دقیقه‌های مجاز یک نوبت تحویل برای خواندن ورودی‌های capture‌شده.</summary>
    private const string PlanStepParseFormatTime = "HH\\:mm";

    /// <summary>پیش‌نمایش فقط‌خواندنی برنامهٔ ارسال همین مرسوله.</summary>
    public Task<Result<AdminShipmentDispatchPlanPreview>> PreviewShipmentDispatchPlanAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid shipmentId,
        CancellationToken cancellationToken) =>
        RunPlanReadAsync(() => PreviewShipmentDispatchPlanCoreAsync(
            checkoutId,
            actorUserId,
            shipmentId,
            cancellationToken));

    /// <summary>تأیید برنامهٔ ارسال همین مرسوله (تنها نوشتن Order؛ هیچ مرسوله/رزرو/تصمیمی ساخته نمی‌شود).</summary>
    public Task<Result<object>> PlanShipmentDispatchAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => PlanShipmentDispatchCoreAsync(
            checkoutId,
            actorUserId,
            request,
            cancellationToken));

    private static async Task<Result<AdminShipmentDispatchPlanPreview>> RunPlanReadAsync(
        Func<Task<AdminShipmentDispatchPlanPreview>> execute)
    {
        try
        {
            return Result.Success(await execute());
        }
        catch (AdminOrderOperationsException ex)
        {
            return Result.Failure<AdminShipmentDispatchPlanPreview>(new SemanticError(ex.Code));
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure<AdminShipmentDispatchPlanPreview>(new SemanticError(ex.Code));
        }
    }

    private async Task<AdminShipmentDispatchPlanPreview> PreviewShipmentDispatchPlanCoreAsync(
        Guid checkoutId,
        Guid actorUserId,
        Guid shipmentId,
        CancellationToken cancellationToken)
    {
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
        // Preview is a read, but it still requires the same physical-operation permission as the confirmation.
        if (!HasAny(effective, "order.handle", "fulfillment.manage"))
        {
            throw new AdminOrderOperationsException("order.operation.denied");
        }

        if (shipmentId == Guid.Empty)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        // TB-270-R4 Repair B: the cancellation gate is NOT applied to proving which parcel this is. An already
        // recorded plan must stay readable (history) after the order was cancelled; cancellation only refuses
        // OFFERING a new calendar, which is decided inside BuildPreviewAsync after the stored row is read.
        var located = await LocateParcelForReadAsync(checkoutId, group, shipmentId, cancellationToken);
        return await BuildPreviewAsync(located, cancellationToken);
    }

    private async Task<object> PlanShipmentDispatchCoreAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);

        // The exact same actor gate as the physical operations, re-checked on every request including a replay.
        if (!HasAny(effective, "order.handle", "fulfillment.manage"))
        {
            throw new AdminOrderOperationsException("order.operation.denied");
        }

        var shipmentId = RequireShipmentId(request);
        var reviewedKey = (request.IdempotencyKey ?? string.Empty).Trim();
        if (reviewedKey.Length == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        var locatedAny = await LocateParcelForReadAsync(checkoutId, group, shipmentId, cancellationToken);

        // An exact replay is answered from the stored fact before ANY mutable state/feasibility gate, so a lost
        // response after commit never re-derives (and never changes) the confirmed promise. A parcel that has since
        // been dispatched/delivered/cancelled — or an order cancelled afterwards — still answers its own recorded plan
        // and this read-only answer re-enables nothing.
        var existing = await _shipmentPlanStore.FindAsync(shipmentId, cancellationToken);
        if (existing is not null)
        {
            if (existing.CheckoutId != checkoutId
                || existing.FulfillmentId != locatedAny.Fulfillment.FulfillmentId
                || existing.ShipmentId != shipmentId)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            if (!string.Equals(existing.OccurrenceKey, reviewedKey, StringComparison.Ordinal))
            {
                throw new AdminOrderOperationsException("order.shipment_plan.conflict");
            }

            if (!SelectionsMatch(existing, request))
            {
                throw new AdminOrderOperationsException("order.shipment_plan.conflict");
            }

            return PlanResult(existing, replayed: true);
        }

        var located = await RequireNewPlanGatesAsync(locatedAny, cancellationToken);
        var plan = await BuildPlanAsync(located, actorUserId, reviewedKey, request, cancellationToken);
        var outcome = await _shipmentPlanStore.TryInsertAsync(plan, cancellationToken);
        switch (outcome)
        {
            case OrderShipmentDispatchPlanInsertOutcome.Inserted:
                return PlanResult(plan, replayed: false);
            case OrderShipmentDispatchPlanInsertOutcome.DuplicateExact:
            {
                var winner = await _shipmentPlanStore.FindAsync(shipmentId, cancellationToken)
                    ?? throw new AdminOrderOperationsException("order.shipment_plan.conflict");
                return PlanResult(winner, replayed: true);
            }
            default:
                throw new AdminOrderOperationsException("order.shipment_plan.conflict");
        }
    }

    private static object PlanResult(OrderShipmentDispatchPlan plan, bool replayed)
        => new
        {
            ok = true,
            code = "plan_shipment_dispatch",
            replayed,
            shipmentId = plan.ShipmentId,
            planId = plan.PlanId,
            shippingGroupId = plan.ShippingGroupId,
            attemptKind = plan.AttemptKind,
            occurrenceKey = plan.OccurrenceKey,
            receivingDate = plan.CustomerReceivingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            receivingOpensAt = FormatTime(plan.CustomerReceivingOpenTicks),
            receivingClosesAt = FormatTime(plan.CustomerReceivingCloseTicks),
            receivingTimeZone = plan.CustomerReceivingTimeZone,
            handoffDate = plan.HandoffDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            handoffOpensAt = FormatTime(plan.HandoffOpenTicks),
            handoffClosesAt = FormatTime(plan.HandoffCloseTicks),
            handoffTimeZone = plan.HandoffTimeZone,
            preparationStartUtc = plan.PreparationStartUtc,
            transferDepartureUtc = plan.TransferDepartureUtc,
            handoffDeadlineUtc = plan.HandoffDeadlineUtc,
            preparationMinutes = plan.PreparationMinutes,
            handoffMinutes = plan.HandoffMinutes,
            transportWorkingDays = plan.TransportWorkingDays,
            policyVersion = plan.PolicyVersion,
            confirmedAtUtc = plan.ConfirmedAtUtc,
        };

    /// <summary>یک مرسولهٔ فیزیکی دقیق با مالکیت/وضعیت زندهٔ خودش.</summary>
    private sealed record LocatedParcel(
        AdminOrderOpsCheckoutSnapshot Checkout,
        AdminOrderOpsSellerOrderSnapshot SellerOrder,
        FulfillmentSnapshot Fulfillment,
        ShipmentSnapshot Shipment,
        IReadOnlyList<FulfillmentSnapshot> AllFulfillments)
    {
        /// <summary>گونهٔ تلاش فیزیکی همین مرسوله.</summary>
        public string AttemptKind => Shipment.IsReplacement
            ? OrderShipmentAttemptKinds.Replacement
            : OrderShipmentAttemptKinds.Resend;

        /// <summary>آیا این مرسوله یک تلاش جدیدِ مرتبط است (نه ارسال اول).</summary>
        public bool IsLinkedAttempt => Shipment.IsReplacement || Shipment.ResendOfShipmentId is not null;
    }

    /// <summary>
    /// Existence/ownership of one exact parcel in this checkout — no mutable/eligibility gate.
    /// </summary>
    /// <remarks>
    /// Used by the read paths (an already-confirmed plan must still be readable after dispatch) and to prove the parcel
    /// really belongs to this checkout before any planning work. It never proves the parcel is plannable.
    /// </remarks>
    private async Task<LocatedParcel> LocateParcelForReadAsync(
        Guid checkoutId,
        AdminOrderOpsCheckoutSnapshot checkout,
        Guid shipmentId,
        CancellationToken cancellationToken)
    {
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var fulfillment = fulfillments.FirstOrDefault(x => x.Shipments.Any(s => s.ShipmentId == shipmentId))
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        var shipment = fulfillment.Shipments.Single(x => x.ShipmentId == shipmentId);
        var sellerOrder = checkout.SellerOrders.FirstOrDefault(x => x.SellerOrderId == fulfillment.SellerOrderId)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        return new LocatedParcel(checkout, sellerOrder, fulfillment, shipment, fulfillments);
    }

    /// <summary>
    /// The mutable/eligibility gates: only a live <c>Created</c> linked parcel of a paid order outside an active
    /// common package can obtain a NEW plan. Stored plans are read through <see cref="LocateParcelForReadAsync"/>.
    /// </summary>
    private LocatedParcel RequirePlannable(LocatedParcel located)
    {
        var parcel = located;
        var shipment = parcel.Shipment;
        // Only an EXACT live Created linked parcel can be planned. First dispatch keeps its original accepted plan;
        // dispatched/delivered/cancelled history never gains a planning action and never becomes plannable again.
        if (!shipment.IsReplacement && shipment.ResendOfShipmentId is null)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.first_dispatch_not_plannable");
        }

        if (shipment.Status != ShipmentOperationStatus.Created || shipment.DispatchedAt is not null)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.not_created");
        }

        if (parcel.SellerOrder.Status != SellerOrderStatus.Paid)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.payment_required");
        }

        if (shipment.ShippingGroupId is not { } shippingGroupId || shippingGroupId == Guid.Empty)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.group_missing");
        }

        return parcel;
    }

    /// <summary>
    /// The mutable/eligibility gates plus the active-common-package lock: a parcel inside a live package is operated
    /// from the package, and only a live <c>Created</c> linked parcel of a paid order can obtain a new plan.
    /// </summary>
    private async Task<LocatedParcel> RequirePlannableAsync(
        LocatedParcel located,
        CancellationToken cancellationToken)
    {
        var membership = await _fulfillment.GetActiveMembershipByShipmentIdsAsync(
            [located.Shipment.ShipmentId],
            cancellationToken);
        if (membership.Any(x => x.ShipmentId == located.Shipment.ShipmentId
            && x.PackageStatus is ConsolidatedPackageOperationStatus.Created or ConsolidatedPackageOperationStatus.Dispatched))
        {
            throw new AdminOrderOperationsException("order.shipment_plan.package_locked");
        }

        return RequirePlannable(located);
    }

    /// <summary>
    /// TB-270-R4 Repair B: the ONE gate composition for a NEW plan (preview calendar or confirmation insert).
    /// </summary>
    /// <remarks>
    /// An exact recorded plan never reaches this method: it is answered from the stored row first, so a cancelled
    /// order or a parcel inside an active package still exposes its own immutable history and re-enables nothing.
    /// A mutation that would create a row must pass BOTH the checkout-cancellation gate and the owning asynchronous
    /// active-package membership check — a UI-only safeguard or the synchronous eligibility helper alone is not enough.
    /// </remarks>
    private async Task<LocatedParcel> RequireNewPlanGatesAsync(
        LocatedParcel located,
        CancellationToken cancellationToken)
    {
        if (IsCheckoutCancelled(located.Checkout))
        {
            throw new AdminOrderOperationsException("order.cancelled.blocks_action");
        }

        return await RequirePlannableAsync(located, cancellationToken);
    }

    /// <summary>
    /// همهٔ ورودی‌های زمان‌بندی همین تلاش: شاهد پذیرفته‌شدهٔ گروه، ورودی‌های capture‌شده و منبع آماده‌بودن فیزیکی.
    /// </summary>
    private sealed record PlanInputs(
        LocatedParcel Parcel,
        AcceptedShipmentGroupBinding Binding,
        ShippingAttemptSchedulingInputs Captured,
        string ReadinessSource,
        DateTimeOffset ReadinessAtUtc,
        string SourceFingerprint);

    private async Task<PlanInputs> ResolvePlanInputsAsync(
        LocatedParcel parcel,
        CancellationToken cancellationToken)
    {
        var shipment = parcel.Shipment;
        var shippingGroupId = shipment.ShippingGroupId!.Value;
        var binding = await _acceptedShipmentBindings.GetAsync(
                parcel.Checkout.CheckoutId,
                shippingGroupId,
                cancellationToken)
            ?? throw new AdminOrderOperationsException("order.shipment_plan.binding_missing");

        // The accepted capture must belong to THIS checkout/seller order/seller/group/method. A group id that resolves
        // to a different owner's evidence is never usable, and the group's recorded method must be the parcel's method.
        if (binding.CheckoutId != parcel.Checkout.CheckoutId
            || binding.SellerOrderId != parcel.SellerOrder.SellerOrderId
            || binding.ShippingGroupId != shippingGroupId
            || (binding.SellerPartyId != Guid.Empty
                && parcel.Fulfillment.SellerPartyId != Guid.Empty
                && binding.SellerPartyId != parcel.Fulfillment.SellerPartyId)
            || (shipment.ShippingMethodCode.Length != 0
                && !CanonicalEquals(shipment.ShippingMethodCode, binding.MethodCode)))
        {
            throw new AdminOrderOperationsException("order.shipment_plan.binding_mismatch");
        }

        if (string.IsNullOrWhiteSpace(binding.ServiceCode))
        {
            throw new AdminOrderOperationsException("order.shipment_plan.service_missing");
        }

        // TB-270-R3 Repair A: the lookup addresses the CAPTURED area group (<c>payload.Groups[].GroupId</c>) and its
        // exact rate row. The Order checkout shipping-group id is a different identity namespace and must never be
        // sent here, or the seam answers a different/absent group.
        var lookup = await _schedulingLookup.LookupAsync(
            new ShippingAttemptSchedulingLookupRequest(
                ToShippingOwnerKind(binding.OwnerKind),
                binding.OwnerTenantKey,
                binding.OwnerSellerPartyId,
                binding.DraftId,
                binding.SourceRevision,
                binding.ServiceCode,
                binding.OptionCode,
                binding.AreaGroupId,
                binding.RateRowId,
                binding.SnapshotId),
            cancellationToken);

        if (!lookup.HasInputs)
        {
            // A typed refusal keeps its own stable reason; it never becomes "no plan possible" or an invented calendar.
            throw new AdminOrderOperationsException(lookup.ReasonCode);
        }

        var captured = lookup.Inputs!;

        // The returned capture must be the SAME immutable revision/group/rate row the accepted evidence names. A
        // mismatch means the authored revision changed under the historical acceptance; planning is refused.
        if (captured.SnapshotId != binding.SnapshotId
            || captured.DraftId != binding.DraftId
            || captured.SourceRevision != binding.SourceRevision
            || captured.FormatVersion != binding.FormatVersion
            || captured.GroupId != binding.AreaGroupId
            || captured.RateRowId != binding.RateRowId
            || !CanonicalEquals(captured.ServiceCode, binding.ServiceCode)
            || !CanonicalEquals(captured.OptionCode, binding.OptionCode)
            || captured.OwnerKind != ToShippingOwnerKind(binding.OwnerKind)
            || !string.Equals(captured.OwnerTenantId, binding.OwnerTenantKey, StringComparison.Ordinal)
            || captured.OwnerSellerPartyId != binding.OwnerSellerPartyId)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.binding_mismatch");
        }
        if (!captured.HasCompleteInputs
            || captured.ReceivingWeek is null
            || captured.CarrierWeek is null
            || captured.PreparationMinutes is null
            || captured.HandoffMinutes is null)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.captured_inputs_incomplete");
        }

        var (readinessSource, readinessAt) = await ResolveReadinessAsync(parcel, cancellationToken);
        return new PlanInputs(
            parcel,
            binding,
            captured,
            readinessSource,
            readinessAt,
            BuildSourceFingerprint(shipment));
    }

    /// <summary>
    /// آماده‌بودن فیزیکی از واقعیت مالک: رسید دریافت سالم (ارسال مجدد) یا رزرو/تصمیم همان جایگزینی.
    /// </summary>
    private async Task<(string Source, DateTimeOffset ReadyAt)> ResolveReadinessAsync(
        LocatedParcel parcel,
        CancellationToken cancellationToken)
    {
        var shipment = parcel.Shipment;
        if (shipment.IsReplacement)
        {
            // TB-270-R3 Repair C: a projected decision status is NOT readiness. The OWNING Fulfillment read verifies
            // the decision, the parcel's exact line/quantity and Inventory's CURRENT hold in one place.
            if (_replacementReadiness is null)
            {
                throw new AdminOrderOperationsException("order.shipment_plan.replacement_hold_missing");
            }

            var readiness = await _replacementReadiness.GetAsync(
                parcel.Fulfillment.FulfillmentId,
                shipment.ShipmentId,
                cancellationToken);
            if (!readiness.IsReady)
            {
                throw new AdminOrderOperationsException(
                    readiness.ReasonCode is FulfillmentReplacementReadinessReasonCodes.HoldQuantityMismatch
                        or FulfillmentReplacementReadinessReasonCodes.LineMismatch
                        ? "order.shipment_plan.source_quantity_mismatch"
                        : "order.shipment_plan.replacement_hold_missing");
            }

            // The fact is about THIS parcel and its exact recorded chain; a fact for any other identity is refused.
            if (readiness.ShipmentId != shipment.ShipmentId
                || readiness.FulfillmentId != parcel.Fulfillment.FulfillmentId
                || readiness.SourceShipmentId != shipment.ResendOfShipmentId
                || readiness.DamagedResolutionId != shipment.DamagedResolutionId
                || readiness.ReplacementReservationId != shipment.ReplacementReservationId
                || readiness.ReadyAtUtc is null)
            {
                throw new AdminOrderOperationsException("order.shipment_plan.replacement_hold_missing");
            }

            return (OrderShipmentReadinessSources.ReplacementHold, readiness.ReadyAtUtc.Value);
        }

        var resendOf = shipment.ResendOfShipmentId
            ?? throw new AdminOrderOperationsException("order.shipment_plan.source_missing");
        var returned = parcel.AllFulfillments
            .SelectMany(x => x.Shipments)
            .FirstOrDefault(x => x.ShipmentId == resendOf)
            ?? throw new AdminOrderOperationsException("order.shipment_plan.source_missing");

        if (returned.Status != ShipmentOperationStatus.Returned || returned.ReturnReceipt is null)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.source_not_ready");
        }

        // Only the SOURCE parcel's own usable received quantity funds this resend; damaged units never do, and a
        // sibling returned parcel can never make this one ready.
        var usableByLine = returned.ReturnReceipt.Items
            .Where(x => x.Condition == ReturnReceiptCondition.Usable)
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        foreach (var line in shipment.Items)
        {
            if (!usableByLine.TryGetValue(line.OrderLineId, out var usable) || usable < line.Quantity)
            {
                throw new AdminOrderOperationsException("order.shipment_plan.source_quantity_mismatch");
            }
        }

        return (OrderShipmentReadinessSources.ReturnReceipt, returned.ReturnedAt ?? returned.ReturnReceipt.ReceivedAt);
    }

    /// <summary>برنامهٔ ممکن همین مرسوله از شاهد تاریخی؛ نبود/امتناع یک مانع نوع‌دار است.</summary>
    private async Task<AdminShipmentDispatchPlanPreview> BuildPreviewAsync(
        LocatedParcel parcel,
        CancellationToken cancellationToken)
    {
        var confirmed = await _shipmentPlanStore.FindAsync(parcel.Shipment.ShipmentId, cancellationToken);
        if (confirmed is not null)
        {
            // An already-confirmed plan is HISTORY: it is reported from the stored row and no new calendar is derived,
            // so a dispatched/delivered parcel still shows its exact promise and offers no second action.
            return HistoryPreview(parcel, confirmed);
        }

        // Eligibility is a REFUSAL, not a blocker: an unplanned parcel that is not a live Created linked attempt (or
        // that is locked in an active package, or whose checkout was cancelled) can never be scheduled, so no
        // calendar/blocker is derived for it. Only the captured-inputs/feasibility stage below answers with a
        // truthful blocker. This is the SAME gate composition the confirmation uses before inserting a row.
        var plannable = await RequireNewPlanGatesAsync(parcel, cancellationToken);

        try
        {
            var inputs = await ResolvePlanInputsAsync(plannable, cancellationToken);
            var anchor = Max(_clock.UtcNow, inputs.ReadinessAtUtc);
            var options = BuildOptions(inputs, anchor);
            return PreviewOf(inputs, options);
        }
        catch (AdminOrderOperationsException blocked)
        {
            // A truthful blocker still returns the exact parcel/binding context it could establish.
            return BlockedPreview(parcel, blocked.Code);
        }
    }

    private static AdminShipmentDispatchPlanPreview HistoryPreview(
        LocatedParcel parcel,
        OrderShipmentDispatchPlan confirmed)
        => new(
            parcel.Checkout.CheckoutId,
            parcel.SellerOrder.SellerOrderId,
            parcel.Fulfillment.FulfillmentId,
            parcel.Shipment.ShipmentId,
            confirmed.ShippingGroupId,
            parcel.AttemptKind,
            parcel.Shipment.ResendOfShipmentId,
            parcel.Shipment.DamagedResolutionId,
            parcel.Shipment.ReplacementReservationId,
            confirmed.SourceFingerprint,
            confirmed.ReadinessSource,
            confirmed.ReadinessAtUtc,
            confirmed.MethodCode,
            confirmed.MethodLabel,
            confirmed.SnapshotId,
            confirmed.DraftId,
            confirmed.SourceRevision,
            confirmed.FormatVersion,
            confirmed.AreaGroupId,
            confirmed.RateRowId,
            confirmed.ConfirmedAtUtc,
            [],
            ConfirmedOf(confirmed),
            HistoryOnly: true);

    private AdminShipmentDispatchPlanPreview PreviewOf(
        PlanInputs inputs,
        IReadOnlyList<AdminShipmentDispatchPlanOption> options)
    {
        var parcel = inputs.Parcel;
        var binding = inputs.Binding;
        return new AdminShipmentDispatchPlanPreview(
            parcel.Checkout.CheckoutId,
            parcel.SellerOrder.SellerOrderId,
            parcel.Fulfillment.FulfillmentId,
            parcel.Shipment.ShipmentId,
            binding.ShippingGroupId,
            parcel.AttemptKind,
            parcel.Shipment.ResendOfShipmentId,
            parcel.Shipment.DamagedResolutionId,
            parcel.Shipment.ReplacementReservationId,
            inputs.SourceFingerprint,
            inputs.ReadinessSource,
            inputs.ReadinessAtUtc,
            binding.MethodCode,
            binding.MethodLabel,
            binding.SnapshotId,
            binding.DraftId,
            binding.SourceRevision,
            binding.FormatVersion,
            binding.AreaGroupId,
            binding.RateRowId,
            _clock.UtcNow,
            options,
            null,
            options.Count == 0 ? "order.shipment_plan.no_feasible_date" : null,
            options.Count == 0
                ? "با ورودی‌های زمان‌بندی همین روش، تاریخ قابل ارائه‌ای برای این مرسوله پیدا نشد؛ زمان‌بندی تأیید نشد."
                : null,
            SchedulingReasonCode: null,
            ReviewFingerprint: BuildReviewFingerprint(inputs));
    }

    private AdminShipmentDispatchPlanPreview BlockedPreview(
        LocatedParcel parcel,
        string blockerCode)
    {
        return new AdminShipmentDispatchPlanPreview(
            parcel.Checkout.CheckoutId,
            parcel.SellerOrder.SellerOrderId,
            parcel.Fulfillment.FulfillmentId,
            parcel.Shipment.ShipmentId,
            parcel.Shipment.ShippingGroupId,
            parcel.AttemptKind,
            parcel.Shipment.ResendOfShipmentId,
            parcel.Shipment.DamagedResolutionId,
            parcel.Shipment.ReplacementReservationId,
            BuildSourceFingerprint(parcel.Shipment),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            _clock.UtcNow,
            [],
            null,
            blockerCode,
            BlockerFa(blockerCode));
    }

    private static string BlockerFa(string code) => code switch
    {
        "order.shipment_plan.binding_missing" =>
            "شاهد ارسال پذیرفته‌شدهٔ این گروه موجود نیست؛ زمان‌بندی از داده‌های جاری بازسازی نمی‌شود.",
        "order.shipment_plan.binding_mismatch" =>
            "شاهد پذیرفته‌شده با گروه/خدمت/نسخهٔ ثبت‌شدهٔ این مرسوله یکی نیست؛ زمان‌بندی از آن ساخته نمی‌شود.",
        "order.shipment_plan.review_context_changed" =>
            "شرایط این زمان‌بندی پس از بازبینی تغییر کرده است؛ بازبینی تازه لازم است و هیچ برنامه‌ای با داده‌های قدیمی ثبت نشد.",
        "order.shipment_plan.review_required" =>
            "این تأیید باید با همان بازبینی سرور انجام شود؛ بازبینی تازه بگیرید.",
        "order.shipment_plan.service_missing" =>
            "خدمت ارسال این گروه در شاهد پذیرفته‌شده ثبت نشده است؛ زمان‌بندی ممکن نیست.",
        "order.shipment_plan.captured_inputs_unavailable" =>
            "ورودی‌های زمان‌بندی این روش از شاهد تاریخی خوانده نشد؛ هیچ تاریخ/ساعتی حدس زده نمی‌شود.",
        "order.shipment_plan.captured_inputs_incomplete" =>
            "بخشی از ورودی‌های زمان‌بندی (هفتهٔ مشتری/حامل/پردازش یا زمان آماده‌سازی) در شاهد ثبت نشده است.",
        "order.shipment_plan.source_missing" =>
            "مرسولهٔ مبدأ این تلاش پیدا نشد؛ زمان‌بندی ساخته نمی‌شود.",
        "order.shipment_plan.source_not_ready" =>
            "این مرسوله هنوز رسید دریافت در مبدأ ندارد؛ زمان‌بندی ارسال ممکن نیست.",
        "order.shipment_plan.source_quantity_mismatch" =>
            "مقدار سالم دریافتی مبدأ برای همین مقدار کافی نیست؛ زمان‌بندی ساخته نمی‌شود.",
        "order.shipment_plan.replacement_hold_missing" =>
            "تصمیم جایگزینی/رزرو موجودی سالم این مرسوله هنوز پایدار نیست؛ زمان‌بندی ساخته نمی‌شود.",
        "order.shipment_plan.payment_required" =>
            "پرداخت این سفارش تأیید نشده است؛ زمان‌بندی ارسال ممکن نیست.",
        "order.shipment_plan.group_missing" =>
            "این مرسوله به گروه ارسال متعهد وصل نیست؛ زمان‌بندی ممکن نیست.",
        "order.shipment_plan.package_locked" =>
            "این مرسوله داخل بستهٔ تجمیعی است؛ اقدام از همان بسته انجام می‌شود.",
        "order.shipment_plan.no_feasible_date" =>
            "با ورودی‌های زمان‌بندی همین روش، تاریخ قابل ارائه‌ای برای این مرسوله پیدا نشد.",
        // Refusals of the ONE scheduling authority keep their own cause instead of one vague "no date".
        "shipping.scheduling.timing_policy_unconfigured" =>
            "سیاست زمان آماده‌سازی/تحویل به حامل برای این روش تنظیم نشده است؛ تاریخ ساختگی ساخته نمی‌شود.",
        "shipping.scheduling.no_carrier_working_day" =>
            "در تقویم پذیرش حامل هیچ روز کاری وجود ندارد؛ زمان‌بندی ممکن نیست.",
        "shipping.scheduling.carrier_week_invalid" =>
            "تقویم پذیرش حامل در شاهد پذیرفته‌شده معتبر نیست؛ زمان‌بندی ممکن نیست.",
        "shipping.scheduling.customer_week_invalid" =>
            "برنامهٔ هفتگی دریافت مشتری در شاهد پذیرفته‌شده معتبر نیست؛ زمان‌بندی ممکن نیست.",
        "shipping.scheduling.no_customer_working_day" =>
            "در برنامهٔ دریافت مشتری هیچ روز کاری وجود ندارد؛ زمان‌بندی ممکن نیست.",
        "shipping.scheduling.no_receiving_date" =>
            "با این ورودی‌ها هیچ تاریخ تحویل معتبری نمی‌ماند؛ زمان‌بندی تأیید نشد.",
        "shipping.scheduling.no_feasible_handoff" =>
            "پنجرهٔ پذیرش حامل تا تاریخ ممکن به‌موقع نمی‌رسد؛ زمان‌بندی تأیید نشد.",
        "shipping.scheduling.processing_week_invalid" =>
            "هفتهٔ کاری پردازش انبار معتبر نیست؛ زمان آماده‌سازی قابل اثبات نیست.",
        "shipping.scheduling.no_processing_working_day" =>
            "در هفتهٔ پردازش انبار هیچ روز کاری نیست؛ آماده‌سازی هرگز تمام نمی‌شود.",
        "shipping.scheduling.deadline_unprovable" =>
            "لحظهٔ موعد روی این تقویم قابل اثبات نیست؛ هیچ موعدی حدس زده نمی‌شود.",
        "shipping.scheduling.preparation_search_exhausted" =>
            "زمان آماده‌سازی در کران جست‌وجو کامل نشد؛ زمان‌بندی تأیید نشد.",
        "shipping.attempt_scheduling.captured_revision_unavailable" =>
            "بازبینی capture‌شدهٔ این گروه در دسترس نیست؛ ورودی‌های زمان‌بندی خوانده نشد.",
        "shipping.attempt_scheduling.owner_scope_unavailable" =>
            "مالک این گروه ارسال در زمینهٔ جاری قابل استفاده نیست؛ زمان‌بندی ممکن نیست.",
        _ => "زمان‌بندی این مرسوله ممکن نیست.",
    };

    private static AdminShipmentDispatchPlanConfirmed ConfirmedOf(OrderShipmentDispatchPlan plan)
        => new(
            plan.PlanId,
            plan.OccurrenceKey,
            plan.CustomerReceivingDate,
            FormatTime(plan.CustomerReceivingOpenTicks),
            FormatTime(plan.CustomerReceivingCloseTicks),
            plan.CustomerReceivingTimeZone,
            plan.HandoffDate,
            FormatTime(plan.HandoffOpenTicks),
            FormatTime(plan.HandoffCloseTicks),
            plan.HandoffTimeZone,
            plan.PreparationStartUtc,
            plan.TransferDepartureUtc,
            plan.HandoffDeadlineUtc,
            plan.PreparationMinutes,
            plan.HandoffMinutes,
            plan.TransportWorkingDays,
            plan.PolicyVersion,
            plan.ConfirmedByUserId,
            plan.ConfirmedAtUtc);

    /// <summary>تقویم ممکن مشتری از مرجع واحد زمان‌بندی؛ هیچ arithmetic محلی کپی نمی‌شود.</summary>
    private static IReadOnlyList<AdminShipmentDispatchPlanOption> BuildOptions(PlanInputs inputs, DateTimeOffset anchor)
    {
        var captured = inputs.Captured;
        var receivingWeek = captured.ReceivingWeek!;
        var carrierWeek = captured.CarrierWeek!;
        var customerWeek = new ShippingSchedulingWeekView(receivingWeek.TimeZoneId, receivingWeek.Days);
        var continuous = captured.ProcessingMode == ShippingAuthoringPreparationProcessingMode.Continuous;

        var calendar = ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(
            anchor,
            carrierWeek,
            customerWeek,
            captured.MaxDeliveryDays,
            ShipmentPlanPreviewHorizonDates,
            receivingWeek.SlotDurationMinutes,
            captured.PreparationMinutes,
            captured.HandoffMinutes,
            captured.ProcessingWeek,
            continuous);
        if (!calendar.IsSuccess)
        {
            // The authority's own stable reason is the blocker; a failed calendar is never "no dates".
            throw new AdminOrderOperationsException(calendar.ReasonCode);
        }

        var options = new List<AdminShipmentDispatchPlanOption>();
        foreach (var date in calendar.Value!.Dates)
        {
            foreach (var slot in date.Slots)
            {
                var commitment = new ShippingReceivingCommitment(
                    slot.Date,
                    slot.OpensAt,
                    slot.ClosesAt,
                    receivingWeek.TimeZoneId);
                var deadlines = ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines(
                    commitment,
                    carrierWeek,
                    captured.MaxDeliveryDays,
                    captured.PreparationMinutes!.Value,
                    captured.HandoffMinutes!.Value,
                    captured.ProcessingWeek,
                    continuous);
                if (!deadlines.IsSuccess)
                {
                    // A slot whose real deadline cannot be proven (same-day boundary/DST) is never offered.
                    continue;
                }

                var value = deadlines.Value!;
                if (value.PreparationStartUtc < anchor)
                {
                    continue;
                }

                options.Add(new AdminShipmentDispatchPlanOption(
                    slot.Date,
                    FormatTime(slot.OpensAt),
                    FormatTime(slot.ClosesAt),
                    receivingWeek.TimeZoneId,
                    value.Handoff.Date,
                    FormatTime(value.Handoff.OpensAt),
                    FormatTime(value.Handoff.ClosesAt),
                    carrierWeek.TimeZoneId,
                    value.PreparationStartUtc,
                    value.TransferDepartureUtc,
                    value.HandoffDeadlineUtc));
            }
        }

        return options;
    }

    /// <summary>
    /// TB-270-R3: یک اثر انگشت غیرقابل حدس از <b>کل</b> زمینهٔ بازبینی (هویت مرسوله/منبع/تصمیم/رزرو، شاهد capture،
    /// منبع آماده‌بودن و اثر انگشت خط/مقدار). تأیید باید همان را برگرداند؛ تغییر زمینه بازبینی تازه لازم دارد.
    /// </summary>
    private static string BuildReviewFingerprint(PlanInputs inputs)
    {
        var parcel = inputs.Parcel;
        var binding = inputs.Binding;
        var captured = inputs.Captured;
        var raw = string.Join(
            '\n',
            "tb270r3:v1",
            parcel.Checkout.CheckoutId.ToString("N"),
            parcel.SellerOrder.SellerOrderId.ToString("N"),
            parcel.Fulfillment.FulfillmentId.ToString("N"),
            parcel.Shipment.ShipmentId.ToString("N"),
            binding.ShippingGroupId.ToString("N"),
            parcel.AttemptKind,
            parcel.Shipment.ResendOfShipmentId?.ToString("N") ?? "-",
            parcel.Shipment.DamagedResolutionId?.ToString("N") ?? "-",
            parcel.Shipment.ReplacementReservationId?.ToString("N") ?? "-",
            inputs.SourceFingerprint,
            binding.SnapshotId.ToString("N"),
            binding.DraftId.ToString("N"),
            binding.SourceRevision.ToString(CultureInfo.InvariantCulture),
            binding.FormatVersion.ToString(CultureInfo.InvariantCulture),
            binding.SelectionVersion.ToString(CultureInfo.InvariantCulture),
            binding.AreaGroupId.ToString("N"),
            binding.RateRowId.ToString("N"),
            binding.MethodCode,
            binding.ServiceCode,
            binding.OptionCode ?? "-",
            inputs.ReadinessSource,
            inputs.ReadinessAtUtc.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture),
            captured.MaxDeliveryDays.ToString(CultureInfo.InvariantCulture),
            captured.PreparationMinutes?.ToString(CultureInfo.InvariantCulture) ?? "-",
            captured.HandoffMinutes?.ToString(CultureInfo.InvariantCulture) ?? "-");

        return "v1:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    /// <summary>ساخت ردیف تغییرناپذیر از انتخاب بازبینی‌شده و تقویم تازه؛ انتخاب منقضی رد می‌شود.</summary>
    private async Task<OrderShipmentDispatchPlan> BuildPlanAsync(
        LocatedParcel parcel,
        Guid actorUserId,
        string occurrenceKey,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        var inputs = await ResolvePlanInputsAsync(parcel, cancellationToken);

        // A confirmation without the reviewed customer slot is a selection error, not a review-context error.
        if (ParseDate(request.DispatchPlanReceivingDate) is null
            || ParseTime(request.DispatchPlanReceivingOpensAt) is null
            || ParseTime(request.DispatchPlanReceivingClosesAt) is null)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.selection_required");
        }

        // TB-270-R3: the confirmation must echo the EXACT reviewed context. A missing fingerprint is a fresh review
        // requirement, and a changed context is refused rather than a different plan being silently confirmed.
        var reviewedFingerprint = (request.DispatchPlanReviewFingerprint ?? string.Empty).Trim();
        if (reviewedFingerprint.Length == 0)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.review_required");
        }

        if (!string.Equals(reviewedFingerprint, BuildReviewFingerprint(inputs), StringComparison.Ordinal))
        {
            throw new AdminOrderOperationsException("order.shipment_plan.review_context_changed");
        }

        var anchor = Max(_clock.UtcNow, inputs.ReadinessAtUtc);
        var options = BuildOptions(inputs, anchor);
        if (options.Count == 0)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.no_feasible_date");
        }

        var selected = ResolveSelection(options, request);
        var binding = inputs.Binding;
        var captured = inputs.Captured;
        return OrderShipmentDispatchPlan.Create(
            _ids.NewId(),
            parcel.Shipment.ShipmentId,
            parcel.Checkout.CheckoutId,
            parcel.SellerOrder.SellerOrderId,
            parcel.Fulfillment.FulfillmentId,
            binding.ShippingGroupId,
            parcel.AttemptKind,
            parcel.Shipment.ResendOfShipmentId,
            parcel.Shipment.DamagedResolutionId,
            parcel.Shipment.ReplacementReservationId,
            inputs.SourceFingerprint,
            (int)binding.OwnerKind,
            binding.OwnerTenantKey,
            binding.OwnerSellerPartyId,
            binding.MethodCode,
            binding.MethodLabel,
            binding.ServiceCode,
            binding.OptionCode,
            binding.SnapshotId,
            binding.DraftId,
            binding.SourceRevision,
            binding.FormatVersion,
            binding.SelectionVersion,
            binding.AreaGroupId,
            binding.RateRowId,
            selected.ReceivingDate,
            TimeToTicks(selected.ReceivingOpensAt),
            TimeToTicks(selected.ReceivingClosesAt),
            selected.ReceivingTimeZone,
            selected.HandoffDate,
            TimeToTicks(selected.HandoffOpensAt),
            TimeToTicks(selected.HandoffClosesAt),
            selected.HandoffTimeZone,
            selected.PreparationStartUtc,
            selected.TransferDepartureUtc,
            selected.HandoffDeadlineUtc,
            captured.PreparationMinutes!.Value,
            captured.HandoffMinutes!.Value,
            captured.MaxDeliveryDays,
            ShippingDispatchSchedulingAuthority.WindowAwarePolicyVersion,
            inputs.ReadinessSource,
            inputs.ReadinessAtUtc,
            occurrenceKey,
            actorUserId,
            _clock.UtcNow);
    }

    /// <summary>
    /// انتخاب اپراتور باید دقیقاً یکی از گزینه‌های تازه باشد؛ «نزدیک‌ترین» یا بازهٔ دیگر هرگز جایگزین نمی‌شود.
    /// </summary>
    private static AdminShipmentDispatchPlanOption ResolveSelection(
        IReadOnlyList<AdminShipmentDispatchPlanOption> options,
        AdminOrderOperationRequest request)
    {
        var date = ParseDate(request.DispatchPlanReceivingDate);
        var open = ParseTime(request.DispatchPlanReceivingOpensAt);
        var close = ParseTime(request.DispatchPlanReceivingClosesAt);
        if (date is null || open is null || close is null)
        {
            throw new AdminOrderOperationsException("order.shipment_plan.selection_required");
        }

        var match = options.FirstOrDefault(x =>
            x.ReceivingDate == date.Value
            && string.Equals(x.ReceivingOpensAt, open, StringComparison.Ordinal)
            && string.Equals(x.ReceivingClosesAt, close, StringComparison.Ordinal));
        return match
            ?? throw new AdminOrderOperationsException("order.shipment_plan.selection_expired");
    }

    private static bool SelectionsMatch(OrderShipmentDispatchPlan plan, AdminOrderOperationRequest request)
    {
        var date = ParseDate(request.DispatchPlanReceivingDate);
        var open = ParseTime(request.DispatchPlanReceivingOpensAt);
        var close = ParseTime(request.DispatchPlanReceivingClosesAt);
        return date is not null
            && open is not null
            && close is not null
            && plan.CustomerReceivingDate == date.Value
            && string.Equals(FormatTime(plan.CustomerReceivingOpenTicks), open, StringComparison.Ordinal)
            && string.Equals(FormatTime(plan.CustomerReceivingCloseTicks), close, StringComparison.Ordinal);
    }

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParseExact(
            (value ?? string.Empty).Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;

    private static string? ParseTime(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return TimeOnly.TryParseExact(
            trimmed,
            PlanStepParseFormatTime,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed.ToString(PlanStepParseFormatTime, CultureInfo.InvariantCulture)
            : null;
    }

    private static string FormatTime(long ticks)
        => TimeOnly.FromTimeSpan(TimeSpan.FromTicks(ticks))
            .ToString(PlanStepParseFormatTime, CultureInfo.InvariantCulture);

    private static string FormatTime(TimeOnly time)
        => time.ToString(PlanStepParseFormatTime, CultureInfo.InvariantCulture);

    private static long TimeToTicks(string value)
        => TimeOnly.ParseExact(value, PlanStepParseFormatTime, CultureInfo.InvariantCulture).Ticks;

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
        => left >= right ? left : right;

    private static ShippingCapturedQuoteOwnerKind ToShippingOwnerKind(ShippingGroupOwnerKind kind)
        => kind switch
        {
            ShippingGroupOwnerKind.Store => ShippingCapturedQuoteOwnerKind.Store,
            ShippingGroupOwnerKind.Marketplace => ShippingCapturedQuoteOwnerKind.Marketplace,
            _ => ShippingCapturedQuoteOwnerKind.Seller,
        };

    /// <summary>اثر انگشت دقیق خط/مقدار همین مرسوله؛ تطبیق پایدار میان Order و Fulfillment.</summary>
    public static string BuildSourceFingerprint(ShipmentSnapshot shipment)
        => OrderShipmentDispatchPlanBindingRules.BuildLineFingerprint(
            shipment.Items.Select(x => (x.OrderLineId, x.Quantity)));

    /// <summary>مقایسهٔ متعارف کدهای خدمت/گزینه: فاصلهٔ اضافی و بزرگی/کوچکی حرف معنا ندارد.</summary>
    private static bool CanonicalEquals(string? left, string? right)
        => string.Equals(
            (left ?? string.Empty).Trim(),
            (right ?? string.Empty).Trim(),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// TB-270-R3: the exact identity a plan must carry, built from the OWNING snapshot (same shape the Fulfillment
    /// guard builds from its unit), so Order and Fulfillment can never disagree about "planned".
    /// </summary>
    private static OrderShipmentDispatchPlanBindingCandidate? BuildPlanBindingCandidate(
        FulfillmentSnapshot fulfillment,
        ShipmentSnapshot shipment)
    {
        if (shipment.ShippingGroupId is not { } shippingGroupId || shippingGroupId == Guid.Empty)
        {
            return null;
        }

        return new OrderShipmentDispatchPlanBindingCandidate(
            shipment.ShipmentId,
            fulfillment.CheckoutId,
            fulfillment.SellerOrderId,
            fulfillment.FulfillmentId,
            shippingGroupId,
            shipment.IsReplacement
                ? OrderShipmentAttemptKinds.Replacement
                : OrderShipmentAttemptKinds.Resend,
            shipment.ResendOfShipmentId,
            shipment.DamagedResolutionId,
            shipment.ReplacementReservationId,
            BuildSourceFingerprint(shipment),
            shipment.ShippingMethodCode);
    }

}
