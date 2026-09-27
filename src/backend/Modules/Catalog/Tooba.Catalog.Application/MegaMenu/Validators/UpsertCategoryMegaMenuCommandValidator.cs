using FluentValidation;
using Tooba.Catalog.Application.MegaMenu.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.MegaMenu.Validators;

/// <summary>Transport shape for UpsertCategoryMegaMenuCommand.</summary>
public sealed class UpsertCategoryMegaMenuCommandValidator : AbstractValidator<UpsertCategoryMegaMenuCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpsertCategoryMegaMenuCommandValidator()
    {
        RuleFor(x => x.Input)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.MegaMenuBindingInputRequired);
        RuleFor(x => x.Input.TitleOverride)
            .MaximumLength(256)
            .When(x => x.Input is not null && !string.IsNullOrWhiteSpace(x.Input.TitleOverride))
            .WithErrorCode(CatalogValidationCodes.MegaMenuTitleOverrideTooLong);
        RuleFor(x => x.Input.BadgeText)
            .MaximumLength(128)
            .When(x => x.Input is not null && !string.IsNullOrWhiteSpace(x.Input.BadgeText))
            .WithErrorCode(CatalogValidationCodes.MegaMenuBadgeTextTooLong);
        RuleFor(x => x.Input.ShortLabel)
            .MaximumLength(128)
            .When(x => x.Input is not null && !string.IsNullOrWhiteSpace(x.Input.ShortLabel))
            .WithErrorCode(CatalogValidationCodes.MegaMenuShortLabelTooLong);
    }
}
