using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Application.Auth.Models;
using Tooba.Identity.Application.Composition;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Requests a customer OTP login challenge (enumeration-safe).</summary>
public sealed record RequestOtpLoginCommand(string? Identifier) : IRequest<Result<AuthOtpChallengeDto>>;

/// <summary>Handler for <see cref="RequestOtpLoginCommand"/>.</summary>
public sealed class RequestOtpLoginCommandHandler(IIdentityOtpLoginService otpLogin)
    : IRequestHandler<RequestOtpLoginCommand, Result<AuthOtpChallengeDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthOtpChallengeDto>> Handle(RequestOtpLoginCommand request, CancellationToken cancellationToken) =>
        await IdentityOperation.ExecuteAsync(async () =>
        {
            var handle = await otpLogin.RequestLoginAsync(request.Identifier ?? string.Empty, cancellationToken);
            return new AuthOtpChallengeDto(true, handle.ChallengeId);
        });
}
