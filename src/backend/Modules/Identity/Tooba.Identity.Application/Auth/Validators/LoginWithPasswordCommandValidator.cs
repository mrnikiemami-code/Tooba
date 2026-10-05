using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Errors;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>
/// Transport-shape validation for <see cref="LoginWithPasswordCommand"/>.
/// Deliberately does not validate <c>IdentifierKind</c>: an unknown kind must still reach the handler
/// so the enumeration-safe collapse to <c>identity.authentication.failed</c> (401) is preserved.
/// </summary>
public sealed class LoginWithPasswordCommandValidator : AbstractValidator<LoginWithPasswordCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public LoginWithPasswordCommandValidator()
    {
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
