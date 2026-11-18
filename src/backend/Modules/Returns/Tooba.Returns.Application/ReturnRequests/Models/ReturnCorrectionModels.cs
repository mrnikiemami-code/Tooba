using System.Diagnostics.CodeAnalysis;
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
/// TB-279 / TB-279-R1: the reviewed resolution of ONE requested line, PER CONDITION.
/// <para>
/// <see cref="AcceptedUsable"/> and <see cref="AcceptedDamaged"/> are the refundable portions of each inspected
/// condition, <see cref="RejectedReceivedUsable"/> and <see cref="RejectedReceivedDamaged"/> are the received units that
/// were declined and stay the customer's property (owed back), and <see cref="RejectedUnreceived"/> is the part of the
/// original claim that never physically arrived. Nothing is inferred: an earlier revision derived the accepted condition
/// from <c>Min(accepted, usable)</c>, which silently chose a condition the operator never reviewed.
/// </para>
/// </summary>
public sealed record ReturnResolutionLineSnapshot(
    Guid ReturnItemId,
    decimal Requested,
    decimal Received,
    decimal Usable,
    decimal Damaged,
    decimal AcceptedUsable,
    decimal AcceptedDamaged,
    decimal RejectedReceivedUsable,
    decimal RejectedReceivedDamaged,
    decimal RejectedUnreceived,
    string? Reason,
    int Version,
    /// <summary>
    /// False for a stored plan row that predates the per-condition columns. Such a row can never be read as a lawful
    /// split, because its accepted condition is unknown — the plan is incomplete and asks to be reviewed again.
    /// </summary>
    bool PortionsKnown = true)
{
    /// <summary>The refundable quantity of this line (both accepted conditions).</summary>
    public decimal Accepted => AcceptedUsable + AcceptedDamaged;

    /// <summary>The declined quantity of this line, received or never received.</summary>
    public decimal Rejected => RejectedReceivedUsable + RejectedReceivedDamaged + RejectedUnreceived;

    /// <summary>Declined units that physically arrived and are therefore owed back to the owning customer.</summary>
    public decimal RejectedReceived => RejectedReceivedUsable + RejectedReceivedDamaged;
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
    DateTimeOffset RecordedAt,
    /// <summary>TB-279-R1: the reviewed proof kind of this handback (operator attestation or owning carrier fact).</summary>
    string? ProofKind = null);

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
    ///  - A stored reviewed plan (the highest version, one row per requested item) must name a coherent per-condition
    ///    split that accounts for the whole claim exactly, and its accepted portions may not exceed what was physically
    ///    received in that same condition.
    ///  - With no stored plan, complete receiving coverage is the only unambiguous baseline. Anything less is
    ///    incomplete and names the exact next step — a case whose accepted quantity was received but whose rejected
    ///    remainder never arrived is only lawful through a REVIEWED plan.
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

                if (!stored.PortionsKnown)
                {
                    // A pre-R1 plan row never named the accepted condition, so it can never authorize money or a
                    // restock. The operator republishes a reviewed, explicit split.
                    return new ReturnResolutionSnapshot(
                        storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionPortionRequired);
                }

                var portions = new[]
                {
                    stored.AcceptedUsable,
                    stored.AcceptedDamaged,
                    stored.RejectedReceivedUsable,
                    stored.RejectedReceivedDamaged,
                    stored.RejectedUnreceived,
                };
                if (portions.Any(value => value < 0m || !PersistedReturnQuantity.IsExactlyRepresentable(value)))
                {
                    return new ReturnResolutionSnapshot(storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionInvalid);
                }

                if (stored.Accepted + stored.Rejected != requested)
                {
                    // The whole original claim must be accounted for; a remainder is never silently refunded or kept.
                    return new ReturnResolutionSnapshot(storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionIncomplete);
                }

                // Each condition is bounded by ITS OWN received quantity. A usable unit can never stand in for a
                // damaged one, and an accepted portion beyond what arrived is not a refund.
                if (stored.AcceptedUsable > usable
                    || stored.AcceptedDamaged > damaged
                    || stored.RejectedReceivedUsable > usable - stored.AcceptedUsable
                    || stored.RejectedReceivedDamaged > damaged - stored.AcceptedDamaged)
                {
                    return new ReturnResolutionSnapshot(storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionReceivedExceeded);
                }

                if (stored.RejectedUnreceived != requested - receivedQuantity)
                {
                    // The never-received remainder is a physical fact, not a free choice: it must equal exactly what
                    // the coherent receipt says never arrived.
                    return new ReturnResolutionSnapshot(storedVersion, false, 0m, true, storedLines, ReturnsErrorCodes.ResolutionPortionInvalid);
                }

                approved += MoneyOf(item.UnitPriceSnapshot, stored.Accepted);
                lines.Add(stored with
                {
                    Requested = requested,
                    Received = receivedQuantity,
                    Usable = usable,
                    Damaged = damaged,
                });
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

        // Complete coverage with no counter-evidence: every requested unit arrived, so every unit is accepted.
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
                received.Usable,
                received.Damaged,
                0m,
                0m,
                0m,
                null,
                0));
        }

        return new ReturnResolutionSnapshot(0, true, Math.Round(baselineApproved, 4, MidpointRounding.AwayFromZero), false, baselineLines);
    }

    /// <summary>
    /// TB-279-R1 (F3): the honest custody plan of a case that was REJECTED/CANCELLED without a reviewed partial plan.
    /// It accepts nothing (zero money) and names every received unit — per condition — as owed back to the customer, so
    /// a whole-case rejection with received goods exposes a lawful handback action instead of a false closure.
    /// </summary>
    public static ReturnResolutionSnapshot DescribeRejectedCustody(
        IReadOnlyList<ReturnItemSnapshot> items,
        ReturnReceivingSummary? receiving)
    {
        if (receiving is not { IsCoherent: true } summary)
        {
            return new ReturnResolutionSnapshot(0, false, 0m, true, [], ReturnsErrorCodes.RecoveryEvidenceConflict);
        }

        var lines = new List<ReturnResolutionLineSnapshot>(items.Count);
        foreach (var item in items)
        {
            var line = summary.Lines.FirstOrDefault(x => x.ReturnItemId == item.ReturnItemId);
            var requested = item.Quantity;
            var received = line?.Received ?? 0m;
            var usable = line?.Usable ?? 0m;
            var damaged = line?.Damaged ?? 0m;
            lines.Add(new ReturnResolutionLineSnapshot(
                item.ReturnItemId,
                requested,
                received,
                usable,
                damaged,
                AcceptedUsable: 0m,
                AcceptedDamaged: 0m,
                RejectedReceivedUsable: usable,
                RejectedReceivedDamaged: damaged,
                RejectedUnreceived: Math.Max(0m, requested - received),
                null,
                0));
        }

        return new ReturnResolutionSnapshot(0, true, 0m, true, lines);
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
    /// TB-279-R1 (F2): the ONE coverage answer every money/physical gate shares.
    /// <para>
    /// A REVIEWED plan is the authority: it accounts for the whole claim, bounds each accepted portion by its own
    /// received condition and names the never-received remainder exactly, so accepted 2 received + rejected 1 never
    /// received is a lawful COMPLETE basis even though the original claim was not fully delivered. Complete receiving
    /// coverage is required only for the IMPLICIT baseline plan (no reviewed split), and incoherent stored evidence is
    /// always a conflict. Nothing here infers a condition or clamps a quantity.
    /// </para>
    /// </summary>
    public static bool TryDescribeCompleteCoverage(
        ReturnSnapshot snapshot,
        out ReturnResolutionSnapshot resolution,
        [NotNullWhen(false)] out string? blockerCode,
        string incoherentCode = ReturnsErrorCodes.RecoveryEvidenceConflict,
        string incompleteReceiptCode = ReturnsErrorCodes.ReceivingIncomplete)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        resolution = Resolve(snapshot);
        if (snapshot.Receiving is { IsCoherent: false })
        {
            blockerCode = incoherentCode;
            return false;
        }

        if (resolution.FromReviewedPlan)
        {
            if (resolution.IsComplete)
            {
                blockerCode = null;
                return true;
            }

            blockerCode = resolution.BlockerCode ?? ReturnsErrorCodes.ResolutionRequired;
            return false;
        }

        // The IMPLICIT baseline (no reviewed split) has exactly ONE lawful basis: the whole claim was physically
        // received and the recorded evidence is coherent. A partial receipt can only become lawful through a reviewed
        // plan, never by assuming the missing units were "accepted".
        if (snapshot.Receiving is { IsCoherent: true, IsComplete: true })
        {
            blockerCode = null;
            return true;
        }

        blockerCode = incompleteReceiptCode;
        return false;
    }

    /// <summary>
    /// True when every accepted portion carries the durable physical fact it must (its own usable restock or its own
    /// non-saleable damaged record) and every rejected received portion was really handed back. Money-complete work is
    /// never reported as settled while received goods are still owed to the owning customer.
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

            if (line.AcceptedUsable > 0m)
            {
                // TB-279-R1 (F2): a NEW completion names the exact accepted USEABLE portion Inventory confirmed.
                if (item.UsableRestockedQuantity is { } restocked)
                {
                    if (restocked != line.AcceptedUsable)
                    {
                        return false;
                    }
                }
                else if (item.Disposition != ReturnItemDisposition.SaleableRestock)
                {
                    // A disposal recorded BEFORE per-portion facts existed is immutable history and still proves its
                    // effect, but only the SALEABLE one proves this portion; anything else (or none) is unsettled.
                    return false;
                }
            }

            if (line.AcceptedDamaged > 0m)
            {
                // The damaged portion is never saleable stock: it owes its own durable non-saleable record.
                if (item.DamagedRecordedQuantity is { } recordedDamaged)
                {
                    if (recordedDamaged != line.AcceptedDamaged)
                    {
                        return false;
                    }
                }
                else if (item.Disposition != ReturnItemDisposition.DamagedNonSaleable)
                {
                    // A legacy saleable restock of a damaged portion is exactly the false fact this rule refuses.
                    return false;
                }
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
        IReadOnlyList<ReturnCustodySnapshot>? custody) =>
        OwedCustodyOfCondition(line, custody, ReturnItemCondition.Usable)
        + OwedCustodyOfCondition(line, custody, ReturnItemCondition.Damaged);

    /// <summary>
    /// TB-279-R1 (F3): the outstanding handback of ONE condition. A handback may never exceed the rejected received
    /// portion of the condition it names, so a usable handback can never settle a damaged obligation.
    /// </summary>
    public static decimal OwedCustodyOfCondition(
        ReturnResolutionLineSnapshot line,
        IReadOnlyList<ReturnCustodySnapshot>? custody,
        ReturnItemCondition condition)
    {
        var rejected = condition == ReturnItemCondition.Damaged
            ? line.RejectedReceivedDamaged
            : line.RejectedReceivedUsable;
        if (rejected <= 0m)
        {
            return 0m;
        }

        var handedBack = custody?
            .Where(x => x.ReturnItemId == line.ReturnItemId && x.Condition == condition)
            .Sum(x => x.Quantity) ?? 0m;
        return Math.Max(0m, rejected - handedBack);
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

        if (snapshot.Items.Any(x => x.Disposition != ReturnItemDisposition.None
                || x.UsableRestockedQuantity is not null
                || x.DamagedRecordedQuantity is not null))
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
