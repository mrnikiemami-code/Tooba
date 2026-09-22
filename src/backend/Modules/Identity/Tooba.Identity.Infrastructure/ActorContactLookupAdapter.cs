using Tooba.Identity.Application;
using Tooba.Identity.Contracts;

namespace Tooba.Identity.Infrastructure;

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
