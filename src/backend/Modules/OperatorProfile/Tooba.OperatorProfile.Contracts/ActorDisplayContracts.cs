namespace Tooba.OperatorProfile.Contracts;

/// <summary>
/// Cross-module display identity of an acting user. Never carries permissions or credentials.
/// </summary>
/// <param name="UserId">Stable user identifier the projection belongs to.</param>
/// <param name="DisplayName">Preferred display name when the owning module has one.</param>
/// <param name="FirstName">Given name when recorded.</param>
/// <param name="LastName">Family name when recorded.</param>
/// <param name="Email">Contact email when the Identity owner supplies it.</param>
/// <param name="Mobile">Contact mobile when the Identity owner supplies it.</param>
public sealed record ActorDisplayProjection(
    Guid UserId,
    string? DisplayName = null,
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? Mobile = null);

/// <summary>Provides a stable cross-module actor display lookup owned by OperatorProfile.</summary>
public interface IActorDisplayLookup
{
    /// <summary>
    /// Batch display projections for acting users. Users without a profile row are omitted.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ActorDisplayProjection>> GetActorDisplaysAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken);
}
