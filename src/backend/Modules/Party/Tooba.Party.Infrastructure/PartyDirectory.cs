using Microsoft.EntityFrameworkCore;
using Tooba.Party.Application;
using Tooba.Party.Domain;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Party.Infrastructure;

/// <summary>
/// پیاده‌سازی نوشتن/خواندن Party روی schema همین ماژول. SpiceDB را در SaveChanges صدا نمی‌زند.
/// </summary>
public sealed class PartyDirectory : IPartyDirectory, IPartyLookupGateway
{
    private readonly PartyDbContext _db;

    /// <summary>
    /// دایرکتوری را به DbContext Tenant-aware وصل می‌کند نه به parse Host.
    /// </summary>
    public PartyDirectory(PartyDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<PartyReference?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken)
    {
        var party = await _db.Parties.AsNoTracking().SingleOrDefaultAsync(x => x.PartyId == partyId, cancellationToken);
        return party is null ? null : new PartyReference(party.PartyId, party.Kind, party.DisplayName);
    }

    /// <inheritdoc />
    public async Task<PartyReference> CreatePersonAsync(string displayName, CancellationToken cancellationToken)
    {
        var party = BusinessParty.CreatePerson(displayName, DateTimeOffset.UtcNow);
        _db.Parties.Add(party);
        await _db.SaveChangesAsync(cancellationToken);
        return new PartyReference(party.PartyId, party.Kind, party.DisplayName);
    }

    /// <inheritdoc />
    public async Task<OrganizationReference> CreateOrganizationAsync(string displayName, string? legalName, CancellationToken cancellationToken)
    {
        var party = BusinessParty.CreateOrganization(displayName, legalName, DateTimeOffset.UtcNow);
        _db.Parties.Add(party);
        await _db.SaveChangesAsync(cancellationToken);
        return new OrganizationReference(party.PartyId, party.DisplayName, party.LegalName);
    }

    /// <inheritdoc />
    public async Task<UserPartyLinkReference> LinkUserAsync(Guid userId, Guid partyId, CancellationToken cancellationToken)
    {
        if (!await _db.Parties.AnyAsync(x => x.PartyId == partyId, cancellationToken))
        {
            throw new InvalidOperationException("Party مقصد پیوند در این پایگاه Tenant وجود ندارد.");
        }

        var link = UserPartyLink.Bind(userId, partyId, DateTimeOffset.UtcNow);
        _db.UserLinks.Add(link);
        await _db.SaveChangesAsync(cancellationToken);
        return new UserPartyLinkReference(link.LinkId, link.UserId, link.PartyId);
    }

    /// <inheritdoc />
    public async Task<MembershipReference> EstablishMembershipAsync(Guid userId, Guid partyId, string relationCode, CancellationToken cancellationToken)
    {
        if (!await _db.Parties.AnyAsync(x => x.PartyId == partyId, cancellationToken))
        {
            throw new InvalidOperationException("Party مقصد عضویت در این پایگاه Tenant وجود ندارد.");
        }

        var membership = PartyMembership.Establish(userId, partyId, relationCode, DateTimeOffset.UtcNow);
        _db.Memberships.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);
        return new MembershipReference(membership.MembershipId, membership.UserId, membership.PartyId, membership.RelationCode, membership.Status);
    }

    /// <inheritdoc />
    public async Task<OrganizationRelationshipReference> RelateOrganizationsAsync(Guid fromPartyId, Guid toPartyId, string relationCode, CancellationToken cancellationToken)
    {
        var from = await _db.Parties.SingleAsync(x => x.PartyId == fromPartyId, cancellationToken);
        var to = await _db.Parties.SingleAsync(x => x.PartyId == toPartyId, cancellationToken);
        if (from.Kind != PartyKind.Organization || to.Kind != PartyKind.Organization)
        {
            throw new InvalidOperationException("رابطهٔ سازمانی فقط بین دو Organization است.");
        }

        var relationship = OrganizationRelationship.Connect(fromPartyId, toPartyId, relationCode, DateTimeOffset.UtcNow);
        _db.OrganizationRelationships.Add(relationship);
        await _db.SaveChangesAsync(cancellationToken);
        return new OrganizationRelationshipReference(
            relationship.RelationshipId,
            relationship.FromPartyId,
            relationship.ToPartyId,
            relationship.RelationCode);
    }

    /// <inheritdoc />
    public async Task GrantOrganizationCapabilityAsync(Guid organizationPartyId, string capabilityCode, CancellationToken cancellationToken)
    {
        var party = await _db.Parties.Include(x => x.Capabilities).SingleAsync(x => x.PartyId == organizationPartyId, cancellationToken);
        party.GrantCapability(capabilityCode, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OrganizationProfileSnapshot?> GetOrganizationProfileAsync(Guid partyId, CancellationToken cancellationToken)
    {
        var party = await _db.Parties.AsNoTracking().SingleOrDefaultAsync(x => x.PartyId == partyId, cancellationToken);
        if (party is null || party.Kind != PartyKind.Organization)
        {
            return null;
        }

        return MapOrganizationProfile(party);
    }

    /// <inheritdoc />
    public async Task<OrganizationProfileSnapshot> UpdateOrganizationProfileAsync(
        Guid partyId,
        OrganizationProfileWrite input,
        CancellationToken cancellationToken)
    {
        var party = await _db.Parties.SingleOrDefaultAsync(x => x.PartyId == partyId, cancellationToken)
            ?? throw new InvalidOperationException("سازمان مقصد پروفایل یافت نشد.");
        party.UpdateOrganizationProfile(
            input.DisplayName,
            input.LegalName,
            input.Description,
            input.SupportPhone,
            input.SupportEmail,
            input.AddressLine,
            DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return MapOrganizationProfile(party);
    }

    private static OrganizationProfileSnapshot MapOrganizationProfile(BusinessParty party) =>
        new(
            party.PartyId,
            party.DisplayName,
            party.LegalName,
            party.Description,
            party.SupportPhone,
            party.SupportEmail,
            party.AddressLine,
            party.UpdatedAt);
}
