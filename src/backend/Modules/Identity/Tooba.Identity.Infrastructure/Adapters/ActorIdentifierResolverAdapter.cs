using Tooba.Identity.Application;
using Tooba.Identity.Contracts;
using Tooba.Identity.Domain;

namespace Tooba.Identity.Infrastructure.Adapters;

/// <summary>
/// Exposes the owning Identity authentication lookup as the contracts-only neutral identifier resolver.
/// </summary>
internal sealed class ActorIdentifierResolverAdapter(IIdentityAuthenticationService authentication)
    : IActorIdentifierResolver
{
    /// <inheritdoc />
    public async Task<Guid?> FindUserIdAsync(
        ActorIdentifierKind kind,
        string identifier,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var loginKind = kind switch
        {
            ActorIdentifierKind.Email => LoginIdentifierKind.Email,
            ActorIdentifierKind.Phone => LoginIdentifierKind.Phone,
            ActorIdentifierKind.Username => LoginIdentifierKind.Username,
            _ => (LoginIdentifierKind?)null,
        };

        if (loginKind is not { } resolvedKind)
        {
            return null;
        }

        return await authentication.FindUserIdByIdentifierAsync(resolvedKind, identifier, cancellationToken);
    }
}
