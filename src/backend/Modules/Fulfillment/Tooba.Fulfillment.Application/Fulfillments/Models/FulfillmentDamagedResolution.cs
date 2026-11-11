using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Fulfillments.Models;

/// <summary>
/// TB-262: answer of one damaged-goods decision command, as the owning directory reports it.
/// </summary>
/// <param name="Snapshot">The owning unit after the decision (unchanged when only a blocker was answered).</param>
/// <param name="DamagedGoodsResolutionId">The durable decision identity.</param>
/// <param name="Status">Durable state of the decision.</param>
/// <param name="ReplacementReservationId">The replacement's OWN healthy-stock reservation, when one is linked.</param>
/// <param name="ReplacementShipmentId">The linked replacement parcel, when one exists.</param>
/// <param name="BlockerCode">
/// A precise typed blocker when the decision is durably recorded but its replacement cannot be created right now
/// (for example no healthy stock of the same offer). The decision is never rolled back and no hold is leaked.
/// </param>
public sealed record FulfillmentDamagedResolution(
    FulfillmentSnapshot Snapshot,
    Guid DamagedGoodsResolutionId,
    DamagedResolutionStatus Status,
    Guid? ReplacementReservationId = null,
    Guid? ReplacementShipmentId = null,
    string? BlockerCode = null);

/// <summary>TB-262: READ-ONLY answer of one exact reviewed damaged-goods decision lookup.</summary>
public sealed record FulfillmentDamagedIntentRecord(
    int State,
    FulfillmentSnapshot? Snapshot = null,
    DamagedGoodsResolutionRecord? Resolution = null,
    string? ConflictCode = null)
{
    /// <summary>No durable decision answers this exact intent.</summary>
    public const int NotRecorded = 0;

    /// <summary>The exact reviewed decision is recorded.</summary>
    public const int Recorded = 1;

    /// <summary>A decision exists under this occurrence but answers a different intent.</summary>
    public const int Conflict = 2;

    /// <summary>The exact recorded decision was cancelled before dispatch and must not be recreated.</summary>
    public const int ResolvedCancelled = 3;
}

/// <summary>TB-262: the recorded decision itself, as the read seam reports it.</summary>
public sealed record DamagedGoodsResolutionRecord(
    Guid DamagedGoodsResolutionId,
    Guid ShipmentId,
    Guid ShipmentReturnReceiptId,
    Guid OrderLineId,
    decimal Quantity,
    DamagedResolutionOutcome Outcome,
    DamagedResolutionStatus Status,
    Guid DecidedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? ReplacementReservationId,
    Guid? ReplacementShipmentId,
    string? Note);
