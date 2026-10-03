using FluentValidation;
using Tooba.OperatorProfile.Application.Admin.Commands;
using Tooba.OperatorProfile.Contracts.Errors;
using DomainProfile = Tooba.OperatorProfile.Domain.Aggregates.OperatorProfile;

namespace Tooba.OperatorProfile.Application.Admin.Validators;

/// <summary>اعتبارسنجی شکل حمل‌ونقل پروفایل اپراتور.</summary>
public sealed class UpsertOperatorProfileCommandValidator : AbstractValidator<UpsertOperatorProfileCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public UpsertOperatorProfileCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode(OperatorProfileErrorCodes.ActorRequired);
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(DomainProfile.DisplayNameMinLength)
            .MaximumLength(DomainProfile.DisplayNameMaxLength)
            .WithErrorCode(OperatorProfileErrorCodes.InvalidDisplayName);
        RuleFor(x => x.FirstName!)
            .MaximumLength(DomainProfile.NamePartMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName))
            .WithErrorCode(OperatorProfileErrorCodes.InvalidFirstName);
        RuleFor(x => x.LastName!)
            .MaximumLength(DomainProfile.NamePartMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.LastName))
            .WithErrorCode(OperatorProfileErrorCodes.InvalidLastName);
        RuleFor(x => x.Bio!)
            .MaximumLength(DomainProfile.BioMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Bio))
            .WithErrorCode(OperatorProfileErrorCodes.InvalidBio);
    }
}
