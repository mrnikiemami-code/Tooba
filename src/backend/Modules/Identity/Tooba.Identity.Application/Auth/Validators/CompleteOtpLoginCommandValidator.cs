using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Errors;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>Transport-shape validation for <see cref="CompleteOtpLoginCommand"/>.</summary>
public sealed class CompleteOtpLoginCommandValidator : AbstractValidator<CompleteOtpLoginCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public CompleteOtpLoginCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.IdentifierRequired);
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
