using FluentValidation;
using Tooba.Catalog.Application.StoreLandingPages.Commands;

namespace Tooba.Catalog.Application.StoreLandingPages.Validators;

/// <summary>Transport validator for AddStoreLandingPageSectionAdminCommand.</summary>
public sealed class AddStoreLandingPageSectionAdminCommandValidator : AbstractValidator<AddStoreLandingPageSectionAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public AddStoreLandingPageSectionAdminCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.SectionType).NotEmpty().WithErrorCode("landing.section.type.required");
    }
}