namespace Tooba.Order.Application.Admin.Detail.Models;

/// <summary>
/// خط سفارش مدیر؛ مبلغ از snapshot سفارش می‌آید و قیمت جاری Product نیست.
/// </summary>
public sealed record AdminOrderLineView(
    Guid OfferId,
    string ProductTitle,
    decimal Quantity,
    decimal UnitAmount,
    decimal LinePayable,
    string Currency,
    Guid? OrderLineId = null,
    decimal? QuantityShipped = null,
    string? ImageUrl = null,
    string? OperationalStatus = null,
    decimal? QuantityPacked = null,
    decimal? QuantityAllocated = null,
    bool? IsReturnable = null,
    int? ReturnWindowDays = null,
    string? ReturnPolicyLabel = null,
    string? ReturnDeadlineDisplay = null,
    string? ReturnRemainingDisplay = null,
    string? ReturnStatusCode = null,
    decimal? QuantityProcessing = null,
    /// <summary>TB-258: physically received usable returned quantity of this line.</summary>
    decimal? ReturnedUsableQuantity = null,
    /// <summary>TB-258: returned capacity still available for a new resend parcel.</summary>
    decimal? ResendableQuantity = null,
    /// <summary>TB-258: quantity actually delivered through delivered parcels.</summary>
    decimal? DeliveredQuantity = null);

/// <summary>خط تخصیص‌یافته در مرسولهٔ فروشنده.</summary>
/// <param name="OrderLineId">خط سفارش.</param>
/// <param name="Quantity">مقدار تخصیص‌یافته در این مرسوله.</param>
/// <param name="ResendableQuantity">
/// TB-258-R1: capacity physically received back on THIS parcel for this line and not yet resent from it.
/// </param>
/// <param name="DamagedQuantity">TB-262: damaged quantity physically received back on THIS parcel for this line.</param>
/// <param name="OutstandingDamagedQuantity">
/// TB-262: damaged quantity of THIS parcel line that still owes a durable replacement/referral decision.
/// </param>
public sealed record AdminShipmentLineView(
    Guid OrderLineId,
    decimal Quantity,
    decimal ResendableQuantity = 0,
    decimal DamagedQuantity = 0,
    decimal OutstandingDamagedQuantity = 0);

/// <summary>کارت مرسولهٔ یک فروشنده روی جزئیات سفارش مدیر.</summary>
public sealed record AdminShipmentView(
    Guid ShipmentId,
    string Status,
    string CarrierDisplayName,
    string? TrackingReference,
    decimal ItemCount,
    IReadOnlyList<AdminShipmentLineView> Lines,
    string? ShippingMethodCode = null,
    string? ShippingMethodLabel = null,
    Guid? ActivePackageId = null,
    string? ActivePackageNumber = null,
    string? PackageLockedReasonFa = null,
    bool CanAddToConsolidatedPackage = false,
    Guid? ShippingGroupId = null,
    /// <summary>
    /// TB-257: server-owned automatic tracking suggestion for this parcel (null once it already has a reference).
    /// Submitting it unchanged takes the owning automatic allocation; it is never a client-side guess.
    /// </summary>
    string? SuggestedTrackingReference = null,
    /// <summary>TB-258: the failed/returned original parcel this one resends.</summary>
    Guid? ResendOfShipmentId = null,
    /// <summary>TB-258: recorded post-dispatch non-delivery reason code.</summary>
    string? NonDeliveryReason = null,
    /// <summary>TB-258: operator note of the non-delivery attempt.</summary>
    string? NonDeliveryNote = null,
    /// <summary>TB-258: server-recorded non-delivery time.</summary>
    DateTimeOffset? NonDeliveredAt = null,
    /// <summary>TB-258: when the carrier started returning the parcel.</summary>
    DateTimeOffset? ReturnStartedAt = null,
    /// <summary>TB-258: when the physical origin receipt was confirmed.</summary>
    DateTimeOffset? ReturnedAt = null,
    /// <summary>TB-258: recorded physical origin receipt lines with their condition.</summary>
    IReadOnlyList<AdminReturnReceiptLineView>? ReturnReceiptLines = null,
    /// <summary>
    /// TB-262-R1: the durable damaged-goods decisions of THIS returned parcel, so a reloaded page can show a pending
    /// decision and resume its EXACT recorded occurrence from server-owned facts.
    /// </summary>
    IReadOnlyList<AdminDamagedResolutionView>? DamagedResolutions = null);

/// <summary>TB-262-R1: server-owned durable damaged-goods decision of one returned parcel.</summary>
public sealed record AdminDamagedResolutionView(
    Guid DamagedGoodsResolutionId,
    Guid OrderLineId,
    decimal Quantity,
    string Outcome,
    string Status,
    string OccurrenceKey,
    Guid? ReplacementShipmentId = null);

/// <summary>TB-258: one physically received line of an origin receipt.</summary>
public sealed record AdminReturnReceiptLineView(Guid OrderLineId, decimal Quantity, string Condition);

/// <summary>گروه ارسال پذیرفته‌شدهٔ مشتری روی جزئیات سفارش مدیر (فقط‌خواندنی).</summary>
public sealed record AdminAcceptedShippingGroupView(
    Guid ShippingGroupId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    string MethodCode,
    string MethodLabel,
    string? ServiceCode,
    string? OptionCode,
    decimal ShippingChargeSnapshot,
    string Currency,
    DateOnly? DeliveryDate,
    string? DeliveryTimeWindow,
    string? DeliveryTimeZone,
    string? OwnerKind,
    string? OwnerTenantKey,
    Guid? OwnerSellerPartyId,
    IReadOnlyList<Guid> OrderLineIds,
    bool CreateParcelEligible,
    string? CreateParcelBlockReasonFa,
    decimal PayableAtCheckoutAmount,
    decimal CarrierCollectAmount,
    /// <summary>Committed transport mode from Order facts (Manual / CarrierApi / LegacyIncomplete).</summary>
    string TransportMode);

/// <summary>TB-253-R2: one constituent line shown in the group review with its remaining packed quantity.</summary>
public sealed record AdminOperationalDeliveryLineView(
    Guid OrderLineId,
    string Title,
    decimal RemainingPackedQuantity,
    decimal PackedQuantity);

/// <summary>عضو یک گروه عملیاتی ارسال (ارجاع به گروه پذیرفته‌شدهٔ پایدار).</summary>
public sealed record AdminOperationalDeliveryMemberView(
    Guid ShippingGroupId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    string SellerDisplayName,
    IReadOnlyList<Guid> OrderLineIds,
    bool CreateParcelEligible,
    string? CreateParcelBlockReasonFa,
    string TransportMode,
    decimal FeeAmount,
    string Currency,
    IReadOnlyList<AdminOperationalDeliveryLineView>? Lines = null);

/// <summary>شکست هزینهٔ تعهدهای عضو در کارت عملیاتی.</summary>
public sealed record AdminOperationalFeeBreakdownView(
    Guid ShippingGroupId,
    string SellerDisplayName,
    decimal FeeAmount,
    string Currency);

/// <summary>گروه عملیاتی ارسال سفارش — projection فقط‌خواندنی روی گروه‌های پذیرفته‌شده.</summary>
public sealed record AdminOperationalDeliveryGroupView(
    string OperationalGroupKey,
    string MethodLabel,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    DateOnly? DeliveryDate,
    string? DeliveryTimeWindow,
    string? ResponsibilityLabelFa,
    bool IsCrossSellerMarketplace,
    string? SeparateReasonFa,
    bool ConsolidateEligible,
    string? ConsolidateBlockReasonFa,
    decimal CombinedFeeTotal,
    string Currency,
    IReadOnlyList<AdminOperationalDeliveryMemberView> Members,
    IReadOnlyList<AdminOperationalFeeBreakdownView> FeeBreakdown,
    /// <summary>TB-253-R2: actual actor authority for parcel/package mutations.</summary>
    bool ActorMayManageOperations = false,
    /// <summary>
    /// TB-256-R1: durable planned dispatch (carrier handoff) of this operational group, kept distinct from
    /// the customer receiving date/time. Null means no accepted group in this card carries a plan.
    /// </summary>
    AdminPlannedDispatchView? PlannedDispatch = null);

/// <summary>
/// TB-256-R1: موعد ارسال (پذیرش حامل) یک گروه عملیاتی — فقط از شاهد پذیرفته‌شده، بدون بازمحاسبه.
/// </summary>
/// <param name="HandoffDate">روز تحویل بسته به حامل (تاریخ محلی حامل).</param>
/// <param name="HandoffTimeWindow">بازهٔ پذیرش همان روز به وقت محلی حامل.</param>
/// <param name="HandoffTimeZone">منطقهٔ زمانی همان بازه.</param>
/// <param name="TransportWorkingDays">MAX روز کاری حملِ مؤثر هنگام پذیرش.</param>
/// <param name="PolicyVersion">نسخهٔ سیاست زمان‌بندیِ ثبت‌شده.</param>
/// <param name="DistinctVariants">تعداد برنامه‌های متفاوت اعضای این کارت (۱ = یک موعد مشترک).</param>
public sealed record AdminPlannedDispatchView(
    DateOnly HandoffDate,
    string HandoffTimeWindow,
    string HandoffTimeZone,
    int TransportWorkingDays,
    int PolicyVersion,
    int DistinctVariants,
    // TB-264: operational deadlines of the SAME chosen plan (UTC), null for legacy policy-v1 plans.
    DateTimeOffset? PreparationStartUtc = null,
    DateTimeOffset? TransferDepartureUtc = null,
    DateTimeOffset? HandoffDeadlineUtc = null,
    int? PreparationMinutes = null,
    int? HandoffMinutes = null);

/// <summary>عضو بسته تجمیعی روی جزئیات سفارش مدیر.</summary>
public sealed record AdminConsolidatedPackageMemberView(
    Guid ShipmentId,
    Guid SellerPartyId,
    Guid FulfillmentId,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ReleasedAt);

/// <summary>کارت بسته تجمیعی مرکزی روی جزئیات سفارش مدیر.</summary>
public sealed record AdminConsolidatedPackageView(
    Guid ConsolidatedPackageId,
    string PackageNumber,
    string Status,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    string? TrackingReference,
    string? Note,
    int SellerCount,
    int MemberShipmentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt,
    IReadOnlyList<AdminConsolidatedPackageMemberView> Members);

/// <summary>برش سفارش یک فروشنده در جزئیات Checkout مدیر.</summary>
public sealed record AdminSellerOrderView(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    string PaymentState,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<AdminOrderLineView> Lines,
    Guid? FulfillmentId = null,
    string? FulfillmentStatus = null,
    IReadOnlyList<AdminShipmentView>? Shipments = null);

/// <summary>برش مالی یک فروشنده در جزئیات سفارش مدیر.</summary>
public sealed record AdminSellerFinancialView(
    Guid SellerOrderId,
    Guid SellerPartyId,
    string SellerDisplayName,
    int LineCount,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal PayableAmount,
    string Currency,
    string SettlementStatus);

/// <summary>رویداد مالی در تاریخچهٔ checkout مدیر.</summary>
public sealed record AdminFinancialEventView(
    DateTimeOffset OccurredAt,
    string EventType,
    decimal Amount,
    string Currency,
    string PartyDisplayName,
    string Reference,
    string PaymentMethod,
    string Status,
    string Description);

/// <summary>جمع‌بندی مالی checkout برای تب خلاصهٔ مالی.</summary>
public sealed record AdminFinancialSummaryView(
    decimal TotalSellerShare,
    decimal TotalCommission,
    decimal GrossOrderProfit,
    decimal PayableToSellers,
    decimal CustomerGrossAmount,
    decimal ShippingCost,
    decimal CustomerDiscounts,
    decimal TotalReceivedFromCustomer,
    string Currency);

/// <summary>جزئیات عملیاتی سفارش با snapshot گیرنده و ارسال؛ راز پرداختی در آن وجود ندارد.</summary>
public sealed record AdminOrderDetailPage(
    Guid CheckoutId,
    string Reference,
    DateTimeOffset SubmittedAt,
    string Status,
    string PaymentState,
    int LineCount,
    int SellerCount,
    decimal Subtotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string Currency,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string ShippingMethodLabel,
    IReadOnlyList<AdminSellerOrderView> SellerOrders,
    IReadOnlyList<AdminSellerFinancialView> SellerFinancials,
    IReadOnlyList<AdminFinancialEventView> FinancialEvents,
    AdminFinancialSummaryView FinancialSummary,
    AdminPaymentOpsView? Payment = null,
    IReadOnlyList<AdminConsolidatedPackageView>? ConsolidatedPackages = null,
    AdminReservationCycleAuditView? ReservationCycle = null,
    IReadOnlyList<AdminAcceptedShippingGroupView>? ShippingGroups = null,
    IReadOnlyList<AdminOperationalDeliveryGroupView>? OperationalDeliveryGroups = null);

/// <summary>بازرسی عملیاتی پرداخت روی جزئیات سفارش مدیر؛ راز یا payload خام ندارد.</summary>
public sealed record AdminPaymentOpsView(
    Guid PaymentId,
    Guid CheckoutId,
    string Status,
    decimal Amount,
    string Currency,
    string ProviderCode,
    string? ProviderRequestReference,
    string? ProviderTransactionReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    string? LastFailureCode,
    bool ReconcileEligible,
    bool ConfirmDepositEligible = false,
    bool RejectDepositEligible = false,
    string? CustomerTransferReference = null,
    Guid? ProofMediaAssetId = null,
    DateTimeOffset? EvidenceSubmittedAt = null,
    string ReservationLabel = "—",
    string ReservationLabelEn = "—",
    string ReservationState = "none",
    int? ReservationCycleNumber = null,
    bool ReservationRetryPossible = false,
    bool ReservationNeedsReacquire = false,
    bool ReservationRetryLimitReached = false);

/// <summary>کمبود خط برای شکست بازتملک.</summary>
public sealed record AdminReservationShortageLine(
    string ItemTitle,
    decimal Required,
    decimal Available,
    decimal Shortage,
    string? UnitCode);

/// <summary>رویداد ممیزی قابل نمایش.</summary>
public sealed record AdminReservationCycleEventView(
    string KindLabelFa,
    string KindLabelEn,
    DateTimeOffset OccurredAt,
    int? CycleNumber,
    string DetailFa,
    string DetailEn);

/// <summary>ردیف تاریخچهٔ immutable یک چرخه.</summary>
public sealed record AdminReservationCycleHistoryRow(
    int CycleNumber,
    string StatusLabelFa,
    string StatusLabelEn,
    string ReasonLabelFa,
    string ReasonLabelEn,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? EndedAt,
    int EffectiveHoldMinutes,
    int EffectiveMaxCycles,
    string PolicySource,
    string PolicySourceLabelFa,
    string PolicySourceLabelEn,
    string? PaymentAttemptRef);

/// <summary>ممیزی رزرو موجودی روی جزئیات سفارش.</summary>
public sealed record AdminReservationCycleAuditView(
    string StatusLabelFa,
    string StatusLabelEn,
    string ReasonLabelFa,
    string ReasonLabelEn,
    int? CurrentCycleNumber,
    int TotalCyclesUsed,
    int MaxCycles,
    int RetryCountRemaining,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset ServerTime,
    int SecondsRemaining,
    string SupplyStatus,
    string SupplyStatusLabelFa,
    bool RetryLimitReached,
    bool CanRetryReservation,
    bool CanExtendTimer,
    IReadOnlyList<AdminReservationCycleHistoryRow> History,
    IReadOnlyList<AdminReservationCycleEventView> Events,
    IReadOnlyList<AdminReservationShortageLine> Shortages);
