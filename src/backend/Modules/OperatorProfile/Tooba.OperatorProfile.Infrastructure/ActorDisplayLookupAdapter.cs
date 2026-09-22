using Tooba.OperatorProfile.Application;
using Tooba.OperatorProfile.Contracts;

namespace Tooba.OperatorProfile.Infrastructure;

/// <summary>
/// Exposes the owning operator profile directory as the contracts-only actor display lookup.
/// </summary>
internal sealed class ActorDisplayLookupAdapter(IOperatorProfileDirectory directory) : IActorDisplayLookup
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ActorDisplayProjection>> GetActorDisplaysAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var distinct = userIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (distinct.Length == 0)
        {
            return new Dictionary<Guid, ActorDisplayProjection>();
        }

        var profiles = await directory.GetManyAsync(distinct, cancellationToken);
        return profiles.ToDictionary(
            pair => pair.Key,
            pair => new ActorDisplayProjection(
                pair.Key,
                pair.Value.DisplayName,
                pair.Value.FirstName,
                pair.Value.LastName));
    }
}
