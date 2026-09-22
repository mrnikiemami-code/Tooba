namespace Tooba.BuildingBlocks.Security;

/// <summary>
/// Transport-neutral authenticated principal for module Application handlers.
/// Host binds this to the request session; modules must not reference Host session types.
/// </summary>
public interface ICurrentAuthenticatedUser
{
    /// <summary>Whether a live authenticated session is present.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Stable user id when authenticated; otherwise null.</summary>
    Guid? UserId { get; }
}
