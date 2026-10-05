using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Revokes every session for the authenticated user.</summary>
public sealed record LogoutAllSessionsCommand(Guid? UserId, bool IsAuthenticated) : IRequest<Result>;

/// <summary>Handler for <see cref="LogoutAllSessionsCommand"/>.</summary>
public sealed class LogoutAllSessionsCommandHandler(IIdentityAuthenticationService auth)
    : IRequestHandler<LogoutAllSessionsCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(LogoutAllSessionsCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsAuthenticated || request.UserId is null)
        {
            return Result.Failure(new SemanticError(IdentityErrorCodes.SessionInvalid));
        }

        await auth.RevokeAllSessionsAsync(request.UserId.Value, "http_logout_all", cancellationToken);
        return Result.Success();
    }
}
