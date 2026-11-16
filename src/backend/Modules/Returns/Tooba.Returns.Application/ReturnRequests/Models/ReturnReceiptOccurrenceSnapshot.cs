using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-275: ONE recorded physical customer receipt occurrence, as stored. The workspace lists every occurrence so an
/// operator sees what actually arrived, when, and in which inspected condition — a partial delivery can never read as
/// one unexplained total.
/// </summary>
public sealed record ReturnReceiptOccurrenceSnapshot(
    Guid ReturnReceiptId,
    string OccurrenceKey,
    Guid ReceivedByUserId,
    DateTimeOffset RecordedAt,
    IReadOnlyList<ReturnReceiptLineSnapshot> Lines);

/// <summary>TB-275: one inspected (item, condition) line of a recorded occurrence.</summary>
public sealed record ReturnReceiptLineSnapshot(
    Guid ReturnItemId,
    decimal Quantity,
    ReturnItemCondition Condition);
