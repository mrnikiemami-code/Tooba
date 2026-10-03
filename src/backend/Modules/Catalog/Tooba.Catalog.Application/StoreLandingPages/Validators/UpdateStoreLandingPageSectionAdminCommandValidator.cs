using FluentValidation;
using Tooba.Catalog.Application.StoreLandingPages.Commands;

namespace Tooba.Catalog.Application.StoreLandingPages.Validators;

/// <summary>Transport validator for UpdateStoreLandingPageSectionAdminCommand.</summary>
public sealed class UpdateStoreLandingPageSectionAdminCommandValidator : AbstractValidator<UpdateStoreLandingPageSectionAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public UpdateStoreLandingPageSectionAdminCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
    }
}