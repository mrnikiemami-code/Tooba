using FluentValidation;
using Tooba.Catalog.Application.TemplateCatalog.Queries;

namespace Tooba.Catalog.Application.TemplateCatalog.Validators;

/// <summary>Transport shape for industry template preview key.</summary>
public sealed class GetIndustryTemplatePreviewQueryValidator : AbstractValidator<GetIndustryTemplatePreviewQuery>
{
    /// <summary>Creates the validator.</summary>
    public GetIndustryTemplatePreviewQueryValidator()
    {
        RuleFor(x => x.TemplateKey)
            .NotEmpty()
            .WithErrorCode("template_catalog.validation.template_key_required");
    }
}
