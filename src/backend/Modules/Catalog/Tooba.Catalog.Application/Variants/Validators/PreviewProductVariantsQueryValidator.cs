using FluentValidation;
using Tooba.Catalog.Application.Validators;
using Tooba.Catalog.Application.Variants.Queries;

namespace Tooba.Catalog.Application.Variants.Validators;

/// <summary>Transport shape for PreviewProductVariantsQuery.</summary>
public sealed class PreviewProductVariantsQueryValidator : AbstractValidator<PreviewProductVariantsQuery>
{
    /// <summary>Creates the validator.</summary>
    public PreviewProductVariantsQueryValidator()
    {
        RuleFor(x => x.Model.SelectedAxes)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.VariantSelectedAxesRequired);
        RuleForEach(x => x.Model.SelectedAxes)
            .ChildRules(axis =>
            {
                axis.RuleFor(a => a.DefinitionId)
                    .NotEmpty()
                    .WithErrorCode(CatalogValidationCodes.VariantAxisDefinitionIdRequired);
            })
            .When(x => x.Model.SelectedAxes is not null);
    }
}
