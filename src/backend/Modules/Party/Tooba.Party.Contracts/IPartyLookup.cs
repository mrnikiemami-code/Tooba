namespace Tooba.Party.Contracts;

/// <summary>Provides a stable cross-module Party lookup.</summary>
public interface IPartyLookup
{
    /// <summary>Finds a party by its stable identifier.</summary>
    Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken);

    /// <summary>Batch display names for grid enrichment.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyList<Guid> partyIds,
        CancellationToken cancellationToken);

    /// <summary>Search party ids by display name substring.</summary>
    Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
        string term,
        int take,
        CancellationToken cancellationToken);

    /// <summary>Filter party ids by display name with grid operators.</summary>
    Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
        string? op,
        string? value,
        IReadOnlyList<string>? values,
        int take,
        CancellationToken cancellationToken);
}

/// <summary>Minimal Party identity required by consumers.</summary>
public sealed record PartyLookupResult(Guid PartyId, string Kind, string? DisplayName = null);
