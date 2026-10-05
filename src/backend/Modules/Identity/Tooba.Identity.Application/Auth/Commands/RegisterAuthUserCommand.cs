using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Identity.Application.Auth.Models;
using Tooba.Identity.Application.Composition;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Errors;

namespace Tooba.Identity.Application.Auth.Commands;

/// <summary>Registers a user with a typed identifier and password credential.</summary>
public sealed record RegisterAuthUserCommand(
    string? IdentifierKind,
    string? Identifier,
    string? Password) : IRequest<Result<AuthRegisterDto>>;

/// <summary>Handler for <see cref="RegisterAuthUserCommand"/>.</summary>
public sealed class RegisterAuthUserCommandHandler(
    IIdentityAuthenticationService auth,
    ILogger<RegisterAuthUserCommandHandler> logger)
    : IRequestHandler<RegisterAuthUserCommand, Result<AuthRegisterDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthRegisterDto>> Handle(RegisterAuthUserCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.IdentifierKind, ignoreCase: true, out LoginIdentifierKind kind)
            || !Enum.IsDefined(kind))
        {
            return Result.Failure<AuthRegisterDto>(new SemanticError(IdentityErrorCodes.ValidationFailed));
        }

        var result = await IdentityOperation.ExecuteAsync(() => auth.RegisterAsync(
            new RegisterUserCommand
            {
                IdentifierKind = kind,
                Identifier = request.Identifier ?? string.Empty,
                Password = request.Password ?? string.Empty,
            },
            cancellationToken));
        if (result.IsFailure)
        {
            return Result.Failure<AuthRegisterDto>(result.Errors);
        }

        logger.LogInformation("identity.register.succeeded");
        return Result.Success(new AuthRegisterDto(result.Value.UserId));
    }
}
