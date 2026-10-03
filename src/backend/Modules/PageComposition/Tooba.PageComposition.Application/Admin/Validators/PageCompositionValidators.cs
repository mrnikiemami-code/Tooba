using FluentValidation;
using Tooba.PageComposition.Application.Admin.Commands;
using Tooba.PageComposition.Application.Admin.Queries;
using Tooba.PageComposition.Contracts.Errors;

namespace Tooba.PageComposition.Application.Admin.Validators;

/// <summary>اعتبارسنجی افزودن section.</summary>
public sealed class AdminAddHomeSectionCommandValidator : AbstractValidator<AdminAddHomeSectionCommand>
{
    /// <summary>قواعد Tenant و SectionType.</summary>
    public AdminAddHomeSectionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.TenantMissing);
        RuleFor(x => x.Input.SectionType)
            .NotEmpty()
            .WithErrorCode(PageCompositionErrorCodes.SectionTypeRequired);
    }
}

/// <summary>اعتبارسنجی مرتب‌سازی sectionها.</summary>
public sealed class AdminReorderHomeSectionsCommandValidator : AbstractValidator<AdminReorderHomeSectionsCommand>
{
    /// <summary>قواعد Tenant و SectionIds.</summary>
    public AdminReorderHomeSectionsCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.TenantMissing);
        RuleFor(x => x.SectionIds)
            .NotNull()
            .Must(ids => ids.Count > 0)
            .WithErrorCode(PageCompositionErrorCodes.SectionIdsRequired);
    }
}

/// <summary>اعتبارسنجی به‌روزرسانی section.</summary>
public sealed class AdminUpdateHomeSectionCommandValidator : AbstractValidator<AdminUpdateHomeSectionCommand>
{
    /// <summary>قواعد Tenant و SectionId.</summary>
    public AdminUpdateHomeSectionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.TenantMissing);
        RuleFor(x => x.SectionId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.SectionIdRequired);
    }
}

/// <summary>اعتبارسنجی حذف section.</summary>
public sealed class AdminRemoveHomeSectionCommandValidator : AbstractValidator<AdminRemoveHomeSectionCommand>
{
    /// <summary>قواعد Tenant و SectionId.</summary>
    public AdminRemoveHomeSectionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.TenantMissing);
        RuleFor(x => x.SectionId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.SectionIdRequired);
    }
}

/// <summary>اعتبارسنجی بازگردانی پیش‌فرض.</summary>
public sealed class AdminRestoreDefaultHomeCompositionCommandValidator
    : AbstractValidator<AdminRestoreDefaultHomeCompositionCommand>
{
    /// <summary>قاعده Tenant.</summary>
    public AdminRestoreDefaultHomeCompositionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.TenantMissing);
    }
}

/// <summary>اعتبارسنجی نمای admin خانه.</summary>
public sealed class AdminGetHomeCompositionQueryValidator : AbstractValidator<AdminGetHomeCompositionQuery>
{
    /// <summary>قاعده Tenant.</summary>
    public AdminGetHomeCompositionQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithErrorCode(PageCompositionErrorCodes.TenantMissing);
    }
}
