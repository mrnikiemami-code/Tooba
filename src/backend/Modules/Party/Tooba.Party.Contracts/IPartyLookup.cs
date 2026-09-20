namespace Tooba.Party.Contracts;

/// <summary>Provides a stable cross-module Party lookup.</summary>
public interface IPartyLookup
{
    /// <summary>Finds a party by its stable identifier.</summary>
    Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken);
}

/// <summary>Minimal Party identity required by consumers.</summary>
public sealed record PartyLookupResult(Guid PartyId, string Kind);
