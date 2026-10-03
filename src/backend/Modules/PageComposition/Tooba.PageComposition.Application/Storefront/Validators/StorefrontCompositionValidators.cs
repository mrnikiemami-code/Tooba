using FluentValidation;
using Tooba.PageComposition.Application.Storefront.Queries;
using Tooba.PageComposition.Contracts.Errors;

namespace Tooba.PageComposition.Application.Storefront.Validators;

/// <summary>اعتبارسنجی ترکیب عمومی خانه.</summary>
public sealed class GetHomeCompositionQueryValidator : AbstractValidator<GetHomeCompositionQuery>
{
    /// <summary>قاعده Tenant.</summary>
    public GetHomeCompositionQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.TenantMissing);
    }
}

/// <summary>
/// Marker validator for parameterless <see cref="GetSectionCatalogQuery"/>.
/// Classification: VALIDATOR_REQUIRED_PRESENT (no transport fields).
/// </summary>
public sealed class GetSectionCatalogQueryValidator : AbstractValidator<GetSectionCatalogQuery>
{
    /// <summary>Registers no field rules (parameterless query).</summary>
    public GetSectionCatalogQueryValidator()
    {
    }
}
