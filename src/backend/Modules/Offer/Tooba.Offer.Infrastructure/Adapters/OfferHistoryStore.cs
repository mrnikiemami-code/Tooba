using Microsoft.EntityFrameworkCore;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Infrastructure.Persistence;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>EF-backed writer/reader for the Offer-owned history projection.</summary>
public sealed class OfferHistoryStore(OfferDbContext db) : IOfferHistoryWriter, IOfferHistoryReader
{
    /// <inheritdoc />
    public Task AppendAsync(OfferHistoryEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        db.OfferHistoryEntries.Add(entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferHistoryItem>> ListAsync(Guid offerId, CancellationToken cancellationToken) =>
        await db.OfferHistoryEntries.AsNoTracking().Where(x => x.OfferId == offerId)
            .OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.OfferHistoryEntryId)
            .Select(x => new OfferHistoryItem(
                x.OfferId, x.OccurredAt, x.ChangeKind, x.ActorUserId, x.Summary, x.OldValue, x.NewValue, x.OfferHistoryEntryId))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<OfferHistoryPage> GetPageAsync(
        Guid offerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (long)(safePage - 1) * safePageSize;
        var safeSkip = (int)Math.Min(skip, int.MaxValue);
        var source = db.OfferHistoryEntries.AsNoTracking().Where(x => x.OfferId == offerId);
        var total = await source.CountAsync(cancellationToken);
        var rows = await source
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.OfferHistoryEntryId)
            .Skip(safeSkip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
        var items = rows.Select(ToItem).ToArray();
        return new OfferHistoryPage(items, total, safePage, safePageSize);
    }

    private static OfferHistoryItem ToItem(OfferHistoryEntry entry) => new(
        entry.OfferId,
        entry.OccurredAt,
        entry.ChangeKind,
        entry.ActorUserId,
        entry.Summary,
        entry.OldValue,
        entry.NewValue,
        entry.OfferHistoryEntryId);
}
