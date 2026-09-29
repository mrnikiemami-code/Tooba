namespace Tooba.Party.Contracts;

/// <summary>
/// Party-owned Development-support request to rename development organizations by their
/// current display name; used only to keep demo copy stable on replay.
/// </summary>
public sealed record DevelopmentOrganizationRename(string FromDisplayName, string ToDisplayName);

/// <summary>
/// Party-owned Development-support capability used by module-owned development seeds
/// so they can resolve/create demo sellers and keep demo organization copy without
/// touching Party persistence.
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

    /// <summary>
    /// Creates the development organization when it does not exist yet, otherwise returns
    /// the existing one with the same display name.
    /// </summary>
    Task<Guid> EnsureDevelopmentOrganizationAsync(
        string displayName,
        string? legalName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Renames development organizations whose current display name matches a given source value.
    /// No-ops for names that are not present.
    /// </summary>
    Task EnsureDevelopmentOrganizationDisplayNamesAsync(
        IReadOnlyCollection<DevelopmentOrganizationRename> renames,
        CancellationToken cancellationToken);
}
