using FluentValidation;
using Tooba.Catalog.Application.Validators;
using Tooba.Catalog.Application.Variants.Commands;
using Tooba.Catalog.Application.Variants.Models;

namespace Tooba.Catalog.Application.Variants.Validators;

/// <summary>Transport shape for ApplyProductVariantMatrixCommand including patch status parse.</summary>
public sealed class ApplyProductVariantMatrixCommandValidator : AbstractValidator<ApplyProductVariantMatrixCommand>
{
    /// <summary>Creates the validator.</summary>
    public ApplyProductVariantMatrixCommandValidator()
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
        RuleForEach(x => x.Model.VariantPatches)
            .ChildRules(patch =>
            {
                patch.RuleFor(p => p.Status)
                    .Must(status => ProductVariantPatchStatusMapper.TryParse(status, out _))
                    .WithErrorCode(CatalogValidationCodes.VariantPatchStatusInvalid);
            })
            .When(x => x.Model.VariantPatches is not null);
    }
}
