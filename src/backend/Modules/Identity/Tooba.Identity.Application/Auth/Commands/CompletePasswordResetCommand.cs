using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Completes a one-time password-reset challenge.</summary>
public sealed record CompletePasswordResetCommand(Guid ChallengeId, string? Secret, string? NewPassword)
    : IRequest<Result>;

/// <summary>Handler for <see cref="CompletePasswordResetCommand"/>.</summary>
public sealed class CompletePasswordResetCommandHandler(IIdentityCredentialLifecycle lifecycle)
    : IRequestHandler<CompletePasswordResetCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CompletePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var outcome = await lifecycle.CompletePasswordResetAsync(
            request.ChallengeId,
            request.Secret ?? string.Empty,
            request.NewPassword ?? string.Empty,
            cancellationToken);
        return outcome == ChallengeConsumeOutcome.Succeeded
            ? Result.Success()
            : Result.Failure(new SemanticError(IdentityErrorCodes.ChallengeInvalid));
    }
}
