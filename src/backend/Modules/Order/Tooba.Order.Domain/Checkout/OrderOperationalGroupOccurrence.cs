namespace Tooba.Order.Domain.Checkout;

/// <summary>
/// TB-253-R4: one durable, immutable Marketplace operational-group workflow occurrence.
/// </summary>
/// <remarks>
/// <para>
/// The row is claimed BEFORE the first parcel allocation and carries the exact reviewed request:
/// actor/owner scope, checkout, operational member set, per-line quantities, explicit reuse parcel
/// ids and the accepted shipping method identity. A retry/remount replays this envelope; a different
/// envelope under the same occurrence is a typed intent conflict rather than a silent re-allocation.
/// </para>
/// <para>
/// Occurrence identity is deliberately separate from any payload fingerprint, so a later deliberate
/// operation with identical quantities is a NEW occurrence with its own parcels.
/// </para>
/// </remarks>
public sealed class OrderOperationalGroupOccurrence
{
    /// <summary>Envelope durably recorded; no allocation written yet.</summary>
    public const string ClaimedState = "Claimed";

    /// <summary>At least one constituent allocation recorded.</summary>
    public const string InProgressState = "InProgress";

    /// <summary>Common package recorded; replay reads this exact result.</summary>
    public const string CompletedState = "Completed";

    /// <summary>سازندهٔ EF.</summary>
    private OrderOperationalGroupOccurrence()
    {
    }

    /// <summary>Workflow occurrence identity chosen by the caller before the first mutation.</summary>
    public Guid OccurrenceId { get; init; }

    /// <summary>Owner checkout of the whole operation.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>Actor/owner scope that claimed the occurrence.</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>Server-projected operational group identity this operation belongs to.</summary>
    public string OperationalGroupKey { get; private set; } = string.Empty;

    /// <summary>Canonical full-envelope fingerprint (immutable once claimed).</summary>
    public string EnvelopeFingerprint { get; private set; } = string.Empty;

    /// <summary>Canonical full-envelope JSON — exact reviewed request, never a payload hash alone.</summary>
    public string EnvelopeJson { get; private set; } = string.Empty;

    /// <summary>Lifecycle state; a completed occurrence is read-only.</summary>
    public string State { get; private set; } = ClaimedState;

    /// <summary>Common package id once recorded.</summary>
    public Guid? ConsolidatedPackageId { get; private set; }

    /// <summary>Common package number once recorded.</summary>
    public string? PackageNumber { get; private set; }

    /// <summary>Accepted shipping method identity used by this occurrence.</summary>
    public string? ShippingMethodCode { get; private set; }

    /// <summary>Claim time (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Last durable progress (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Creates the claim row for one exact envelope.</summary>
    public static OrderOperationalGroupOccurrence Claim(
        Guid occurrenceId,
        Guid checkoutId,
        Guid actorUserId,
        string operationalGroupKey,
        string envelopeFingerprint,
        string envelopeJson,
        string shippingMethodCode,
        DateTimeOffset now) =>
        new()
        {
            OccurrenceId = occurrenceId,
            CheckoutId = checkoutId,
            ActorUserId = actorUserId,
            OperationalGroupKey = operationalGroupKey,
            EnvelopeFingerprint = envelopeFingerprint,
            EnvelopeJson = envelopeJson,
            ShippingMethodCode = shippingMethodCode,
            State = ClaimedState,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

    /// <summary>Marks durable constituent progress (idempotent).</summary>
    public void MarkInProgress(DateTimeOffset now)
    {
        if (State == CompletedState)
        {
            return;
        }

        State = InProgressState;
        UpdatedAtUtc = now;
    }

    /// <summary>
    /// TB-259: records a completed SEPARATE packing occurrence. No package id/number exists, and that
    /// absence is the recorded result of this completion shape — not pending work.
    /// </summary>
    public void CompleteSeparate(string shippingMethodCode, DateTimeOffset now)
    {
        if (State == CompletedState)
        {
            return;
        }

        ConsolidatedPackageId = null;
        PackageNumber = null;
        ShippingMethodCode = shippingMethodCode;
        State = CompletedState;
        UpdatedAtUtc = now;
    }

    /// <summary>
    /// Records the common package result once; a replayed result is never overwritten.
    /// </summary>
    public void Complete(
        Guid consolidatedPackageId,
        string packageNumber,
        string shippingMethodCode,
        DateTimeOffset now)
    {
        if (State == CompletedState)
        {
            return;
        }

        ConsolidatedPackageId = consolidatedPackageId;
        PackageNumber = packageNumber;
        ShippingMethodCode = shippingMethodCode;
        State = CompletedState;
        UpdatedAtUtc = now;
    }
}

/// <summary>
/// TB-253-R4: exact constituent allocation recorded for one member of one workflow occurrence.
/// </summary>
/// <remarks>
/// The row is written immediately after that member's allocation is durably created, so a crash
/// between members keeps the exact parcel identity of the member already written. Retrying the same
/// occurrence reads THIS link instead of sweeping open parcels by quantity.
/// </remarks>
public sealed class OrderOperationalGroupOccurrenceMember
{
    /// <summary>سازندهٔ EF.</summary>
    private OrderOperationalGroupOccurrenceMember()
    {
    }

    /// <summary>Occurrence owner.</summary>
    public Guid OccurrenceId { get; init; }

    /// <summary>Constituent accepted shipping group.</summary>
    public Guid ShippingGroupId { get; init; }

    /// <summary>JSON array of newly created shipment ids (empty for reuse-only).</summary>
    public string ShipmentIdsJson { get; private set; } = "[]";

    /// <summary>JSON array of explicitly reused shipment ids.</summary>
    public string ReusedShipmentIdsJson { get; private set; } = "[]";

    /// <summary>True when this member allocated a new parcel.</summary>
    public bool Created { get; private set; }

    /// <summary>Record time (UTC).</summary>
    public DateTimeOffset RecordedAtUtc { get; private set; }

    /// <summary>Creates one exact member allocation link.</summary>
    public static OrderOperationalGroupOccurrenceMember Record(
        Guid occurrenceId,
        Guid shippingGroupId,
        string shipmentIdsJson,
        string reusedShipmentIdsJson,
        bool created,
        DateTimeOffset now) =>
        new()
        {
            OccurrenceId = occurrenceId,
            ShippingGroupId = shippingGroupId,
            ShipmentIdsJson = shipmentIdsJson,
            ReusedShipmentIdsJson = reusedShipmentIdsJson,
            Created = created,
            RecordedAtUtc = now,
        };
}
