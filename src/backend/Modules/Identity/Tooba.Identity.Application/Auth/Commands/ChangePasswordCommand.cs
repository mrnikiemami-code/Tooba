using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Application.Composition;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Changes password after current-password proof; revokes sessions on success.</summary>
public sealed record ChangePasswordCommand(
    Guid UserId,
    bool IsAuthenticated,
    string? CurrentPassword,
    string? NewPassword) : IRequest<Result>;

/// <summary>Handler for <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandHandler(IIdentityAuthenticationService auth)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsAuthenticated)
        {
            return Result.Failure(new SemanticError(IdentityErrorCodes.SessionInvalid));
        }

        try
        {
            await auth.ChangePasswordAsync(
                request.UserId,
                request.CurrentPassword ?? string.Empty,
                request.NewPassword ?? string.Empty,
                cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(new SemanticError(IdentityErrorCodes.PasswordChangeFailed));
        }
        catch (ArgumentException)
        {
            return Result.Failure(new SemanticError(IdentityErrorCodes.ValidationFailed));
        }
    }
}
