using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Validators;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>Transport-shape validation for <see cref="CompleteIdentifierVerificationCommand"/>.</summary>
public sealed class CompleteIdentifierVerificationCommandValidator
    : AbstractValidator<CompleteIdentifierVerificationCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public CompleteIdentifierVerificationCommandValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.ChallengeIdRequired);
        RuleFor(x => x.Secret)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.SecretRequired);
    }
}
