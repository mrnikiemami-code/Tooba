using FluentValidation;
using Tooba.Catalog.Application.StoreLandingPages.Commands;

namespace Tooba.Catalog.Application.StoreLandingPages.Validators;

/// <summary>Transport validator for UpdateStoreLandingPageAdminCommand.</summary>
public sealed class UpdateStoreLandingPageAdminCommandValidator : AbstractValidator<UpdateStoreLandingPageAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateStoreLandingPageAdminCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Title).NotEmpty().WithErrorCode("landing.title.required");
        RuleFor(x => x.Body.Slug).NotEmpty().WithErrorCode("landing.slug.required");
    }
}