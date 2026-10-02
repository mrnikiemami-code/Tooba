using Tooba.Identity.Application.Models;
using Tooba.Identity.Application.Options;
using Tooba.Identity.Application.Ports;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Contacts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Actors;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Identity.Infrastructure.Adapters;

/// <summary>
/// Exposes the owning Identity contact lookup as the contracts-only actor contact lookup.
/// </summary>
internal sealed class ActorContactLookupAdapter(IIdentityContactLookup contacts) : IActorContactLookup
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ActorContactProjection>> GetActorContactsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var distinct = userIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (distinct.Length == 0)
        {
            return new Dictionary<Guid, ActorContactProjection>();
        }

        var snapshots = await contacts.GetContactsAsync(distinct, cancellationToken);
        return snapshots.ToDictionary(
            pair => pair.Key,
            pair => new ActorContactProjection(pair.Key, pair.Value.Email, pair.Value.Mobile));
    }
}
