namespace Tooba.Identity.Contracts;

/// <summary>
/// Neutral identifier kinds usable for cross-module actor resolution.
/// Owned by Identity; callers never see Identity credentials or login internals.
/// </summary>
public enum ActorIdentifierKind
{
    /// <summary>Display email identifier.</summary>
    Email = 0,

    /// <summary>Display mobile identifier.</summary>
    Phone = 1,

    /// <summary>Login username identifier.</summary>
    Username = 2,
}

/// <summary>
/// Resolves a neutral actor identifier to its stable user id. Owned by Identity.
/// </summary>
public interface IActorIdentifierResolver
{
    /// <summary>
    /// Resolves the identifier to a user id, or <c>null</c> when no user owns it.
    /// </summary>
    /// <param name="kind">Neutral identifier kind.</param>
    /// <param name="identifier">Raw identifier value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Guid?> FindUserIdAsync(
        ActorIdentifierKind kind,
        string identifier,
        CancellationToken cancellationToken);
}
