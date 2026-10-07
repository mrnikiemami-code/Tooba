namespace Tooba.Party.Contracts.Ports;

/// <summary>
/// Narrow Contracts-side Party development-seed directory for foreign module development seeds.
/// <para>
/// Party owns organization/party creation as a business capability; foreign Development seeds may
/// resolve or create demo sellers only through this stable port, never through
/// <c>Tooba.Party.Application</c>. Production runtime code has no reason to consume this port.
/// </para>
/// </summary>
public interface IPartyDevelopmentDirectory
{
    /// <summary>
    /// Creates a development organization when it does not exist yet, otherwise returns the existing
    /// party. Mirrors the Party-owned organization creation semantics without exposing the
    /// Application-internal directory.
    /// </summary>
    Task<PartyDevelopmentOrganization> EnsureDevelopmentOrganizationAsync(
        string displayName,
        string? legalName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Searches development organizations by display name substring and returns at most
    /// <paramref name="take"/> stable party ids (mirrors <c>IPartyLookup.SearchIdsByDisplayNameAsync</c>).
    /// </summary>
    Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
        string term,
        int take,
        CancellationToken cancellationToken);
}

/// <summary>Stable Contracts-side reference to a Party-owned development organization.</summary>
public sealed record PartyDevelopmentOrganization(Guid PartyId, string DisplayName, string? LegalName);
