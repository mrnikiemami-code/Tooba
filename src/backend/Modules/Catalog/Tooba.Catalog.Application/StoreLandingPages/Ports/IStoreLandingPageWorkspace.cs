using Tooba.Catalog.Application.StoreLandingPages.Models;

namespace Tooba.Catalog.Application.StoreLandingPages.Ports;

/// <summary>Orchestration خواندن/نوشتن Landing + cache (مالک Catalog).</summary>
public interface IStoreLandingPageWorkspace
{
    /// <summary>فهرست صفحات Admin.</summary>
    Task<IReadOnlyList<StoreLandingPageAdminView>> ListAsync(CancellationToken cancellationToken);

    /// <summary>یک صفحه Admin.</summary>
    Task<StoreLandingPageAdminView> GetAsync(Guid pageId, CancellationToken cancellationToken);

    /// <summary>پیش‌نمایش Admin.</summary>
    Task<StoreLandingPagePublicView> ResolvePreviewAsync(Guid pageId, CancellationToken cancellationToken);

    /// <summary>ایجاد صفحه.</summary>
    Task<StoreLandingPageAdminView> CreateAsync(StoreLandingPageWriteRequest body, CancellationToken cancellationToken);

    /// <summary>به‌روزرسانی صفحه.</summary>
    Task<StoreLandingPageAdminView> UpdateAsync(
        Guid pageId,
        StoreLandingPageWriteRequest body,
        CancellationToken cancellationToken);

    /// <summary>تغییر وضعیت.</summary>
    Task<StoreLandingPageAdminView> SetStatusAsync(Guid pageId, string? status, CancellationToken cancellationToken);

    /// <summary>حذف صفحه.</summary>
    Task DeletePageAsync(Guid pageId, CancellationToken cancellationToken);

    /// <summary>تنظیم خانه.</summary>
    Task<StoreHomeSelectionView> SetHomeAsync(Guid? pageId, CancellationToken cancellationToken);

    /// <summary>خواندن انتخاب خانه.</summary>
    Task<StoreHomeSelectionView> GetHomeSelectionAsync(CancellationToken cancellationToken);

    /// <summary>فهرست بخش‌ها.</summary>
    Task<IReadOnlyList<StoreLandingPageSectionAdminView>> ListSectionsAsync(
        Guid pageId,
        CancellationToken cancellationToken);

    /// <summary>افزودن بخش.</summary>
    Task<StoreLandingPageSectionAdminView> AddSectionAsync(
        Guid pageId,
        StoreLandingPageSectionWriteRequest body,
        CancellationToken cancellationToken);

    /// <summary>جایگزینی ترکیب.</summary>
    Task<IReadOnlyList<StoreLandingPageSectionAdminView>> ReplaceCompositionAsync(
        Guid pageId,
        IReadOnlyList<StoreLandingPageSectionWriteRequest>? sections,
        CancellationToken cancellationToken);

    /// <summary>به‌روزرسانی بخش.</summary>
    Task<StoreLandingPageSectionAdminView> UpdateSectionAsync(
        Guid pageId,
        Guid sectionId,
        StoreLandingPageSectionWriteRequest body,
        CancellationToken cancellationToken);

    /// <summary>فعال/غیرفعال بخش.</summary>
    Task<StoreLandingPageSectionAdminView> SetSectionEnabledAsync(
        Guid pageId,
        Guid sectionId,
        bool enabled,
        CancellationToken cancellationToken);

    /// <summary>ترتیب بخش‌ها.</summary>
    Task<IReadOnlyList<StoreLandingPageSectionAdminView>> ReorderSectionsAsync(
        Guid pageId,
        IReadOnlyList<Guid>? sectionIds,
        CancellationToken cancellationToken);

    /// <summary>حذف بخش.</summary>
    Task DeleteSectionAsync(Guid pageId, Guid sectionId, CancellationToken cancellationToken);

    /// <summary>صفحهٔ عمومی؛ null وقتی نیست.</summary>
    Task<StoreLandingPagePublicView?> ResolvePublicAsync(
        string? locale,
        string? slug,
        CancellationToken cancellationToken);

    /// <summary>sitemap ایندکس‌پذیر.</summary>
    Task<IReadOnlyList<StoreLandingSitemapEntry>> ListIndexableLandingsAsync(CancellationToken cancellationToken);
}
