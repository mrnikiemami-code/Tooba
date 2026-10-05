using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Application.Auth.Models;
using Tooba.Identity.Application.Composition;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Rotates a refresh secret for an existing session handle.</summary>
public sealed record RefreshAuthSessionCommand(Guid SessionId, string? RefreshToken)
    : IRequest<Result<AuthSessionDto>>;

/// <summary>Handler for <see cref="RefreshAuthSessionCommand"/>.</summary>
public sealed class RefreshAuthSessionCommandHandler(
    IIdentityAuthenticationService auth,
    ILogger<RefreshAuthSessionCommandHandler> logger)
    : IRequestHandler<RefreshAuthSessionCommand, Result<AuthSessionDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthSessionDto>> Handle(RefreshAuthSessionCommand request, CancellationToken cancellationToken)
    {
        var outcome = await auth.RefreshSessionAsync(
            request.SessionId,
            request.RefreshToken ?? string.Empty,
            cancellationToken);
        var ticket = IdentityOperation.FromAuthResult(outcome, IdentityErrorCodes.SessionInvalid);
        if (ticket.IsFailure)
        {
            logger.LogInformation("identity.refresh.failed");
            return Result.Failure<AuthSessionDto>(ticket.Errors);
        }

        return Result.Success(AuthSessionMapper.FromTicket(ticket.Value));
    }
}
