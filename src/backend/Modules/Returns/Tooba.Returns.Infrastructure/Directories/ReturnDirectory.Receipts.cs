using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-274: the physical customer receipt of a delivered-customer RMA.
/// <para>
/// Returns owns this fact for a delivered-customer case the same way Fulfillment owns the courier-origin receipt: one
/// row per physical occurrence, one row per (line, condition) inside it. Recording it is a pure physical statement —
/// it moves no money, releases no hold and restocks nothing — and it is the ONLY source of the "what actually
/// arrived, and how much of it is usable" answer that approval, money and restock are gated on.
/// </para>
/// <para>
/// The occurrence key makes the write idempotent: the same key with the same lines answers the stored receipt, a
/// different intent under that key is a typed conflict. Over-receiving is refused cumulatively across occurrences, so
/// a case can be received partially without ever exceeding what the customer was asked to return, and the lock on the
/// case row serializes two concurrent receipts of the same case.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <inheritdoc />
    public async Task<ReturnSnapshot> RecordReceiptAsync(
        RecordReturnReceiptCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        // TB-274-R1: the recording operator is supplied by the authenticated authority, never by the browser, and a
        // receipt that names no actor is refused as a typed fault before any case read or write.
        if (command.ActorUserId == Guid.Empty)
        {
            throw new ContractOperationException(ReturnsErrorCodes.ReceiptActorRequired);
        }

        var occurrenceKey = NormalizeOccurrenceKey(command.OccurrenceKey);
        var lines = NormalizeReceiptLines(command.Items);

        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var ownedTransaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            if (relational)
            {
                // The case row is the shared protocol: two receipts of the same case serialize here, so the
                // read-then-insert of the cumulative coverage cannot interleave.
                await _db.Database.ExecuteSqlAsync(
                    $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {command.ReturnRequestId} FOR UPDATE",
                    cancellationToken);
            }

            var request = await _db.ReturnRequests.AsNoTracking()
                .SingleOrDefaultAsync(x => x.ReturnRequestId == command.ReturnRequestId, cancellationToken)
                ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);

            if (request.Origin != ReturnOrigin.CustomerDelivered)
            {
                // A courier-origin case's receipt already exists in Fulfillment and is REUSED; receiving it again
                // here would create a second, disagreeing physical fact.
                throw new ContractOperationException(ReturnsErrorCodes.ReceiptOriginInvalid);
            }

            var stored = await _db.ReturnReceipts.AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ReturnRequestId == command.ReturnRequestId
                         && x.OccurrenceKey == occurrenceKey,
                    cancellationToken);
            if (stored is not null)
            {
                var storedLines = await _db.ReturnReceiptLines.AsNoTracking()
                    .Where(x => x.ReturnReceiptId == stored.ReturnReceiptId)
                    .Select(x => new { x.ReturnItemId, x.Quantity, x.Condition })
                    .ToListAsync(cancellationToken);

                var storedIntent = storedLines
                    .Select(x => (x.ReturnItemId, x.Quantity, x.Condition))
                    .OrderBy(x => x.ReturnItemId)
                    .ThenBy(x => x.Condition)
                    .ThenBy(x => x.Quantity)
                    .ToArray();
                if (!storedIntent.SequenceEqual(lines.Select(x => (x.ReturnItemId, x.Quantity, x.Condition))
                        .OrderBy(x => x.ReturnItemId)
                        .ThenBy(x => x.Condition)
                        .ThenBy(x => x.Quantity)))
                {
                    // The same occurrence identity carrying a different inspected intent is never a replay.
                    throw new ContractOperationException(ReturnsErrorCodes.ReceiptConflict);
                }

                await CommitReceiptAsync(ownedTransaction, cancellationToken);
                return await MapSnapshotAsync(request, cancellationToken);
            }

            if (request.Status != ReturnRequestStatus.Requested)
            {
                // TB-274: a NEW physical receipt is a statement about goods the operator has in hand. It is lawful
                // while the case is still awaiting a decision, and for a case whose MONEY already started but whose
                // physical side is still entirely unsettled (the only way such a legacy case can lawfully proceed).
                // A decided, rejected, cancelled or already-settled physical history is a durable record this
                // submission may not retroactively change.
                var physicalHistoryExists = await _db.ReturnItems.AsNoTracking()
                    .AnyAsync(
                        x => x.ReturnRequestId == command.ReturnRequestId
                             && x.Disposition != ReturnItemDisposition.None,
                        cancellationToken)
                    || await _db.ReturnReceipts.AsNoTracking()
                        .AnyAsync(x => x.ReturnRequestId == command.ReturnRequestId, cancellationToken);
                var moneyStartedWithoutPhysicalHistory = request.Status is ReturnRequestStatus.Approved
                    or ReturnRequestStatus.RefundProcessing
                    or ReturnRequestStatus.RefundFailed
                    or ReturnRequestStatus.Completed;
                if (!moneyStartedWithoutPhysicalHistory || physicalHistoryExists)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ReceiptStateInvalid);
                }
            }

            var requestedItems = await _db.ReturnItems.AsNoTracking()
                .Where(x => x.ReturnRequestId == command.ReturnRequestId)
                .Select(x => new { x.ReturnItemId, x.Quantity })
                .ToListAsync(cancellationToken);
            var requestedById = requestedItems.ToDictionary(x => x.ReturnItemId, x => x.Quantity);

            foreach (var line in lines)
            {
                if (!requestedById.ContainsKey(line.ReturnItemId))
                {
                    // A receipt may only answer for a line of THIS case.
                    throw new ContractOperationException(ReturnsErrorCodes.ReceiptLineInvalid);
                }
            }

            // TB-274-R1: the cumulative coverage is read through the SAME owning binding every read/action uses. If the
            // already-stored evidence is contradictory (borrowed receipt header, foreign item, non-positive or
            // undefined stored row, duplicate identity, over-coverage) a NEW occurrence never "repairs" it by adding
            // more lines: it is refused with the typed evidence conflict and zero new rows.
            var coverage = await ReadReceivingAsync(command.ReturnRequestId, cancellationToken);
            if (coverage is not { IsCoherent: true })
            {
                throw new ContractOperationException(ReturnsErrorCodes.RecoveryEvidenceConflict);
            }

            foreach (var group in lines.GroupBy(x => x.ReturnItemId))
            {
                var alreadyReceived = coverage.Lines
                    .Where(x => x.ReturnItemId == group.Key)
                    .Select(x => x.Received)
                    .Single();
                if (alreadyReceived + group.Sum(x => x.Quantity) > requestedById[group.Key])
                {
                    // Cumulative coverage across occurrences may never exceed what was requested.
                    throw new ContractOperationException(ReturnsErrorCodes.ReceiptOverReceived);
                }
            }

            var receipt = ReturnReceipt.Create(
                _ids.NewId(),
                () => _ids.NewId(),
                command.ReturnRequestId,
                occurrenceKey,
                command.ActorUserId,
                _clock.UtcNow,
                lines.Select(x => (x.ReturnItemId, x.Quantity, x.Condition)));

            _db.ReturnReceipts.Add(receipt);
            _db.ReturnReceiptLines.AddRange(receipt.Lines);
            await _db.SaveChangesAsync(cancellationToken);
            _telemetry.RecordReceipt();

            await CommitReceiptAsync(ownedTransaction, cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }
        finally
        {
            if (ownedTransaction is not null)
            {
                await ownedTransaction.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// TB-274: the stored receiving summary of a delivered-customer case, computed ONLY from the requested quantities
    /// and the recorded receipt lines. A courier-origin case has no receipt here (Fulfillment owns it), so it returns
    /// <c>null</c> rather than an empty, misleading "nothing received".
    /// </summary>
    private async Task<ReturnReceivingSummary?> ReadReceivingAsync(
        ReturnRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Origin != ReturnOrigin.CustomerDelivered)
        {
            return null;
        }

        return await ReadReceivingAsync(request.ReturnRequestId, cancellationToken);
    }

    /// <summary>
    /// TB-274-R1: the ONE stored-fact read of a case's receiving evidence. Every recorded line is bound to its OWN
    /// receipt header OF THIS CASE before it may count, so a line that names another case's receipt is never borrowed
    /// into this case's coverage — the summary reports that contradiction instead.
    /// </summary>
    private async Task<ReturnReceivingSummary> ReadReceivingAsync(
        Guid returnRequestId,
        CancellationToken cancellationToken)
    {
        var requestedItems = await _db.ReturnItems.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => new { x.ReturnItemId, x.Quantity })
            .ToListAsync(cancellationToken);

        var owningReceipts = await _db.ReturnReceipts.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => x.ReturnReceiptId)
            .ToListAsync(cancellationToken);

        var recordedLines = await _db.ReturnReceiptLines.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => new { x.ReturnReceiptId, x.ReturnItemId, x.Quantity, x.Condition })
            .ToListAsync(cancellationToken);

        return ReturnReceivingSummary.Describe(
            requestedItems.Select(x => (x.ReturnItemId, x.Quantity)),
            recordedLines.Select(x => ((Guid?)x.ReturnReceiptId, x.ReturnItemId, x.Quantity, x.Condition)),
            owningReceipts);
    }

    /// <summary>
    /// TB-274: the shared gate of every NEW ordinary money/stock start. Approval, a new refund attempt and a physical
    /// restock may only proceed over a complete, coherent receipt.
    /// </summary>
    private async Task EnsureOrdinaryReceivingCompleteAsync(
        ReturnRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Origin != ReturnOrigin.CustomerDelivered)
        {
            return;
        }

        var receiving = await ReadReceivingAsync(request, cancellationToken);
        if (receiving is null || !receiving.IsCoherent || !receiving.IsComplete)
        {
            throw new ContractOperationException(ReturnsErrorCodes.ReceivingIncomplete);
        }
    }

    private static async Task CommitReceiptAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? ownedTransaction,
        CancellationToken cancellationToken)
    {
        if (ownedTransaction is not null)
        {
            await ownedTransaction.CommitAsync(cancellationToken);
        }
    }

    /// <summary>TB-274: the reviewed occurrence identity of one delivery (trimmed, bounded, never defaulted).</summary>
    private static string NormalizeOccurrenceKey(string? occurrenceKey)
    {
        var key = occurrenceKey?.Trim() ?? string.Empty;
        if (key.Length == 0 || key.Length > ReturnReceipt.OccurrenceKeyMaxLength)
        {
            throw new ContractOperationException(ReturnsErrorCodes.ReceiptOccurrenceRequired);
        }

        return key;
    }

    /// <summary>
    /// TB-274: refused shapes before any read/write — an unknown/empty item, a non-positive quantity, more precision
    /// than the persisted scale stores, an undefined condition, or the same (item, condition) twice in one occurrence.
    /// </summary>
    private static IReadOnlyList<(Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)> NormalizeReceiptLines(
        IReadOnlyList<CustomerReceiptLine>? items)
    {
        if (items is null || items.Count == 0)
        {
            throw new ContractOperationException(ReturnsErrorCodes.ReceiptLineInvalid);
        }

        var identities = new HashSet<(Guid, ReturnItemCondition)>();
        var lines = new List<(Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)>(items.Count);
        foreach (var line in items)
        {
            if (line.ReturnItemId == Guid.Empty
                || line.Quantity <= 0m
                || !PersistedReturnQuantity.IsExactlyRepresentable(line.Quantity)
                || !Enum.IsDefined(line.Condition)
                || !identities.Add((line.ReturnItemId, line.Condition)))
            {
                throw new ContractOperationException(ReturnsErrorCodes.ReceiptLineInvalid);
            }

            lines.Add((line.ReturnItemId, line.Quantity, line.Condition));
        }

        return lines;
    }
}
