using FluentValidation;
using Tooba.Catalog.Application.ProductMedia.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.ProductMedia.Validators;

/// <summary>Transport shape for ReorderProductMediaCommand — collection must be present.</summary>
public sealed class ReorderProductMediaCommandValidator : AbstractValidator<ReorderProductMediaCommand>
{
    /// <summary>Creates the validator.</summary>
    public ReorderProductMediaCommandValidator()
    {
        RuleFor(x => x.Model.OrderedMediaAssetIds)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.ProductMediaOrderedIdsRequired);
    }
}
