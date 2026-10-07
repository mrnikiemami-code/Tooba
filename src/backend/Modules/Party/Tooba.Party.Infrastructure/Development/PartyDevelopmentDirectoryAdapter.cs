using Tooba.Party.Application.Ports;
using Tooba.Party.Contracts.Ports;

namespace Tooba.Party.Infrastructure.Development;

/// <summary>
/// Party-owned Contracts adapter exposing the narrow development-seed surface
/// (<see cref="IPartyDevelopmentDirectory"/>) so foreign Development seeds resolve/create demo
/// organizations through Party.Contracts only — never through Party.Application. Production
/// runtime behavior is unchanged; this adapter delegates to the same Party-owned directory.
/// </summary>
public sealed class PartyDevelopmentDirectoryAdapter(IPartyDirectory directory) : IPartyDevelopmentDirectory
{
    /// <inheritdoc />
    public async Task<PartyDevelopmentOrganization> EnsureDevelopmentOrganizationAsync(
        string displayName,
        string? legalName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        var created = await directory.CreateOrganizationAsync(displayName, legalName, cancellationToken);
        return new PartyDevelopmentOrganization(created.PartyId, created.DisplayName, created.LegalName);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
        string term,
        int take,
        CancellationToken cancellationToken) =>
        ((IPartyLookup)directory).SearchIdsByDisplayNameAsync(term, take, cancellationToken);
}
