using MediatR;
using FluentValidation;
using Tooba.Catalog.Domain;
using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Application;

/// <summary>
/// درز موقت عضویت کمپین برای اعتبارسنجی ارجاع بخش Landing (بدون وابستگی Catalog.Application به Promotion).
/// </summary>
public interface IStoreLandingExternalReferenceGate
{
    /// <summary>آیا کمپین به Store تعلق دارد؟</summary>
    Task<bool> CampaignBelongsToStoreAsync(Guid campaignId, Guid storeId, CancellationToken cancellationToken);
}

/// <summary>نوشتن Landing/Page Composition روی مالک فعلی Catalog (orchestration موقت تا جابه‌جایی BC).</summary>
public interface IStoreLandingPageDirectory
{
    /// <summary>صفحه پیش‌نویس می‌سازد.</summary>
    Task<StoreLandingPage> CreateAsync(StoreLandingPageWriteModel model, CancellationToken cancellationToken);

    /// <summary>صفحه را به‌روز می‌کند.</summary>
    Task<StoreLandingPage> UpdateAsync(Guid pageId, StoreLandingPageWriteModel model, CancellationToken cancellationToken);

    /// <summary>وضعیت انتشار را عوض می‌کند.</summary>
    Task<StoreLandingPage> SetStatusAsync(Guid pageId, string? status, CancellationToken cancellationToken);

    /// <summary>ارجاع خانه را اتمیک می‌نویسد.</summary>
    Task<StoreHomeWriteResult> SetHomeAsync(Guid? pageId, CancellationToken cancellationToken);

    /// <summary>بخش اضافه می‌کند.</summary>
    Task<StoreLandingPageSection> AddSectionAsync(Guid pageId, StoreLandingPageSectionWriteModel model, CancellationToken cancellationToken);

    /// <summary>ترکیب بخش‌ها را جایگزین می‌کند.</summary>
    Task<IReadOnlyList<StoreLandingPageSection>> ReplaceCompositionAsync(Guid pageId, IReadOnlyList<StoreLandingPageSectionWriteModel> sections, CancellationToken cancellationToken);

    /// <summary>پیکربندی بخش را به‌روز می‌کند.</summary>
    Task<StoreLandingPageSection> UpdateSectionAsync(Guid pageId, Guid sectionId, StoreLandingPageSectionWriteModel model, CancellationToken cancellationToken);

    /// <summary>فعال/غیرفعال بخش.</summary>
    Task<StoreLandingPageSection> SetSectionEnabledAsync(Guid pageId, Guid sectionId, bool enabled, CancellationToken cancellationToken);

    /// <summary>ترتیب بخش‌ها.</summary>
    Task<IReadOnlyList<StoreLandingPageSection>> ReorderSectionsAsync(Guid pageId, IReadOnlyList<Guid> sectionIds, CancellationToken cancellationToken);

    /// <summary>حذف صفحه.</summary>
    Task<StoreLandingPage> DeletePageAsync(Guid pageId, CancellationToken cancellationToken);

    /// <summary>حذف بخش.</summary>
    Task DeleteSectionAsync(Guid pageId, Guid sectionId, CancellationToken cancellationToken);
}

/// <summary>مدل نوشتن صفحه (مرز Application).</summary>
/// <param name="Title">عنوان.</param>
/// <param name="Slug">اسلاگ.</param>
/// <param name="Locale">لوکیل.</param>
/// <param name="SeoTitle">عنوان SEO.</param>
/// <param name="SeoDescription">توضیح SEO.</param>
/// <param name="PageType">نوع صفحه.</param>
/// <param name="RobotsIndex">ایندکس ربات.</param>
/// <param name="RobotsFollow">دنبال‌کردن ربات.</param>
/// <param name="CanonicalUrl">Canonical.</param>
/// <param name="OgTitle">OG عنوان.</param>
/// <param name="OgDescription">OG توضیح.</param>
/// <param name="OgImageUrl">OG تصویر.</param>
/// <param name="PrimaryH1">H1 اصلی.</param>
public sealed record StoreLandingPageWriteModel(
    string? Title,
    string? Slug,
    string? Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? PageType,
    bool? RobotsIndex,
    bool? RobotsFollow,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? PrimaryH1);

/// <summary>مدل نوشتن بخش.</summary>
/// <param name="SectionType">نوع بخش.</param>
/// <param name="ConfigJson">پیکربندی JSON.</param>
/// <param name="Config">پیکربندی جایگزین.</param>
/// <param name="IsEnabled">فعال؟</param>
/// <param name="InsertAt">ایندکس درج.</param>
public sealed record StoreLandingPageSectionWriteModel(
    string? SectionType,
    string? ConfigJson,
    string? Config,
    bool? IsEnabled,
    int? InsertAt);

/// <summary>نتیجهٔ SetHome برای invalidation در Host.</summary>
/// <param name="HomePageId">خانه فعال.</param>
/// <param name="InvalidateLocale">لوکیل برای پاک‌کردن کش.</param>
/// <param name="InvalidateSlug">اسلاگ برای پاک‌کردن کش.</param>
/// <param name="InvalidateHome">پاک‌کردن کش خانه.</param>
public sealed record StoreHomeWriteResult(Guid? HomePageId, string? InvalidateLocale, string? InvalidateSlug, bool InvalidateHome);

/// <summary>فرمان ایجاد صفحه Landing.</summary>
/// <param name="Model">مدل نوشتن.</param>
public sealed record CreateStoreLandingPageCommand(StoreLandingPageWriteModel Model) : IRequest<StoreLandingPage>;

/// <summary>فرمان به‌روزرسانی صفحه.</summary>
/// <param name="PageId">شناسه صفحه.</param>
/// <param name="Model">مدل نوشتن.</param>
public sealed record UpdateStoreLandingPageCommand(Guid PageId, StoreLandingPageWriteModel Model) : IRequest<StoreLandingPage>;

/// <summary>فرمان تغییر وضعیت انتشار.</summary>
/// <param name="PageId">شناسه صفحه.</param>
/// <param name="Status">Published یا Draft.</param>
public sealed record SetStoreLandingPageStatusCommand(Guid PageId, string? Status) : IRequest<StoreLandingPage>;

/// <summary>فرمان تنظیم خانه.</summary>
/// <param name="PageId">صفحه هدف یا null برای پاک‌کردن.</param>
public sealed record SetStoreHomePageCommand(Guid? PageId) : IRequest<StoreHomeWriteResult>;

/// <summary>فرمان افزودن بخش.</summary>
/// <param name="PageId">شناسه صفحه.</param>
/// <param name="Model">مدل بخش.</param>
public sealed record AddStoreLandingPageSectionCommand(Guid PageId, StoreLandingPageSectionWriteModel Model) : IRequest<StoreLandingPageSection>;

/// <summary>فرمان جایگزینی ترکیب بخش‌ها.</summary>
/// <param name="PageId">شناسه صفحه.</param>
/// <param name="Sections">لیست بخش‌ها.</param>
public sealed record ReplaceStoreLandingPageCompositionCommand(Guid PageId, IReadOnlyList<StoreLandingPageSectionWriteModel> Sections) : IRequest<IReadOnlyList<StoreLandingPageSection>>;

/// <summary>فرمان به‌روزرسانی بخش.</summary>
/// <param name="PageId">شناسه صفحه.</param>
/// <param name="SectionId">شناسه بخش.</param>
/// <param name="Model">مدل بخش.</param>
public sealed record UpdateStoreLandingPageSectionCommand(Guid PageId, Guid SectionId, StoreLandingPageSectionWriteModel Model) : IRequest<StoreLandingPageSection>;

/// <summary>فرمان فعال/غیرفعال بخش.</summary>
/// <param name="PageId">شناسه صفحه.</param>
/// <param name="SectionId">شناسه بخش.</param>
/// <param name="Enabled">فعال؟</param>
public sealed record SetStoreLandingPageSectionEnabledCommand(Guid PageId, Guid SectionId, bool Enabled) : IRequest<StoreLandingPageSection>;

/// <summary>فرمان ترتیب بخش‌ها.</summary>
/// <param name="PageId">شناسه صفحه.</param>
/// <param name="SectionIds">ترتیب شناسه‌ها.</param>
public sealed record ReorderStoreLandingPageSectionsCommand(Guid PageId, IReadOnlyList<Guid> SectionIds) : IRequest<IReadOnlyList<StoreLandingPageSection>>;

/// <summary>فرمان حذف صفحه.</summary>
/// <param name="PageId">شناسه صفحه.</param>
public sealed record DeleteStoreLandingPageCommand(Guid PageId) : IRequest<StoreLandingPage>;

/// <summary>فرمان حذف بخش.</summary>
/// <param name="PageId">شناسه صفحه.</param>
/// <param name="SectionId">شناسه بخش.</param>
public sealed record DeleteStoreLandingPageSectionCommand(Guid PageId, Guid SectionId) : IRequest<Unit>;

/// <summary>اعتبارسنج ایجاد صفحه.</summary>
public sealed class CreateStoreLandingPageCommandValidator : AbstractValidator<CreateStoreLandingPageCommand>
{
    /// <summary>قواعد حداقلی عنوان/اسلاگ/لوکیل.</summary>
    public CreateStoreLandingPageCommandValidator()
    {
        RuleFor(x => x.Model.Title).NotEmpty().WithErrorCode("landing.title.required");
        RuleFor(x => x.Model.Slug).NotEmpty().WithErrorCode("landing.slug.required");
        RuleFor(x => x.Model.Locale).NotEmpty().WithErrorCode("landing.locale.required");
    }
}
