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
/// <see cref="IsCoherent"/> is false when a receipt names an item that does not belong to the case or receives more
/// than was requested — a contradiction that must never be read as "complete". <see cref="IsComplete"/> is true only
/// for a coherent case with at least one line where EVERY requested unit was actually received.
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
    /// Composes the summary from the requested lines of the case and every recorded receipt line. Duplicate
    /// (item, condition) pairs are impossible by the receipt invariant, so the sums are exact.
    /// </summary>
    public static ReturnReceivingSummary Describe(
        IEnumerable<(Guid ReturnItemId, decimal Requested)> requested,
        IEnumerable<(Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)> received)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(received);

        var requestedLines = requested as IReadOnlyList<(Guid ReturnItemId, decimal Requested)> ?? requested.ToArray();
        var receipts = received as IReadOnlyList<(Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)>
            ?? received.ToArray();

        var requestedById = new Dictionary<Guid, decimal>();
        foreach (var line in requestedLines)
        {
            requestedById[line.ReturnItemId] = line.Requested;
        }

        // A receipt naming a line that does not belong to this case is a contradiction, never capacity.
        var coherent = true;
        var byItem = new Dictionary<Guid, (decimal Received, decimal Usable, decimal Damaged)>();
        foreach (var line in receipts)
        {
            if (!requestedById.ContainsKey(line.ReturnItemId))
            {
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
}
