using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Application.Auth.Models;
using Tooba.Identity.Application.Composition;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Issues an identifier-verification challenge for the authenticated user.</summary>
public sealed record RequestIdentifierVerificationCommand(
    Guid UserId,
    bool IsAuthenticated,
    string? IdentifierKind,
    string? Identifier) : IRequest<Result<AuthAcceptedDto>>;

/// <summary>Handler for <see cref="RequestIdentifierVerificationCommand"/>.</summary>
public sealed class RequestIdentifierVerificationCommandHandler(IIdentityCredentialLifecycle lifecycle)
    : IRequestHandler<RequestIdentifierVerificationCommand, Result<AuthAcceptedDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthAcceptedDto>> Handle(
        RequestIdentifierVerificationCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.IsAuthenticated)
        {
            return Result.Failure<AuthAcceptedDto>(new SemanticError(IdentityErrorCodes.SessionInvalid));
        }

        if (!Enum.TryParse(request.IdentifierKind, ignoreCase: true, out LoginIdentifierKind kind)
            || !Enum.IsDefined(kind))
        {
            return Result.Failure<AuthAcceptedDto>(new SemanticError(IdentityErrorCodes.ValidationFailed));
        }

        return await IdentityOperation.ExecuteAsync(async () =>
        {
            await lifecycle.IssueIdentifierVerificationAsync(
                request.UserId,
                kind,
                request.Identifier ?? string.Empty,
                cancellationToken);
            return new AuthAcceptedDto(true);
        });
    }
}
