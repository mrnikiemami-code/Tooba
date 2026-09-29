namespace Tooba.Party.Contracts;

/// <summary>
/// Party-owned Development-support capability used by the Catalog attribute-schema seed
/// so Catalog can resolve a demo seller without touching Party persistence.
/// </summary>
public interface IPartyDevelopmentSeedGateway
{
    /// <summary>
    /// Returns the earliest-created party id, or creates a development organization when none exists.
    /// </summary>
    Task<Guid> ResolveDevelopmentSellerPartyAsync(
        string displayName,
        string? legalName,
        CancellationToken cancellationToken);
}
