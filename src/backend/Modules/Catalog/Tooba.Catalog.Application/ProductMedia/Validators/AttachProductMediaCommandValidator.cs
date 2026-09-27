using FluentValidation;
using Tooba.Catalog.Application.ProductMedia.Commands;
using Tooba.Catalog.Contracts.Errors;

namespace Tooba.Catalog.Application.ProductMedia.Validators;

/// <summary>Transport shape for AttachProductMediaCommand — empty MediaAssetId.</summary>
public sealed class AttachProductMediaCommandValidator : AbstractValidator<AttachProductMediaCommand>
{
    /// <summary>Creates the validator.</summary>
    public AttachProductMediaCommandValidator()
    {
        RuleFor(x => x.Model.MediaAssetId)
            .NotEmpty()
            .WithErrorCode(CatalogErrorCodes.WorkspaceMediaAssetMissing);
    }
}
