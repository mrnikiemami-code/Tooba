using Tooba.PageComposition.Application.Models;

namespace Tooba.PageComposition.Application.Ports;

/// <summary>قابلیت خواندن و مدیریت Page Composition.</summary>
public interface IPageCompositionDirectory
{
    /// <summary>کاتالوگ section types و schema config را برمی‌گرداند.</summary>
    Task<SectionCatalogSnapshot> GetCatalogAsync(CancellationToken cancellationToken);

    /// <summary>sectionهای visible خانه را به ترتیب برمی‌گرداند.</summary>
    Task<HomeCompositionSnapshot> GetHomeCompositionAsync(Guid tenantId, string? locale, CancellationToken cancellationToken);

    /// <summary>نمای admin خانه شامل sectionهای پنهان.</summary>
    Task<AdminHomeCompositionSnapshot> AdminGetHomeAsync(Guid tenantId, string? locale, CancellationToken cancellationToken);

    /// <summary>sectionهای خانه را مرتب می‌کند.</summary>
    Task<AdminHomeCompositionSnapshot> AdminReorderHomeAsync(
        Guid tenantId,
        string? locale,
        IReadOnlyList<Guid> sectionIdsInOrder,
        CancellationToken cancellationToken);

    /// <summary>section را به‌روزرسانی می‌کند.</summary>
    Task<AdminHomeCompositionSnapshot> AdminUpdateSectionAsync(
        Guid tenantId,
        string? locale,
        Guid sectionId,
        UpdateHomeSectionCommand command,
        CancellationToken cancellationToken);

    /// <summary>section تأییدشده اضافه می‌کند.</summary>
    Task<AdminHomeCompositionSnapshot> AdminAddSectionAsync(
        Guid tenantId,
        string? locale,
        AddHomeSectionCommand command,
        CancellationToken cancellationToken);

    /// <summary>section را حذف می‌کند.</summary>
    Task<AdminHomeCompositionSnapshot> AdminRemoveSectionAsync(
        Guid tenantId,
        string? locale,
        Guid sectionId,
        CancellationToken cancellationToken);

    /// <summary>ترکیب پیش‌فرض خانه را بازمی‌گرداند.</summary>
    Task<AdminHomeCompositionSnapshot> AdminRestoreDefaultHomeAsync(
        Guid tenantId,
        string? locale,
        CancellationToken cancellationToken);
}
