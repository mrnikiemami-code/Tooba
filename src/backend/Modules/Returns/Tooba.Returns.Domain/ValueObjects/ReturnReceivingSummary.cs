namespace Tooba.Returns.Domain.ValueObjects;

/// <summary>
/// TB-274: the receiving/inspection fact of ONE requested line of a delivered-customer RMA, derived only from
/// durable facts (the requested quantity and the recorded receipt lines).
/// <para>
/// <see cref="Usable"/> is the only quantity that may ever become saleable stock again, and only after the money is
/// confirmed; <see cref="Damaged"/> is recorded and never restocked. <see cref="Remaining"/> is what the customer
/// still owes, so a partial delivery is visible instead of being rounded up to "received".
/// </para>
/// </summary>
public sealed record ReturnReceivingLine(
    Guid ReturnItemId,
    decimal Requested,
    decimal Received,
    decimal Usable,
    decimal Damaged);

/// <summary>
/// TB-274: the ONE receiving authority over stored facts. Requested/received/usable/damaged/remaining are computed
/// per line from the recorded receipt rows; nothing is inferred from a status, a condition default or an approval.
/// <para>
/// <see cref="IsCoherent"/> is false when the stored facts contradict the request: a receipt line whose header does not
/// belong to this case (borrowed evidence), a foreign item, a non-positive or non-representable quantity, an undefined
/// stored inspection, a duplicate (item, condition) row, an invalid/duplicate requested identity or a cumulative
/// over-receipt. A contradiction is never read as "complete" and never contributes capacity.
/// </para>
/// <para>
/// TB-274-R1: fail closed. Every stored number is validated before it is summed, so a malformed row can never be read
/// as usable coverage, and an undefined inspection is never classified as <see cref="ReturnItemCondition.Usable"/>.
/// <see cref="IsComplete"/> is true only for a coherent case with at least one line where EVERY requested unit was
/// actually received.
/// </para>
/// </summary>
public sealed record ReturnReceivingSummary(
    IReadOnlyList<ReturnReceivingLine> Lines,
    bool IsComplete,
    bool IsCoherent)
{
    /// <summary>True when at least one requested line exists, so completeness is a real fact and not an empty sum.</summary>
    public bool HasLines => Lines.Count > 0;

    /// <summary>Total received quantity across the case.</summary>
    public decimal Received => Lines.Sum(x => x.Received);

    /// <summary>Total inspected usable quantity across the case.</summary>
    public decimal Usable => Lines.Sum(x => x.Usable);

    /// <summary>Total inspected damaged quantity across the case.</summary>
    public decimal Damaged => Lines.Sum(x => x.Damaged);

    /// <summary>Remaining quantity the customer still has to hand over.</summary>
    public decimal Remaining => Lines.Sum(x => x.Requested - x.Received);

    /// <summary>
    /// TB-274-R1: the exact inspected usable quantity of ONE requested line, or <c>0</c> when this case carries no such
    /// line. It is the ONLY quantity a saleable restock may use, and it is never derived from the requested quantity or
    /// from a line's legacy condition flag.
    /// </summary>
    public decimal UsableOf(Guid returnItemId) =>
        Lines.FirstOrDefault(x => x.ReturnItemId == returnItemId)?.Usable ?? 0m;

    /// <summary>
    /// TB-279-R1 (F2): the exact inspected DAMAGED quantity of ONE requested line, or <c>0</c> when this case carries no
    /// such line. It is the only quantity a non-saleable record may name, and never the requested quantity.
    /// </summary>
    public decimal DamagedOf(Guid returnItemId) =>
        Lines.FirstOrDefault(x => x.ReturnItemId == returnItemId)?.Damaged ?? 0m;

    /// <summary>True when this line received at least one unit.</summary>
    public bool WasReceived(Guid returnItemId) =>
        Lines.Any(x => x.ReturnItemId == returnItemId && x.Received > 0m);

    /// <summary>
    /// Composes the summary from the requested lines of the case and every recorded receipt line. Duplicate
    /// (item, condition) pairs are impossible by the receipt invariant, so the sums are exact.
    /// </summary>
    public static ReturnReceivingSummary Describe(
        IEnumerable<(Guid ReturnItemId, decimal Requested)> requested,
        IEnumerable<(Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)> received) =>
        Describe(
            requested,
            received.Select(x => ((Guid?)null, x.ReturnItemId, x.Quantity, x.Condition)),
            owningReceiptIds: null);

    /// <summary>
    /// TB-274-R1: composes the summary from receipt rows that also carry their owning receipt header identity. A row
    /// whose header is not one of <paramref name="owningReceiptIds"/> is borrowed evidence: it never counts toward this
    /// case and marks the facts incoherent. When <paramref name="owningReceiptIds"/> is <c>null</c> the caller has
    /// already bound the rows to this case (the header identity is then not re-checked here).
    /// </summary>
    public static ReturnReceivingSummary Describe(
        IEnumerable<(Guid ReturnItemId, decimal Requested)> requested,
        IEnumerable<(Guid? ReturnReceiptId, Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)> received,
        IReadOnlyCollection<Guid>? owningReceiptIds)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(received);

        var requestedLines = requested as IReadOnlyList<(Guid ReturnItemId, decimal Requested)> ?? requested.ToArray();
        var receipts = received
                as IReadOnlyList<(Guid? ReturnReceiptId, Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)>
            ?? received.ToArray();
        var owning = owningReceiptIds is null ? null : new HashSet<Guid>(owningReceiptIds);

        var coherent = true;

        // The requested identity itself must be a real, positive, exactly persisted line, named once.
        var requestedById = new Dictionary<Guid, decimal>();
        foreach (var line in requestedLines)
        {
            if (line.ReturnItemId == Guid.Empty
                || line.Requested <= 0m
                || !PersistedReturnQuantity.IsExactlyRepresentable(line.Requested)
                || !requestedById.TryAdd(line.ReturnItemId, line.Requested))
            {
                coherent = false;
            }
        }

        // A receipt row counts only when it is bound to this case's own header, names a line of THIS case, carries a
        // positive exactly-persisted quantity, a DEFINED inspection and a unique (item, condition) identity.
        var byItem = new Dictionary<Guid, (decimal Received, decimal Usable, decimal Damaged)>();
        // The duplicate identity is PER OCCURRENCE — exactly the stored unique (receipt, item, condition) row — so two
        // separate deliveries of the same line may each cover it again (cumulative coverage), while the same
        // (item, condition) twice inside ONE occurrence stays a contradiction.
        var identities = new HashSet<(Guid?, Guid, ReturnItemCondition)>();
        foreach (var line in receipts)
        {
            if (line.ReturnReceiptId is { } receiptId && owning is not null && !owning.Contains(receiptId))
            {
                // A line answering for another case's receipt is a contradiction, never capacity.
                coherent = false;
                continue;
            }

            if (line.ReturnItemId == Guid.Empty || !requestedById.ContainsKey(line.ReturnItemId))
            {
                coherent = false;
                continue;
            }

            if (line.Quantity <= 0m
                || !PersistedReturnQuantity.IsExactlyRepresentable(line.Quantity)
                || !IsDefinedInspection(line.Condition)
                || !identities.Add((line.ReturnReceiptId, line.ReturnItemId, line.Condition)))
            {
                // A non-positive/overprecise/duplicated row is a contradiction; an undefined inspection is NEVER
                // classified as usable.
                coherent = false;
                continue;
            }

            var current = byItem.TryGetValue(line.ReturnItemId, out var existing) ? existing : default;
            byItem[line.ReturnItemId] = line.Condition == ReturnItemCondition.Damaged
                ? (current.Received + line.Quantity, current.Usable, current.Damaged + line.Quantity)
                : (current.Received + line.Quantity, current.Usable + line.Quantity, current.Damaged);
        }

        var lines = new List<ReturnReceivingLine>(requestedById.Count);
        foreach (var (returnItemId, requestedQuantity) in requestedById)
        {
            var sums = byItem.TryGetValue(returnItemId, out var value) ? value : default;
            if (sums.Received > requestedQuantity)
            {
                // Over-receiving is refused at the write boundary; if it is stored anyway it can never authorize
                // approval or a restock.
                coherent = false;
            }

            lines.Add(new ReturnReceivingLine(
                returnItemId,
                requestedQuantity,
                sums.Received,
                sums.Usable,
                sums.Damaged));
        }

        var complete = coherent
            && lines.Count > 0
            && lines.All(x => x.Received == x.Requested && x.Received > 0m);

        return new ReturnReceivingSummary(lines, complete, coherent);
    }

    /// <summary>
    /// TB-274-R1: the only two values a stored inspection may carry. Anything else is an undefined provider/legacy
    /// value and is refused instead of being treated as a usable or damaged quantity.
    /// </summary>
    private static bool IsDefinedInspection(ReturnItemCondition condition) =>
        condition is ReturnItemCondition.Usable or ReturnItemCondition.Damaged;
}
