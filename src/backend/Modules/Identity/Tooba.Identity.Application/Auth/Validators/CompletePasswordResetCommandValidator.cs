using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Validators;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>Transport-shape validation for <see cref="CompletePasswordResetCommand"/>.</summary>
public sealed class CompletePasswordResetCommandValidator : AbstractValidator<CompletePasswordResetCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public CompletePasswordResetCommandValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.ChallengeIdRequired);
        RuleFor(x => x.Secret)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.SecretRequired);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.NewPasswordRequired);
    }
}
