using FluentValidation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Validators;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Validators;

/// <summary>Transport-shape validation for <see cref="RequestOtpLoginCommand"/>.</summary>
public sealed class RequestOtpLoginCommandValidator : AbstractValidator<RequestOtpLoginCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public RequestOtpLoginCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithErrorCode(IdentityErrorCodes.ValidationFailed)
            .WithMessage(IdentityValidationCodes.IdentifierRequired);
    }
}
