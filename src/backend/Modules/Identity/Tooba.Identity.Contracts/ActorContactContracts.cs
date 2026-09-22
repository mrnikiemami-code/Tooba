namespace Tooba.Identity.Contracts;

/// <summary>Display-only contact identity of a user. Not a credential and not an auth claim.</summary>
/// <param name="UserId">Stable user identifier the projection belongs to.</param>
/// <param name="Email">Verified display email when recorded.</param>
/// <param name="Mobile">Verified display mobile when recorded.</param>
public sealed record ActorContactProjection(Guid UserId, string? Email = null, string? Mobile = null);

/// <summary>Provides a stable cross-module actor contact lookup owned by Identity.</summary>
public interface IActorContactLookup
{
    /// <summary>
    /// Batch contact projections for acting users. Users without a contact identifier are omitted.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ActorContactProjection>> GetActorContactsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken);
}
