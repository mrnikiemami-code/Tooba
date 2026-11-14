using System.Globalization;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Domain.Aggregates;

/// <summary>
/// TB-265: one exclusive claim of a returned parcel's physically received quantity, created for a courier-origin
/// Returns case.
/// <para>
/// The returned quantity of a parcel is ONE pool. It can fund a resend, a damaged replacement or a Returns case —
/// never two at once. Fulfillment owns the receipt, so it owns the claim: the Returns case cannot be created until
/// the exact quantity is durably claimed here, and the claim cannot be released by anything except the owning
/// durable decision (reject/cancel) or the approval settlement.
/// </para>
/// </summary>
public sealed class CourierReturnClaim
{
    private List<CourierReturnClaimLine>? _lines;

    private CourierReturnClaim()
    {
    }

    /// <summary>Stable claim identity.</summary>
    public Guid CourierReturnClaimId { get; init; }

    /// <summary>Owning fulfillment unit.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>The returned parcel the quantity came back on.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>The single physical origin receipt that is reused (never re-received).</summary>
    public Guid ShipmentReturnReceiptId { get; init; }

    /// <summary>Reviewed occurrence identity; unique per intent.</summary>
    public string OccurrenceKey { get; init; } = string.Empty;

    /// <summary>Durable lifecycle.</summary>
    public CourierReturnClaimStatus Status { get; private set; }

    /// <summary>Operator who reviewed the claim.</summary>
    public Guid CreatedByUserId { get; init; }

    /// <summary>Created at.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Last change.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>The Returns case funded by this claim (null until it is linked once).</summary>
    public Guid? ReturnRequestId { get; private set; }

    /// <summary>
    /// Uniqueness guard for still-active claims. It is non-null only while the claim holds capacity, so many terminal
    /// rows can coexist while at most ONE active claim can exist per (receipt, line, condition).
    /// </summary>
    public string? ActiveKey { get; private set; }

    /// <summary>TB-265: when the approval settlement closed this branch (null while still open).</summary>
    public DateTimeOffset? SettledAt { get; private set; }

    /// <summary>TB-265: quantity released from the unconsumed hold by the settlement (durable replay answer).</summary>
    public decimal ReleasedHeldQuantity { get; private set; }

    /// <summary>Canonical claim-line payload (stable, invariant formatting).</summary>
    public string LinesPayload { get; private set; } = string.Empty;

    /// <summary>Claimed lines.</summary>
    public IReadOnlyList<CourierReturnClaimLine> Lines =>
        _lines ??= ParsePayload(LinesPayload);

    /// <summary>True while this claim still holds capacity.</summary>
    public bool IsActive => Status is CourierReturnClaimStatus.Requested or CourierReturnClaimStatus.Approved;

    /// <summary>Creates the one exclusive claim for a reviewed courier-origin quantity.</summary>
    public static CourierReturnClaim Create(
        Guid claimId,
        Guid fulfillmentId,
        Guid shipmentId,
        Guid shipmentReturnReceiptId,
        Guid actorUserId,
        string occurrenceKey,
        IReadOnlyList<CourierReturnClaimLine> lines,
        DateTimeOffset now)
    {
        if (claimId == Guid.Empty || fulfillmentId == Guid.Empty || shipmentId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.returns.claim_ids_required");
        }

        if (shipmentReturnReceiptId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.returns.claim_receipt_required");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.returns.claim_actor_required");
        }

        if (string.IsNullOrWhiteSpace(occurrenceKey) || occurrenceKey.Trim().Length > 128)
        {
            throw new ContractOperationException("fulfillment.returns.claim_occurrence_required");
        }

        if (lines is null || lines.Count == 0)
        {
            throw new ContractOperationException("fulfillment.returns.claim_lines_required");
        }

        var normalized = lines
            .Where(x => x.Quantity > 0m)
            .Select(x => new CourierReturnClaimLine(x.OrderLineId, x.Quantity, x.Condition))
            .ToArray();

        if (normalized.Length == 0 || normalized.Length != lines.Count)
        {
            throw new ContractOperationException("fulfillment.returns.claim_quantity_invalid");
        }

        if (normalized.Any(x => x.OrderLineId == Guid.Empty || !Enum.IsDefined(x.Condition)))
        {
            throw new ContractOperationException("fulfillment.returns.claim_condition_invalid");
        }

        if (normalized.GroupBy(x => new { x.OrderLineId, x.Condition }).Any(g => g.Count() > 1))
        {
            throw new ContractOperationException("fulfillment.returns.claim_duplicate_line");
        }

        var claim = new CourierReturnClaim
        {
            CourierReturnClaimId = claimId,
            FulfillmentId = fulfillmentId,
            ShipmentId = shipmentId,
            ShipmentReturnReceiptId = shipmentReturnReceiptId,
            OccurrenceKey = occurrenceKey.Trim(),
            Status = CourierReturnClaimStatus.Requested,
            CreatedByUserId = actorUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        claim._lines = normalized.ToList();
        claim.LinesPayload = BuildPayload(normalized);
        claim.ActiveKey = claim.BuildActiveKey();
        return claim;
    }

    /// <summary>Loads the persisted lines (used when a caller already read the rows).</summary>
    public void AttachLoadedLines(IEnumerable<CourierReturnClaimLine> lines)
    {
        _lines = lines.ToList();
        LinesPayload = BuildPayload(_lines);
    }

    /// <summary>Does this claim answer exactly the reviewed intent?</summary>
    public bool AnswersIntent(Guid fulfillmentId, Guid shipmentId, IReadOnlyList<CourierReturnClaimLine> lines)
    {
        var current = Lines;
        if (FulfillmentId != fulfillmentId || ShipmentId != shipmentId || current.Count != lines.Count)
        {
            return false;
        }

        foreach (var line in lines)
        {
            if (!current.Any(x => x.OrderLineId == line.OrderLineId
                                  && x.Condition == line.Condition
                                  && x.Quantity == line.Quantity))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Links the created Returns case exactly once; a changed case is a typed conflict.</summary>
    public void LinkCase(Guid returnRequestId, DateTimeOffset now)
    {
        if (returnRequestId == Guid.Empty)
        {
            throw new ContractOperationException("fulfillment.returns.case_required");
        }

        if (Status is CourierReturnClaimStatus.Rejected or CourierReturnClaimStatus.Cancelled)
        {
            // A released claim holds no capacity, so it can never be attached to a live Returns case afterwards.
            throw new ContractOperationException("fulfillment.returns.claim_not_open");
        }

        if (ReturnRequestId is { } existing)
        {
            if (existing != returnRequestId)
            {
                throw new ContractOperationException("fulfillment.returns.claim_case_conflict");
            }

            UpdatedAt = now;
            return;
        }

        ReturnRequestId = returnRequestId;
        UpdatedAt = now;
    }

    /// <summary>Releases ONLY this branch claim; the order's stock hold is untouched.</summary>
    public void Release(CourierReturnClaimStatus terminalStatus, DateTimeOffset now)
    {
        if (terminalStatus is not (CourierReturnClaimStatus.Rejected or CourierReturnClaimStatus.Cancelled))
        {
            throw new ContractOperationException("fulfillment.returns.claim_release_state_invalid");
        }

        if (Status == terminalStatus)
        {
            UpdatedAt = now;
            return;
        }

        if (Status == CourierReturnClaimStatus.Approved)
        {
            throw new ContractOperationException("fulfillment.returns.claim_already_approved");
        }

        if (Status is CourierReturnClaimStatus.Rejected or CourierReturnClaimStatus.Cancelled)
        {
            // A different terminal state after the branch was released is a harmless replay of the same effect.
            UpdatedAt = now;
            return;
        }

        Status = terminalStatus;
        ActiveKey = null;
        UpdatedAt = now;
    }

    /// <summary>
    /// Approval settlement. The claim keeps blocking its quantity (the case still owes the customer closure), and the
    /// branch becomes final so no resend/replacement can fund the same units again.
    /// </summary>
    public void MarkApproved(Guid returnRequestId, DateTimeOffset now)
    {
        if (Status == CourierReturnClaimStatus.Approved)
        {
            UpdatedAt = now;
            return;
        }

        if (Status != CourierReturnClaimStatus.Requested)
        {
            throw new ContractOperationException("fulfillment.returns.claim_not_open");
        }

        ReturnRequestId ??= returnRequestId;
        Status = CourierReturnClaimStatus.Approved;
        ActiveKey ??= BuildActiveKey();
        UpdatedAt = now;
    }

    /// <summary>
    /// Final closure after the settlement: the returned/closed case permanently consumes the capacity, so the
    /// active-key guard is cleared while the claim row stays as durable history.
    /// </summary>
    public void MarkSettled(decimal releasedHeldQuantity, DateTimeOffset now)
    {
        Status = CourierReturnClaimStatus.Approved;
        ActiveKey = null;
        if (SettledAt is not null)
        {
            // The first durable settlement is the answer; a replay after a lost response can never change how much
            // was released or release the same held quantity a second time.
            UpdatedAt = now;
            return;
        }

        ReleasedHeldQuantity = releasedHeldQuantity > 0m ? releasedHeldQuantity : 0m;
        SettledAt = now;
        UpdatedAt = now;
    }

    private string BuildActiveKey() =>
        $"{ShipmentReturnReceiptId:N}:{string.Join('|', Lines.Select(x => $"{x.OrderLineId:N}:{(int)x.Condition}"))}";

    private static string BuildPayload(IEnumerable<CourierReturnClaimLine> lines) =>
        string.Join(';', lines.Select(x =>
            $"{x.OrderLineId:N}|{x.Quantity.ToString(CultureInfo.InvariantCulture)}|{(int)x.Condition}"));

    private static List<CourierReturnClaimLine> ParsePayload(string payload)
    {
        var result = new List<CourierReturnClaimLine>();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return result;
        }

        foreach (var part in payload.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = part.Split('|');
            if (fields.Length != 3)
            {
                continue;
            }

            if (!Guid.TryParseExact(fields[0], "N", out var orderLineId)
                || !decimal.TryParse(fields[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity)
                || !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var condition))
            {
                continue;
            }

            result.Add(new CourierReturnClaimLine(orderLineId, quantity, (CourierReturnClaimCondition)condition));
        }

        return result;
    }
}
