using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Application.Auth.Models;
using Tooba.Identity.Application.Composition;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Completes OTP login and issues the current session.</summary>
public sealed record CompleteOtpLoginCommand(string? Identifier, Guid ChallengeId, string? Secret)
    : IRequest<Result<AuthSessionDto>>;

/// <summary>Handler for <see cref="CompleteOtpLoginCommand"/>.</summary>
public sealed class CompleteOtpLoginCommandHandler(
    IIdentityOtpLoginService otpLogin,
    ILogger<CompleteOtpLoginCommandHandler> logger)
    : IRequestHandler<CompleteOtpLoginCommand, Result<AuthSessionDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthSessionDto>> Handle(CompleteOtpLoginCommand request, CancellationToken cancellationToken)
    {
        var outcome = await otpLogin.CompleteLoginAsync(
            request.Identifier ?? string.Empty,
            request.ChallengeId,
            request.Secret ?? string.Empty,
            cancellationToken);
        var ticket = IdentityOperation.FromAuthResult(outcome, IdentityErrorCodes.AuthenticationFailed);
        if (ticket.IsFailure)
        {
            logger.LogInformation("identity.otp_login.failed");
            return Result.Failure<AuthSessionDto>(ticket.Errors);
        }

        logger.LogInformation("identity.otp_login.succeeded");
        return Result.Success(AuthSessionMapper.FromTicket(ticket.Value));
    }
}
