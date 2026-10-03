using FluentValidation;
using Tooba.Catalog.Application.StoreLandingPages.Commands;

namespace Tooba.Catalog.Application.StoreLandingPages.Validators;

/// <summary>Transport validator for ReplaceStoreLandingPageCompositionAdminCommand.</summary>
public sealed class ReplaceStoreLandingPageCompositionAdminCommandValidator : AbstractValidator<ReplaceStoreLandingPageCompositionAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public ReplaceStoreLandingPageCompositionAdminCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Sections).NotNull();
    }
}