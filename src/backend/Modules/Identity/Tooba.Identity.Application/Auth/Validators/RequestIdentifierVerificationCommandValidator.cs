using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Validators;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>Transport-shape validation for <see cref="RequestIdentifierVerificationCommand"/>.</summary>
public sealed class RequestIdentifierVerificationCommandValidator
    : AbstractValidator<RequestIdentifierVerificationCommand>
{
    /// <summary>Registers primitive-shape rules (actor/session gate remains in handler).</summary>
    public RequestIdentifierVerificationCommandValidator()
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
    }
}
