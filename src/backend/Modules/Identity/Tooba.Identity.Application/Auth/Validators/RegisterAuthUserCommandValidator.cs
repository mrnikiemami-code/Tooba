using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Auth.Validators;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Errors;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>Transport-shape validation for <see cref="RegisterAuthUserCommand"/>.</summary>
public sealed class RegisterAuthUserCommandValidator : AbstractValidator<RegisterAuthUserCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public RegisterAuthUserCommandValidator()
    {
        RuleFor(x => x.IdentifierKind)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .Must(k => Enum.TryParse<LoginIdentifierKind>(k, ignoreCase: true, out var kind) && Enum.IsDefined(kind))
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.IdentifierKindRequired);
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.IdentifierRequired);
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.PasswordRequired);
    }
}
