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

    /// <inheritdoc />
    public async Task<Guid> EnsureDevelopmentOrganizationAsync(
        string displayName,
        string? legalName,
        CancellationToken cancellationToken)
    {
        var existing = await db.Parties.AsNoTracking()
            .Where(p => p.Kind == PartyKind.Organization && p.DisplayName == displayName)
            .OrderBy(p => p.CreatedAt)
            .Select(p => (Guid?)p.PartyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is { } partyId)
        {
            return partyId;
        }

        var party = BusinessParty.CreateOrganization(displayName, legalName, clock.UtcNow);
        db.Parties.Add(party);
        await db.SaveChangesAsync(cancellationToken);
        return party.PartyId;
    }

    /// <inheritdoc />
    public async Task EnsureDevelopmentOrganizationDisplayNamesAsync(
        IReadOnlyCollection<DevelopmentOrganizationRename> renames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(renames);
        if (renames.Count == 0)
        {
            return;
        }

        var sourceNames = renames.Select(x => x.FromDisplayName).Distinct(StringComparer.Ordinal).ToArray();
        var parties = await db.Parties
            .Where(p => sourceNames.Contains(p.DisplayName))
            .ToListAsync(cancellationToken);
        var changed = false;
        foreach (var party in parties)
        {
            var target = renames.FirstOrDefault(x => string.Equals(x.FromDisplayName, party.DisplayName, StringComparison.Ordinal));
            if (target is null || string.Equals(target.ToDisplayName, party.DisplayName, StringComparison.Ordinal))
            {
                continue;
            }

            party.DisplayName = target.ToDisplayName;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
