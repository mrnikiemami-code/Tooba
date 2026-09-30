using FluentValidation;
using Tooba.PageComposition.Application.Commands;

namespace Tooba.PageComposition.Application.Validators;

/// <summary>کدهای شکل انتقال PageComposition.</summary>
public static class PageCompositionValidationCodes
{
    public const string SectionTypeRequired = "page-composition.sectionType.required";
    public const string SectionIdsRequired = "page-composition.sectionIds.required";
}

/// <summary>اعتبارسنجی افزودن section.</summary>
public sealed class AdminAddHomeSectionCommandValidator : AbstractValidator<AdminAddHomeSectionCommand>
{
    public AdminAddHomeSectionCommandValidator()
    {
        RuleFor(x => x.Input.SectionType)
            .NotEmpty()
            .WithErrorCode(PageCompositionValidationCodes.SectionTypeRequired);
    }
}

/// <summary>اعتبارسنجی مرتب‌سازی sectionها.</summary>
public sealed class AdminReorderHomeSectionsCommandValidator : AbstractValidator<AdminReorderHomeSectionsCommand>
{
    public AdminReorderHomeSectionsCommandValidator()
    {
        RuleFor(x => x.SectionIds)
            .NotNull()
            .WithErrorCode(PageCompositionValidationCodes.SectionIdsRequired);
    }
}
