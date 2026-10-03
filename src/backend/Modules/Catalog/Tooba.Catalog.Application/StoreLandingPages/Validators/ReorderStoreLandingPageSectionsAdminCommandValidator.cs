using FluentValidation;
using Tooba.Catalog.Application.StoreLandingPages.Commands;

namespace Tooba.Catalog.Application.StoreLandingPages.Validators;

/// <summary>Transport validator for ReorderStoreLandingPageSectionsAdminCommand.</summary>
public sealed class ReorderStoreLandingPageSectionsAdminCommandValidator : AbstractValidator<ReorderStoreLandingPageSectionsAdminCommand>
{
    /// <summary>Creates the validator.</summary>
    public ReorderStoreLandingPageSectionsAdminCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.SectionIds).NotNull();
    }
}