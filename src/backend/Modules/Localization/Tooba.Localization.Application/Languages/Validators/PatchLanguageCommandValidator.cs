using FluentValidation;
using Tooba.Localization.Application.Languages.Commands;
using Tooba.Localization.Contracts.Errors;

namespace Tooba.Localization.Application.Languages.Validators;

/// <summary>Transport-shape validation for <see cref="PatchLanguageCommand"/>.</summary>
public sealed class PatchLanguageCommandValidator : AbstractValidator<PatchLanguageCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public PatchLanguageCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithErrorCode(LanguageErrorCodes.InvalidCode);
    }
}
