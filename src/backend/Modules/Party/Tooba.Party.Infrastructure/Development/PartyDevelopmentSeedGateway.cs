using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;


using Tooba.Party.Domain.Aggregates;
using Tooba.Party.Domain.Enums;
using Tooba.Party.Domain.Events;
using Tooba.Party.Infrastructure.Persistence;

using Tooba.Party.Application.Ports;

using Tooba.Party.Contracts.Ports;

namespace Tooba.Party.Infrastructure.Development;

/// <summary>Development-only Party seed capability owned by Party.Infrastructure.</summary>
public sealed class PartyDevelopmentSeedGateway(
    PartyDbContext db,
    IPartyDirectory parties,
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

    /// <inheritdoc />
    public async Task<Guid?> FindDevelopmentOrganizationByDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken)
    {
        var existing = await db.Parties.AsNoTracking()
            .Where(p => p.Kind == PartyKind.Organization && p.DisplayName == displayName)
            .OrderBy(p => p.CreatedAt)
            .Select(p => (Guid?)p.PartyId)
            .FirstOrDefaultAsync(cancellationToken);
        return existing;
    }

    /// <inheritdoc />
    public async Task<Guid?> FindDevelopmentMembershipSellerPartyAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = await db.Memberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.RelationCode == MembershipRelationCodes.Member)
            .OrderBy(x => x.PartyId)
            .Select(x => (Guid?)x.PartyId)
            .FirstOrDefaultAsync(cancellationToken);
        return membership;
    }

    /// <inheritdoc />
    public async Task EnsureDevelopmentMemberMembershipAsync(
        Guid userId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var exists = await db.Memberships.AsNoTracking().AnyAsync(
            x => x.UserId == userId && x.PartyId == sellerPartyId && x.RelationCode == MembershipRelationCodes.Member,
            cancellationToken);
        if (exists)
        {
            return;
        }

        await parties.EstablishMembershipAsync(userId, sellerPartyId, MembershipRelationCodes.Member, cancellationToken);
    }
}
