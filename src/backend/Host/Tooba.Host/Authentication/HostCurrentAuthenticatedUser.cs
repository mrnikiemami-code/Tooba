using Tooba.BuildingBlocks.Security;

namespace Tooba.Host;

/// <summary>Binds Host session to the shared authenticated-user abstraction.</summary>
internal sealed class HostCurrentAuthenticatedUser(CurrentAuthenticatedSession session) : ICurrentAuthenticatedUser
{
    /// <inheritdoc />
    public bool IsAuthenticated => session.IsAuthenticated;

    /// <inheritdoc />
    public Guid? UserId => session.UserId;
}
