namespace Tooba.Party.Contracts;

/// <summary>Minimal Party scalar projection required by cross-module Admin composition.</summary>
public sealed record PartyStatusProjection(Guid PartyId, string DisplayName, string Status);

/// <summary>
/// Party-owned Admin sellers read boundary.
/// Returns semantic projections only — never EF types, entities, or queryables.
/// </summary>
public interface IPartyAdminSellerReadGateway
{
    /// <summary>
    /// Returns the Party slice (id, display name, status) for the requested party ids.
    /// Unknown ids are omitted; result order is not significant.
    /// </summary>
    Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
        IReadOnlyList<Guid> partyIds,
        CancellationToken cancellationToken);
}
