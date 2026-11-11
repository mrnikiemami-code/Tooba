using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>
/// TB-262: the durable, source-scoped operator decision for physically DAMAGED goods of one origin receipt line.
///
/// It links the paid order line, the immediate damaged source parcel, the exact receipt, the decided quantity, the
/// reviewing actor/time and — for a replacement — the NEW purpose-bound healthy-stock reservation and the linked
/// replacement parcel. Damaged units never become usable resend capacity and never raise saleable stock: the original
/// sale already consumed them, so this decision moves NO existing stock by itself.
///
/// A replacement is funded by its OWN new reservation, never by returned capacity and never by the original consumed
/// sale reservation. One damaged unit can therefore be allocated to exactly ONE decision at a time.
/// </summary>
public sealed class DamagedGoodsResolution
{
    private DamagedGoodsResolution()
    {
    }

    /// <summary>شناسه پایدار تصمیم.</summary>
    public Guid DamagedGoodsResolutionId { get; init; }

    /// <summary>واحد مالک.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>مرسولهٔ آسیب‌دیدهٔ مبدأ که کالای معیوب از آن برگشت.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>رسید فیزیکی مبدأ که مقدار آسیب‌دیده در آن ثبت شده است.</summary>
    public Guid ShipmentReturnReceiptId { get; init; }

    /// <summary>خط سفارش.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>مقدار آسیب‌دیدهٔ تعیین‌تکلیف‌شده.</summary>
    public decimal Quantity { get; init; }

    /// <summary>تصمیم اپراتور.</summary>
    public DamagedResolutionOutcome Outcome { get; private set; }

    /// <summary>وضعیت پایدار تصمیم.</summary>
    public DamagedResolutionStatus Status { get; private set; }

    /// <summary>رزرو موجودی سالمِ اختصاصی جایگزینی (فقط Replacement).</summary>
    public Guid? ReplacementReservationId { get; private set; }

    /// <summary>مرسولهٔ جایگزینِ ساخته‌شده (فقط Replacement).</summary>
    public Guid? ReplacementShipmentId { get; private set; }

    /// <summary>کلید تکرارناپذیری رخداد تصمیم.</summary>
    public string OccurrenceKey { get; init; } = string.Empty;

    /// <summary>اپراتور تصمیم‌گیرنده (سمت سرور).</summary>
    public Guid DecidedByUserId { get; init; }

    /// <summary>
    /// تلاش رزرو جایگزینی. با هر ابطال/آزادسازی یک واحد افزایش می‌یابد تا کلید تکرارناپذیری رزرو تازه باشد و
    /// رزرو آزادشدهٔ قبلی هرگز به‌صورت کورکورانه بازاستفاده نشود.
    /// </summary>
    public int Attempt { get; private set; }

    /// <summary>یادداشت اختیاری اپراتور.</summary>
    public string? Note { get; init; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین تغییر.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    internal static DamagedGoodsResolution Record(
        Guid resolutionId,
        Guid fulfillmentId,
        Guid shipmentId,
        Guid shipmentReturnReceiptId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome,
        string occurrenceKey,
        Guid decidedByUserId,
        string? note,
        DateTimeOffset now)
    {
        if (resolutionId == Guid.Empty || fulfillmentId == Guid.Empty || shipmentId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_ids_required");
        }

        if (shipmentReturnReceiptId == Guid.Empty || orderLineId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_ids_required");
        }

        if (quantity <= 0m)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_qty_positive");
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new ContractOperationException("fulfillment.damage.resolution_outcome_invalid");
        }

        if (decidedByUserId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_actor_required");
        }

        if (string.IsNullOrWhiteSpace(occurrenceKey) || occurrenceKey.Trim().Length > 128)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_occurrence_required");
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote is { Length: > 512 })
        {
            throw new ContractOperationException("fulfillment.damage.resolution_note_invalid");
        }

        return new DamagedGoodsResolution
        {
            DamagedGoodsResolutionId = resolutionId,
            FulfillmentId = fulfillmentId,
            ShipmentId = shipmentId,
            ShipmentReturnReceiptId = shipmentReturnReceiptId,
            OrderLineId = orderLineId,
            Quantity = quantity,
            Outcome = outcome,
            Status = DamagedResolutionStatus.Recorded,
            OccurrenceKey = occurrenceKey.Trim(),
            DecidedByUserId = decidedByUserId,
            Attempt = 1,
            Note = trimmedNote,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// کلید تکرارناپذیری رزرو سالم این تصمیم.
    /// <para>
    /// RETENTION RULE: the key is STABLE across attempts. The hold belongs to the active ORDER, so cancelling a
    /// replacement parcel or retrying the decision must reuse this exact allocation instead of reserving a second unit.
    /// </para>
    /// </summary>
    public string ReservationKey => $"damage-replacement:{DamagedGoodsResolutionId:N}";

    /// <summary>کلید پایدار مصرف‌کنندهٔ رزرو جایگزینی در زمان ارسال.</summary>
    public string ReservationConsumerKey => $"damage-replacement:{DamagedGoodsResolutionId:N}";

    /// <summary>آیا این تصمیم هنوز به موجودی سالم نیاز دارد؟</summary>
    public bool AwaitsHealthyStock =>
        Outcome == DamagedResolutionOutcome.Replacement
        && Status == DamagedResolutionStatus.Recorded
        && ReplacementReservationId is null;

    /// <summary>تصمیم، رزرو سالم اختصاصی خود را دریافت کرد.</summary>
    internal void AttachReservation(Guid reservationId, DateTimeOffset now)
    {
        if (Outcome != DamagedResolutionOutcome.Replacement)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_not_replacement");
        }

        if (Status is DamagedResolutionStatus.Cancelled or DamagedResolutionStatus.Fulfilled)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_not_open");
        }

        if (reservationId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.damage.reservation_required");
        }

        if (ReplacementReservationId == reservationId)
        {
            UpdatedAt = now;
            return;
        }

        if (ReplacementReservationId is not null)
        {
            throw new ContractOperationException("fulfillment.damage.reservation_conflict");
        }

        ReplacementReservationId = reservationId;
        Status = DamagedResolutionStatus.Reserved;
        UpdatedAt = now;
    }

    /// <summary>مرسولهٔ جایگزینِ مرتبط ساخته شد.</summary>
    internal void MarkFulfilled(Guid shipmentId, DateTimeOffset now)
    {
        if (Outcome != DamagedResolutionOutcome.Replacement)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_not_replacement");
        }

        if (Status == DamagedResolutionStatus.Cancelled)
        {
            throw new ContractOperationException("fulfillment.damage.resolution_cancelled");
        }

        if (ReplacementShipmentId is { } existing)
        {
            if (existing != shipmentId)
            {
                throw new ContractOperationException("fulfillment.damage.replacement_conflict");
            }

            UpdatedAt = now;
            return;
        }

        ReplacementShipmentId = shipmentId;
        Status = DamagedResolutionStatus.Fulfilled;
        UpdatedAt = now;
    }

    /// <summary>
    /// ابطال مرسولهٔ جایگزین پیش از ارسال: فقط تخصیص همین مرسوله برداشته می‌شود.
    /// <para>
    /// RETENTION RULE (explicit user rule «تا سفارش لغو نشده نباید آزاد بشه رزرو»): رزرو موجودی سالم به سفارش فعال
    /// گره خورده و آزاد نمی‌شود. تصمیم صرفاً مرسولهٔ باطل‌شده را رها می‌کند و تلاش بعدی همان رزرو را بازمی‌بندد.
    /// واحدهای آسیب‌دیده غیرقابل‌فروش و تعهد مشتری پابرجا می‌ماند.
    /// </para>
    /// </summary>
    /// <param name="cancelledShipmentId">The parcel whose cancellation was requested (never any other parcel).</param>
    internal void RetainStockBeforeDispatch(Guid cancelledShipmentId, DateTimeOffset now)
    {
        if (Status == DamagedResolutionStatus.Cancelled || cancelledShipmentId == Guid.Empty)
        {
            return;
        }

        // EXACT association: only the CURRENT live linked parcel may detach its allocation. A replayed cancel of an
        // already cancelled parcel (or of an OLD parcel that a later retry replaced) is a no-op — it must never erase
        // the NEW link, advance the attempt or free the held stock.
        if (ReplacementShipmentId is not { } current || current != cancelledShipmentId)
        {
            return;
        }

        Attempt += 1;
        ReplacementShipmentId = null;
        Status = ReplacementReservationId is null
            ? DamagedResolutionStatus.Recorded
            : DamagedResolutionStatus.Reserved;
        UpdatedAt = now;
    }

    /// <summary>
    /// TB-262-R1: does this decision still own open replacement work (durable decision without a live parcel)?
    /// It is the server-owned fact that lets an operator resume the EXACT recorded decision after a reload.
    /// </summary>
    public bool AwaitsReplacementParcel =>
        Outcome == DamagedResolutionOutcome.Replacement
        && Status is DamagedResolutionStatus.Recorded or DamagedResolutionStatus.Reserved
        && ReplacementShipmentId is null;

    /// <summary>
    /// لغو مجاز و مالکانهٔ خود سفارش: تنها همین رخداد می‌تواند رزرو سالمِ مصرف‌نشدهٔ این تصمیم را یک‌بار آزاد کند.
    /// مقدار آسیب‌دیده همچنان غیرقابل‌فروش می‌ماند و تاریخچهٔ تصمیم پاک نمی‌شود.
    /// </summary>
    internal void MarkOrderCancelled(DateTimeOffset now)
    {
        ReplacementShipmentId = null;
        ReplacementReservationId = null;
        Status = DamagedResolutionStatus.Cancelled;
        UpdatedAt = now;
    }

    /// <summary>آیا این تصمیم همان intent بازبینی‌شده است؟</summary>
    internal bool AnswersIntent(
        Guid shipmentId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome) =>
        ShipmentId == shipmentId
        && OrderLineId == orderLineId
        && Quantity == quantity
        && Outcome == outcome;
}
