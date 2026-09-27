using FluentValidation;
using Tooba.Catalog.Application.Tags.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.Tags.Validators;

/// <summary>Transport shape for CreateTagCommand.</summary>
public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    /// <summary>Creates the validator.</summary>
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Model.Code)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Model.Code))
            .WithErrorCode(CatalogValidationCodes.TagCodeTooLong);
        RuleFor(x => x.Model.Slug)
            .MaximumLength(128)
            .When(x => !string.IsNullOrWhiteSpace(x.Model.Slug))
            .WithErrorCode(CatalogValidationCodes.TagSlugTooLong);
        RuleFor(x => x.Model.LocalizedNames)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.TagLocalizedNamesRequired);
        RuleFor(x => x.Model.LocalizedNames)
            .Must(names => names.All(pair => !string.IsNullOrWhiteSpace(pair.Key)))
            .When(x => x.Model.LocalizedNames is not null)
            .WithErrorCode(CatalogValidationCodes.TagLocalizedNameLocaleRequired);
    }
}
