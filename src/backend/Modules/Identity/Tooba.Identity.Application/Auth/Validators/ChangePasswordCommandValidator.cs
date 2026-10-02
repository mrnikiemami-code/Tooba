using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Validators;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>Transport-shape validation for <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>Registers primitive-shape rules (session gate remains in handler).</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.CurrentPasswordRequired);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.NewPasswordRequired);
    }
}
