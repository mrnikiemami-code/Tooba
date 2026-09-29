using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Party.Contracts;
using Tooba.Party.Domain;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Party.Infrastructure;

/// <summary>Development-only Party seed capability owned by Party.Infrastructure.</summary>
public sealed class PartyDevelopmentSeedGateway(
    PartyDbContext db,
    IClock clock) : IPartyDevelopmentSeedGateway
{
    /// <inheritdoc />
    public async Task<Guid> ResolveDevelopmentSellerPartyAsync(
        string displayName,
        string? legalName,
        CancellationToken cancellationToken)
    {
        var existing = await db.Parties.AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .Select(p => (Guid?)p.PartyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is { } sellerPartyId)
        {
            return sellerPartyId;
        }

        var party = BusinessParty.CreateOrganization(displayName, legalName, clock.UtcNow);
        db.Parties.Add(party);
        await db.SaveChangesAsync(cancellationToken);
        return party.PartyId;
    }
}
