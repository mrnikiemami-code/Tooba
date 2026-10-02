using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Application.Auth.Models;
using Tooba.Identity.Application.Composition;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>
/// Password login. Invalid identifier kinds collapse to authentication failure (enumeration-safe).
/// </summary>
public sealed record LoginWithPasswordCommand(
    string? IdentifierKind,
    string? Identifier,
    string? Password) : IRequest<Result<AuthSessionDto>>;

/// <summary>Handler for <see cref="LoginWithPasswordCommand"/>.</summary>
public sealed class LoginWithPasswordCommandHandler(
    IIdentityAuthenticationService auth,
    ILogger<LoginWithPasswordCommandHandler> logger)
    : IRequestHandler<LoginWithPasswordCommand, Result<AuthSessionDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthSessionDto>> Handle(LoginWithPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.IdentifierKind, ignoreCase: true, out LoginIdentifierKind kind)
            || !Enum.IsDefined(kind))
        {
            logger.LogInformation("identity.login.failed");
            return Result.Failure<AuthSessionDto>(new SemanticError(IdentityErrorCodes.AuthenticationFailed));
        }

        var outcome = await auth.AuthenticateWithPasswordAsync(
            kind,
            request.Identifier ?? string.Empty,
            request.Password ?? string.Empty,
            cancellationToken);
        var ticket = IdentityOperation.FromAuthResult(outcome, IdentityErrorCodes.AuthenticationFailed);
        if (ticket.IsFailure)
        {
            logger.LogInformation("identity.login.failed");
            return Result.Failure<AuthSessionDto>(ticket.Errors);
        }

        logger.LogInformation("identity.login.succeeded");
        return Result.Success(AuthSessionMapper.FromTicket(ticket.Value));
    }
}
