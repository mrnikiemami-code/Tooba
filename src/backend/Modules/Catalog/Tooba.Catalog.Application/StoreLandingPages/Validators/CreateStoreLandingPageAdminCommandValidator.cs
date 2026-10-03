using FluentValidation;
using Tooba.Catalog.Application.StoreLandingPages.Commands;

namespace Tooba.Catalog.Application.StoreLandingPages.Validators;

/// <summary>Transport validator for CreateStoreLandingPageAdminCommand.</summary>
public sealed class CreateStoreLandingPageAdminCommandValidator : AbstractValidator<CreateStoreLandingPageAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public CreateStoreLandingPageAdminCommandValidator()
    {
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Title).NotEmpty().WithErrorCode("landing.title.required");
        RuleFor(x => x.Body.Slug).NotEmpty().WithErrorCode("landing.slug.required");
        RuleFor(x => x.Body.Locale).NotEmpty().WithErrorCode("landing.locale.required");
    }
}