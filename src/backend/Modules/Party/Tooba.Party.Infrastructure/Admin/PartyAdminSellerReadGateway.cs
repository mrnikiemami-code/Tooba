using Microsoft.EntityFrameworkCore;
using Tooba.Party.Contracts.Ports;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Party.Infrastructure.Admin;

/// <summary>
/// Party-owned adapter for the Admin sellers read boundary.
/// Keeps Party persistence inside the Party module and exposes only scalars.
/// </summary>
public sealed class PartyAdminSellerReadGateway(PartyDbContext db) : IPartyAdminSellerReadGateway
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
        IReadOnlyList<Guid> partyIds,
        CancellationToken cancellationToken)
    {
        if (partyIds.Count == 0)
        {
            return [];
        }

        var ids = partyIds.Distinct().ToArray();
        var rows = await db.Parties.AsNoTracking()
            .Where(x => ids.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName, x.Status })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new PartyStatusProjection(x.PartyId, x.DisplayName, x.Status.ToString()))
            .ToList();
    }
}
