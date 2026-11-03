using FluentValidation;
using Tooba.Catalog.Application.ProductIdentity.Commands;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain.Rules;

namespace Tooba.Catalog.Application.ProductIdentity.Validators;

/// <summary>Validates typed physical-specification write fields before persistence.</summary>
public sealed class UpdateProductPhysicalSpecificationCommandValidator
    : AbstractValidator<UpdateProductPhysicalSpecificationCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateProductPhysicalSpecificationCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.WeightGrams)
            .Must(BeValidOptionalMeasurement)
            .WithErrorCode(CatalogErrorCodes.WorkspacePhysicalSpecificationRejected)
            .WithMessage("WeightGrams must be null or a positive value within bounds.");
        RuleFor(x => x.Model.LengthCentimeters)
            .Must(BeValidOptionalDimension)
            .WithErrorCode(CatalogErrorCodes.WorkspacePhysicalSpecificationRejected)
            .WithMessage("LengthCentimeters must be null or a positive value within bounds.");
        RuleFor(x => x.Model.WidthCentimeters)
            .Must(BeValidOptionalDimension)
            .WithErrorCode(CatalogErrorCodes.WorkspacePhysicalSpecificationRejected)
            .WithMessage("WidthCentimeters must be null or a positive value within bounds.");
        RuleFor(x => x.Model.HeightCentimeters)
            .Must(BeValidOptionalDimension)
            .WithErrorCode(CatalogErrorCodes.WorkspacePhysicalSpecificationRejected)
            .WithMessage("HeightCentimeters must be null or a positive value within bounds.");
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
