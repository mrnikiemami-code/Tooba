using FluentValidation;
using Tooba.Catalog.Application.Validators;
using Tooba.Catalog.Application.Variants.Commands;

namespace Tooba.Catalog.Application.Variants.Validators;

/// <summary>Transport shape for SetProductVariantAxesCommand.</summary>
public sealed class SetProductVariantAxesCommandValidator : AbstractValidator<SetProductVariantAxesCommand>
{
    /// <summary>Creates the validator.</summary>
    public SetProductVariantAxesCommandValidator()
    {
        RuleFor(x => x.Model.OrderedDefinitionIds)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.VariantAxesOrderedIdsRequired);
        RuleForEach(x => x.Model.OrderedDefinitionIds)
            .Must(id => id != Guid.Empty)
            .When(x => x.Model.OrderedDefinitionIds is not null)
            .WithErrorCode(CatalogValidationCodes.VariantAxisDefinitionIdRequired);
    }
}
