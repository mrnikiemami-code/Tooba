using FluentValidation;
using Tooba.Catalog.Application.ProductIdentity.Commands;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain.Rules;

namespace Tooba.Catalog.Application.ProductIdentity.Validators;

/// <summary>Validates variant physical-specification override fields.</summary>
public sealed class UpdateVariantPhysicalSpecificationCommandValidator
    : AbstractValidator<UpdateVariantPhysicalSpecificationCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateVariantPhysicalSpecificationCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.WeightGrams)
            .Must(BeValidOptionalMeasurement)
            .WithErrorCode(CatalogErrorCodes.WorkspacePhysicalSpecificationRejected);
        RuleFor(x => x.Model.LengthCentimeters)
            .Must(BeValidOptionalDimension)
            .WithErrorCode(CatalogErrorCodes.WorkspacePhysicalSpecificationRejected);
        RuleFor(x => x.Model.WidthCentimeters)
            .Must(BeValidOptionalDimension)
            .WithErrorCode(CatalogErrorCodes.WorkspacePhysicalSpecificationRejected);
        RuleFor(x => x.Model.HeightCentimeters)
            .Must(BeValidOptionalDimension)
            .WithErrorCode(CatalogErrorCodes.WorkspacePhysicalSpecificationRejected);
    }

    private static bool BeValidOptionalMeasurement(decimal? value)
    {
        if (value is null)
        {
            return true;
        }

        try
        {
            CatalogPhysicalSpecification.ValidateOrThrow(value, null, null, null);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool BeValidOptionalDimension(decimal? value)
    {
        if (value is null)
        {
            return true;
        }

        try
        {
            CatalogPhysicalSpecification.ValidateOrThrow(null, value, value, value);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
