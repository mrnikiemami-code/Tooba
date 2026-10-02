using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Application.Auth.Models;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>
/// Enumeration-safe password-reset request. Invalid kinds are ignored; response always accepts.
/// </summary>
public sealed record RequestPasswordResetCommand(string? IdentifierKind, string? Identifier)
    : IRequest<Result<AuthAcceptedDto>>;

/// <summary>Handler for <see cref="RequestPasswordResetCommand"/>.</summary>
public sealed class RequestPasswordResetCommandHandler(IIdentityCredentialLifecycle lifecycle)
    : IRequestHandler<RequestPasswordResetCommand, Result<AuthAcceptedDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthAcceptedDto>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        if (Enum.TryParse(request.IdentifierKind, ignoreCase: true, out LoginIdentifierKind kind)
            && Enum.IsDefined(kind))
        {
            await lifecycle.RequestPasswordResetAsync(kind, request.Identifier ?? string.Empty, cancellationToken);
        }

        return Result.Success(new AuthAcceptedDto(true));
    }
}
