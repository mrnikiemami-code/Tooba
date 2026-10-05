using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Errors;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>Transport-shape validation for <see cref="RefreshAuthSessionCommand"/>.</summary>
public sealed class RefreshAuthSessionCommandValidator : AbstractValidator<RefreshAuthSessionCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public RefreshAuthSessionCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.RefreshTokenRequired);
    }
}
