using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-279: one entry of the append-only decision history of a case. The legacy decision row (when it exists) is the
/// baseline version 0 and stays immutable; every later decision or correction is its own versioned fact.
/// </summary>
public sealed record ReturnDecisionRevisionSnapshot(
    int Version,
    string Decision,
    string? Reason,
    Guid ActorUserId,
    DateTimeOffset RecordedAt);

/// <summary>
/// TB-279: the reviewed resolution of ONE requested line. <see cref="Accepted"/> is the refundable quantity,
/// <see cref="Rejected"/> the withdrawn/declined quantity, and the two may never name more than the original claim or
/// accept more than what was physically received.
/// </summary>
public sealed record ReturnResolutionLineSnapshot(
    Guid ReturnItemId,
    decimal Requested,
    decimal Received,
    decimal Usable,
    decimal Damaged,
    decimal Accepted,
    decimal Rejected,
    string? Reason,
    int Version)
{
    /// <summary>Accepted usable portion — the ONLY quantity a saleable restock may use.</summary>
    public decimal AcceptedUsable => Math.Min(Accepted, Usable);

    /// <summary>Accepted portion that was inspected damaged (never saleable).</summary>
    public decimal AcceptedDamaged => Math.Max(0m, Math.Min(Accepted, Usable + Damaged) - AcceptedUsable);

    /// <summary>Rejected received usable portion: customer-owned custody, never restocked.</summary>
    public decimal RejectedUsable => Math.Max(0m, Usable - AcceptedUsable);

    /// <summary>Rejected received damaged portion: customer-owned custody, never saleable.</summary>
    public decimal RejectedDamaged => Math.Max(0m, Damaged - AcceptedDamaged);

    /// <summary>Rejected received quantity of both conditions.</summary>
    public decimal RejectedReceived => RejectedUsable + RejectedDamaged;
}

/// <summary>
/// TB-279: the effective reviewed resolution plan of a case. <see cref="IsComplete"/> is a POSITIVE fact: every
/// requested line carries a deliberate accepted + rejected split that accounts for the whole original claim.
/// <see cref="ApprovedAmount"/> is derived from the accepted quantities and each line's STORED unit price only.
/// </summary>
public sealed record ReturnResolutionSnapshot(
    int Version,
    bool IsComplete,
    decimal ApprovedAmount,
    bool FromReviewedPlan,
    IReadOnlyList<ReturnResolutionLineSnapshot> Lines,
    string? BlockerCode = null);

/// <summary>TB-279: one recorded ACTUAL handback of rejected received goods to the owning customer.</summary>
public sealed record ReturnCustodySnapshot(
    Guid ReturnCustodyId,
    Guid ReturnItemId,
    decimal Quantity,
    ReturnItemCondition Condition,
    Guid RecipientUserId,
    string ProofReference,
    DateTimeOffset RecordedAt);

/// <summary>
/// TB-279: whether this case may lawfully be corrected, and the exact typed reason when it may not. The correction is
/// only ever available BEFORE a real effect: an audited reopen cannot undo a recorded refund, a restock or a handback.
/// </summary>
public sealed record ReturnCorrectionAssessment(bool CanReopenForReview, string? BlockerCode);

/// <summary>
/// TB-279: the ONE resolution authority. It decides what the approved money of a case is from stored facts only, so a
/// partial plan can never refund the original claim by accident and a browser amount is never an input.
/// </summary>
public static class ReturnResolutionPolicy
{
    /// <summary>The approved amount of a line at the persisted money scale (numeric(18,4)).</summary>
    public static decimal MoneyOf(decimal unitPrice, decimal quantity) =>
        Math.Round(unitPrice * quantity, 4, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Describes the effective resolution plan.
    /// <para>
    ///  - A courier-origin case derives its money from the Fulfillment-owned source claim, so it is complete by
    ///    construction and keeps the original amount.
    ///  - A stored reviewed plan (the highest version, one row per requested item) must account for the whole claim
    ///    exactly, and its accepted quantities may not exceed what was physically received.
    ///  - With no stored plan, complete receiving coverage is the only unambiguous baseline: every requested unit
    ///    arrived, so every unit is accepted. Anything less is incomplete and names the exact next step.
    /// </para>
    /// </summary>
    public static ReturnResolutionSnapshot Describe(
        ReturnOrigin origin,
        IReadOnlyList<ReturnItemSnapshot> items,
        ReturnReceivingSummary? receiving,
        IReadOnlyList<ReturnResolutionLineSnapshot> storedLines,
        int storedVersion,
        decimal originalAmount)
    {
        if (origin == ReturnOrigin.CourierOrigin)
        {
            return new ReturnResolutionSnapshot(0, true, originalAmount, false, []);
        }

        if (items.Count == 0)
        {
            return new ReturnResolutionSnapshot(0, false, 0m, false, [], ReturnsErrorCodes.ResolutionRequired);
        }

        var receivingById = receiving?.Lines.ToDictionary(x => x.ReturnItemId, x => x)
            ?? new Dictionary<Guid, ReturnReceivingLine>();

        if (storedLines.Count > 0)
        {
            if (storedLines.Count != items.Count
                || items.Any(item => storedLines.All(line => line.ReturnItemId != item.ReturnItemId)))
            {
                // A plan that resolves only some lines is not a reviewed whole-case decision.
                return new ReturnResolutionSnapshot(storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionIncomplete);
            }

            var lines = new List<ReturnResolutionLineSnapshot>(items.Count);
            var approved = 0m;
            foreach (var item in items)
            {
                var stored = storedLines.Single(x => x.ReturnItemId == item.ReturnItemId);
                receivingById.TryGetValue(item.ReturnItemId, out var received);
                var requested = item.Quantity;
                var receivedQuantity = received?.Received ?? stored.Received;
                var usable = received?.Usable ?? stored.Usable;
                var damaged = received?.Damaged ?? stored.Damaged;

                if (stored.Accepted < 0m || stored.Rejected < 0m
                    || !PersistedReturnQuantity.IsExactlyRepresentable(stored.Accepted)
                    || !PersistedReturnQuantity.IsExactlyRepresentable(stored.Rejected))
                {
                    return new ReturnResolutionSnapshot(storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionInvalid);
                }

                if (stored.Accepted + stored.Rejected != requested)
                {
                    // The whole original claim must be accounted for; a remainder is never silently refunded or kept.
                    return new ReturnResolutionSnapshot(storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionIncomplete);
                }

                if (stored.Accepted > receivedQuantity)
                {
                    // An accepted quantity must have coherent physical evidence; an unreceived unit is never accepted.
                    return new ReturnResolutionSnapshot(storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionReceivedExceeded);
                }

                approved += MoneyOf(item.UnitPriceSnapshot, stored.Accepted);
                lines.Add(new ReturnResolutionLineSnapshot(
                    item.ReturnItemId,
                    requested,
                    receivedQuantity,
                    usable,
                    damaged,
                    stored.Accepted,
                    stored.Rejected,
                    stored.Reason,
                    stored.Version));
            }

            return new ReturnResolutionSnapshot(storedVersion, true, Math.Round(approved, 4, MidpointRounding.AwayFromZero), true, lines);
        }

        if (receiving is not { IsCoherent: true, IsComplete: true })
        {
            // No reviewed plan and no complete receiving coverage: the case cannot name a lawful approved amount yet.
            return new ReturnResolutionSnapshot(
                0,
                false,
                0m,
                false,
                [],
                receiving is { IsCoherent: false }
                    ? ReturnsErrorCodes.RecoveryEvidenceConflict
                    : ReturnsErrorCodes.ResolutionRequired);
        }

        // Complete coverage with no counter-evidence: every requested unit was received, so every unit is accepted.
        var baselineLines = new List<ReturnResolutionLineSnapshot>(items.Count);
        var baselineApproved = 0m;
        foreach (var item in items)
        {
            receivingById.TryGetValue(item.ReturnItemId, out var received);
            var requested = item.Quantity;
            if (received is null || received.Received != requested)
            {
                return new ReturnResolutionSnapshot(0, false, 0m, false, [], ReturnsErrorCodes.ResolutionRequired);
            }

            baselineApproved += MoneyOf(item.UnitPriceSnapshot, requested);
            baselineLines.Add(new ReturnResolutionLineSnapshot(
                item.ReturnItemId,
                requested,
                received.Received,
                received.Usable,
                received.Damaged,
                requested,
                0m,
                null,
                0));
        }

        return new ReturnResolutionSnapshot(0, true, Math.Round(baselineApproved, 4, MidpointRounding.AwayFromZero), false, baselineLines);
    }

    /// <summary>
    /// The EFFECTIVE resolution of one stored case: the described plan when the caller already computed it, else the
    /// plan derived from the SAME stored facts. A hand-built snapshot therefore answers identically to a mapped one,
    /// and no caller can invent an approved amount.
    /// </summary>
    public static ReturnResolutionSnapshot Resolve(ReturnSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.Resolution
            ?? Describe(
                snapshot.Origin,
                snapshot.Items,
                snapshot.Receiving,
                [],
                0,
                snapshot.RefundAmount);
    }

    /// <summary>
    /// True when every accepted portion carries the durable physical fact it must (a usable restock or a non-saleable
    /// damaged record) and every rejected received portion was really handed back. Money-complete work is never
    /// reported as settled while received goods are still owed to the owning customer.
    /// </summary>
    public static bool IsPhysicalComplete(ReturnSnapshot snapshot)
    {
        if (snapshot.Origin == ReturnOrigin.CourierOrigin)
        {
            // Courier-origin cases keep their existing owning closure semantics.
            return snapshot.Items.Count > 0
                && snapshot.Items.All(x => x.Disposition != ReturnItemDisposition.None);
        }

        var resolution = Resolve(snapshot);
        if (resolution is not { IsComplete: true })
        {
            return false;
        }

        foreach (var line in resolution.Lines)
        {
            var item = snapshot.Items.FirstOrDefault(x => x.ReturnItemId == line.ReturnItemId);
            if (item is null)
            {
                return false;
            }

            if (line.AcceptedUsable > 0m && item.Disposition != ReturnItemDisposition.SaleableRestock)
            {
                return false;
            }

            if (line.AcceptedUsable <= 0m && line.AcceptedDamaged > 0m && item.Disposition == ReturnItemDisposition.None)
            {
                return false;
            }

            if (OwedCustody(line, snapshot.Custody) > 0m)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Rejected received quantity of one line that the owning customer has NOT yet physically received back.</summary>
    public static decimal OwedCustody(
        ReturnResolutionLineSnapshot line,
        IReadOnlyList<ReturnCustodySnapshot>? custody)
    {
        if (line.RejectedReceived <= 0m)
        {
            return 0m;
        }

        var handedBack = custody?
            .Where(x => x.ReturnItemId == line.ReturnItemId)
            .Sum(x => x.Quantity) ?? 0m;
        return Math.Max(0m, line.RejectedReceived - handedBack);
    }

    /// <summary>The exact approved money of a case: the frozen reviewed amount, else the legacy baseline when money already started.</summary>
    public static decimal MoneyOf(ReturnSnapshot snapshot) =>
        snapshot.ApprovedAmount
        ?? (snapshot.RefundAttempts.Count > 0
            ? snapshot.RefundAmount
            : Resolve(snapshot).ApprovedAmount);
}

/// <summary>
/// TB-279: whether an audited decision correction is available, derived from stored facts only. Every blocker is a
/// real recorded effect the correction may NOT erase — a correction is never a financial/physical reset.
/// </summary>
public static class ReturnCorrectionPolicy
{
    /// <summary>Describes the correction availability of one stored case.</summary>
    public static ReturnCorrectionAssessment Describe(ReturnSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Origin != ReturnOrigin.CustomerDelivered)
        {
            // A courier-origin case's receipt/claim/closure belongs to Fulfillment; re-acquiring a released claim is not
            // an available owning operation here, so the correction is refused with its own typed reason.
            return new ReturnCorrectionAssessment(false, ReturnsErrorCodes.CorrectionSourceUnsupported);
        }

        if (snapshot.EffectiveDecision is null)
        {
            // Nothing was decided (or the case is already back in review) — there is no decision to correct.
            return new ReturnCorrectionAssessment(false, ReturnsErrorCodes.CorrectionNotApplicable);
        }

        if (snapshot.Status is not (ReturnRequestStatus.Approved or ReturnRequestStatus.Rejected))
        {
            return new ReturnCorrectionAssessment(false, ReturnsErrorCodes.CorrectionStateInvalid);
        }

        if (snapshot.RefundAttempts.Count > 0)
        {
            // Pending / failed / unknown / succeeded — ANY money attempt is a recorded financial effect.
            return new ReturnCorrectionAssessment(false, ReturnsErrorCodes.CorrectionFinancialEffectRecorded);
        }

        if (snapshot.Custody is { Count: > 0 })
        {
            return new ReturnCorrectionAssessment(false, ReturnsErrorCodes.CorrectionCustodyRecorded);
        }

        if (snapshot.Items.Any(x => x.Disposition != ReturnItemDisposition.None))
        {
            return new ReturnCorrectionAssessment(false, ReturnsErrorCodes.CorrectionPhysicalEffectRecorded);
        }

        if (snapshot.ClosurePhase != CourierOriginClosurePhase.None
            || snapshot.ObligationClosedAt is not null
            || snapshot.StockSettledAt is not null)
        {
            return new ReturnCorrectionAssessment(false, ReturnsErrorCodes.CorrectionSourceEffectRecorded);
        }

        return new ReturnCorrectionAssessment(true, null);
    }
}
