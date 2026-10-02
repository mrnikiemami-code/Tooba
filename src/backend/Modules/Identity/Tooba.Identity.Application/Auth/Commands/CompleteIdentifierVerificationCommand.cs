using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Completes an identifier-verification challenge.</summary>
public sealed record CompleteIdentifierVerificationCommand(Guid ChallengeId, string? Secret)
    : IRequest<Result>;

/// <summary>Handler for <see cref="CompleteIdentifierVerificationCommand"/>.</summary>
public sealed class CompleteIdentifierVerificationCommandHandler(IIdentityCredentialLifecycle lifecycle)
    : IRequestHandler<CompleteIdentifierVerificationCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CompleteIdentifierVerificationCommand request, CancellationToken cancellationToken)
    {
        var outcome = await lifecycle.CompleteIdentifierVerificationAsync(
            request.ChallengeId,
            request.Secret ?? string.Empty,
            cancellationToken);
        return outcome == ChallengeConsumeOutcome.Succeeded
            ? Result.Success()
            : Result.Failure(new SemanticError(IdentityErrorCodes.ChallengeInvalid));
    }
}
