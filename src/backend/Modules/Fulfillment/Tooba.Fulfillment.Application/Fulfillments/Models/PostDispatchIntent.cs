namespace Tooba.Fulfillment.Application.Fulfillments.Models;

/// <summary>
/// TB-258-R2: which reviewed post-dispatch intent the read-only reconcile lookup is asked about.
/// </summary>
public enum PostDispatchIntentKind
{
    /// <summary>A recorded courier non-delivery attempt.</summary>
    RecordNonDelivery = 0,

    /// <summary>A recorded physical return to origin.</summary>
    StartReturn = 1,

    /// <summary>A recorded physical origin receipt.</summary>
    RecordReturnReceipt = 2,

    /// <summary>A recorded resend allocation occurrence.</summary>
    ResendShipment = 3,
}

/// <summary>
/// TB-258-R2: the exact reviewed post-dispatch intent, expressed with owning application models so the directory
/// never depends on a transport contract.
/// </summary>
public sealed record PostDispatchIntent(
    PostDispatchIntentKind Kind,
    Guid FulfillmentId,
    Guid ShipmentId,
    string? Reason = null,
    string? Note = null,
    IReadOnlyList<ReturnReceiptLineCommand>? ReceiptItems = null,
    IReadOnlyList<ShipmentLineCommand>? ResendItems = null,
    string? OccurrenceKey = null)
{
    /// <summary>A non-delivery attempt with its operator reason/note.</summary>
    public static PostDispatchIntent NonDelivery(
        Guid fulfillmentId,
        Guid shipmentId,
        string? reason,
        string? note) =>
        new(PostDispatchIntentKind.RecordNonDelivery, fulfillmentId, shipmentId, reason, note);

    /// <summary>The carrier starting the physical return.</summary>
    public static PostDispatchIntent ReturnStart(Guid fulfillmentId, Guid shipmentId) =>
        new(PostDispatchIntentKind.StartReturn, fulfillmentId, shipmentId);

    /// <summary>The physical origin receipt with its exact per-line quantities and conditions.</summary>
    public static PostDispatchIntent Receipt(
        Guid fulfillmentId,
        Guid shipmentId,
        IReadOnlyList<ReturnReceiptLineCommand> items) =>
        new(PostDispatchIntentKind.RecordReturnReceipt, fulfillmentId, shipmentId, ReceiptItems: items);

    /// <summary>A resend occurrence key with its exact per-line quantities.</summary>
    public static PostDispatchIntent Resend(
        Guid fulfillmentId,
        Guid shipmentId,
        string? occurrenceKey,
        IReadOnlyList<ShipmentLineCommand> items) =>
        new(
            PostDispatchIntentKind.ResendShipment,
            fulfillmentId,
            shipmentId,
            ResendItems: items,
            OccurrenceKey: occurrenceKey);
}

/// <summary>
/// TB-258-R2: read-only recorded-intent answer of the owning directory. It is evidence about durable facts only;
/// the caller keeps every authorization and projection decision.
/// </summary>
public sealed record PostDispatchIntentRecord(
    int State,
    FulfillmentSnapshot? Snapshot = null,
    Guid? RecordedShipmentId = null,
    string? ConflictCode = null)
{
    /// <summary>No durable fact answers this exact intent.</summary>
    public const int NotRecorded = 0;

    /// <summary>The exact intent is already recorded.</summary>
    public const int Recorded = 1;

    /// <summary>A durable fact answers a different intent.</summary>
    public const int Conflict = 2;

    /// <summary>The recorded occurrence was cancelled and must not be recreated.</summary>
    public const int ResolvedCancelled = 3;
}
