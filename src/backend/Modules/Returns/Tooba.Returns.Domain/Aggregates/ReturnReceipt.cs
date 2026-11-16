using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Domain.Aggregates;

/// <summary>
/// TB-274: ONE explicitly recorded customer return receipt (one physical occurrence) of a customer-delivered case.
/// <para>
/// A delivered-customer RMA previously had no recorded receiving/inspection fact: approval restocked the whole
/// requested quantity from the line's default condition and nothing stated what physically arrived. This aggregate is
/// the owning record of what actually arrived — per line, how many units this occurrence received and how many of them
/// are <see cref="ReturnItemCondition.Usable"/> versus <see cref="ReturnItemCondition.Damaged"/> — recorded in
/// occurrences so a case can be received across more than one delivery without splitting the requested line.
/// </para>
/// <para>
/// The occurrence key is the durable identity of one receipt: the same key with the same lines is an exact replay, a
/// different intent under the same key is a conflict. The aggregate never moves money or stock; it only records the
/// physical fact every later gate reads. A courier-origin case's receipt belongs to Fulfillment and is never created
/// here.
/// </para>
/// </summary>
public sealed class ReturnReceipt
{
    /// <summary>Persisted length of the occurrence identity.</summary>
    public const int OccurrenceKeyMaxLength = 128;

    private readonly List<ReturnReceiptLine> _lines = [];

    private ReturnReceipt()
    {
    }

    /// <summary>شناسه رسید.</summary>
    public Guid ReturnReceiptId { get; init; }

    /// <summary>پروندهٔ مرجوعی مالک این رسید.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>TB-274: reviewed occurrence identity of this receipt (durable exact replay).</summary>
    public string OccurrenceKey { get; init; } = string.Empty;

    /// <summary>اپراتوری که رسید را ثبت کرد.</summary>
    public Guid ReceivedByUserId { get; init; }

    /// <summary>زمان ثبت فیزیکی.</summary>
    public DateTimeOffset RecordedAt { get; init; }

    /// <summary>خطوط بازرسی این رسید.</summary>
    public IReadOnlyList<ReturnReceiptLine> Lines => _lines;

    /// <summary>
    /// TB-274: builds one receipt occurrence. Every invariant the owning directory enforces is repeated here so no
    /// future caller can persist a receipt the gates cannot read: a non-empty occurrence identity within the persisted
    /// length, at least one line, and per line a real item, a positive quantity the persisted six-decimal scale stores
    /// exactly, a defined condition and no duplicate (item, condition) inside one occurrence.
    /// </summary>
    public static ReturnReceipt Create(
        Guid returnReceiptId,
        Func<Guid> newId,
        Guid returnRequestId,
        string occurrenceKey,
        Guid receivedByUserId,
        DateTimeOffset recordedAt,
        IEnumerable<(Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)> lines)
    {
        ArgumentNullException.ThrowIfNull(newId);
        ArgumentNullException.ThrowIfNull(lines);

        if (returnReceiptId == Guid.Empty || returnRequestId == Guid.Empty || receivedByUserId == Guid.Empty)
        {
            throw new InvalidOperationException("return.receipt.line_invalid");
        }

        var key = occurrenceKey?.Trim() ?? string.Empty;
        if (key.Length == 0 || key.Length > OccurrenceKeyMaxLength)
        {
            throw new InvalidOperationException("return.receipt.occurrence_required");
        }

        var received = lines as IReadOnlyList<(Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)>
            ?? lines.ToArray();
        if (received.Count == 0)
        {
            throw new InvalidOperationException("return.receipt.line_invalid");
        }

        var receipt = new ReturnReceipt
        {
            ReturnReceiptId = returnReceiptId,
            ReturnRequestId = returnRequestId,
            OccurrenceKey = key,
            ReceivedByUserId = receivedByUserId,
            RecordedAt = recordedAt,
        };

        var identities = new HashSet<(Guid, ReturnItemCondition)>();
        foreach (var line in received)
        {
            if (line.ReturnItemId == Guid.Empty
                || line.Quantity <= 0m
                || !PersistedReturnQuantity.IsExactlyRepresentable(line.Quantity)
                || !Enum.IsDefined(line.Condition)
                || !identities.Add((line.ReturnItemId, line.Condition)))
            {
                throw new InvalidOperationException("return.receipt.line_invalid");
            }

            receipt._lines.Add(ReturnReceiptLine.Create(
                newId(),
                returnReceiptId,
                returnRequestId,
                line.ReturnItemId,
                line.Quantity,
                line.Condition));
        }

        return receipt;
    }

    /// <summary>The normalized intent this receipt stores, used for exact replay comparison.</summary>
    public IReadOnlyList<(Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)> Intent() =>
        _lines
            .Select(line => (line.ReturnItemId, line.Quantity, line.Condition))
            .OrderBy(line => line.ReturnItemId)
            .ThenBy(line => line.Condition)
            .ThenBy(line => line.Quantity)
            .ToArray();

    /// <summary>خطوط بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedLines(IEnumerable<ReturnReceiptLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.Clear();
        _lines.AddRange(lines);
    }
}
