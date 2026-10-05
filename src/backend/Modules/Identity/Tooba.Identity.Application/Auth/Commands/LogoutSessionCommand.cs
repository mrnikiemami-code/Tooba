using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Revokes a single session (bearer session id preferred, else current principal session).</summary>
public sealed record LogoutSessionCommand(Guid? BearerSessionId, Guid? CurrentSessionId, bool IsAuthenticated)
    : IRequest<Result>;

/// <summary>Handler for <see cref="LogoutSessionCommand"/>.</summary>
public sealed class LogoutSessionCommandHandler(IIdentityAuthenticationService auth)
    : IRequestHandler<LogoutSessionCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(LogoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (request.BearerSessionId is { } bearer)
        {
            await auth.RevokeSessionAsync(bearer, "http_logout", cancellationToken);
            return Result.Success();
        }

        if (!request.IsAuthenticated || request.CurrentSessionId is null)
        {
            return Result.Failure(new SemanticError(IdentityErrorCodes.SessionInvalid));
        }

        await auth.RevokeSessionAsync(request.CurrentSessionId.Value, "http_logout", cancellationToken);
        return Result.Success();
    }
}
