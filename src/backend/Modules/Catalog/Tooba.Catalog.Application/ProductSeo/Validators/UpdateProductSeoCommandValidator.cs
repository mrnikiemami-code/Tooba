using FluentValidation;
using Tooba.Catalog.Application.ProductSeo.Commands;
using Tooba.Catalog.Application.Validators;

namespace Tooba.Catalog.Application.ProductSeo.Validators;

/// <summary>
/// Transport shape for UpdateProductSeoCommand — Locale required on body (blank still normalizes in Domain).
/// Does not duplicate slug, uniqueness, concurrency, or readiness rules.
/// </summary>
public sealed class UpdateProductSeoCommandValidator : AbstractValidator<UpdateProductSeoCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateProductSeoCommandValidator()
    {
        RuleFor(x => x.Model.Locale)
            .NotNull()
            .WithErrorCode(CatalogValidationCodes.ProductSeoLocaleRequired);
    }
}
