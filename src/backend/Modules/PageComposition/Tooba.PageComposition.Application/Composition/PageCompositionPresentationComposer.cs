using Tooba.BuildingBlocks;
using Tooba.PageComposition.Contracts.Errors;
using Tooba.PageComposition.Application.Models;
using Tooba.PageComposition.Application.Ports;
using Tooba.PageComposition.Domain.Constants;

namespace Tooba.PageComposition.Application.Composition;

/// <summary>ترکیب use-case برای مسیرهای عمومی و مدیریتی Page Composition.</summary>
public sealed class PageCompositionPresentationComposer
{
    private readonly IPageCompositionDirectory _pageComposition;

    /// <summary>دایرکتوری Page Composition را تزریق می‌کند.</summary>
    public PageCompositionPresentationComposer(IPageCompositionDirectory pageComposition) =>
        _pageComposition = pageComposition;

    /// <summary>کاتالوگ section types.</summary>
    public Task<SectionCatalogSnapshot> GetCatalogAsync(CancellationToken cancellationToken) =>
        Guard(() => _pageComposition.GetCatalogAsync(cancellationToken));

    /// <summary>ترکیب عمومی خانه.</summary>
    public Task<HomeCompositionSnapshot> GetHomeCompositionAsync(
        Guid tenantId,
        string? locale,
        CancellationToken cancellationToken) =>
        Guard(() => _pageComposition.GetHomeCompositionAsync(tenantId, locale, cancellationToken));

    /// <summary>نمای admin خانه.</summary>
    public Task<AdminHomeCompositionSnapshot> AdminGetHomeAsync(
        Guid tenantId,
        string? locale,
        CancellationToken cancellationToken) =>
        Guard(() => _pageComposition.AdminGetHomeAsync(tenantId, locale, cancellationToken));

    /// <summary>مرتب‌سازی sectionها.</summary>
    public Task<AdminHomeCompositionSnapshot> AdminReorderHomeAsync(
        Guid tenantId,
        string? locale,
        IReadOnlyList<Guid> sectionIdsInOrder,
        CancellationToken cancellationToken) =>
        Guard(() => _pageComposition.AdminReorderHomeAsync(tenantId, locale, sectionIdsInOrder, cancellationToken));

    /// <summary>به‌روزرسانی section.</summary>
    public Task<AdminHomeCompositionSnapshot> AdminUpdateSectionAsync(
        Guid tenantId,
        string? locale,
        Guid sectionId,
        UpdateHomeSectionCommand command,
        CancellationToken cancellationToken) =>
        Guard(() => _pageComposition.AdminUpdateSectionAsync(tenantId, locale, sectionId, command, cancellationToken));

    /// <summary>افزودن section.</summary>
    public Task<AdminHomeCompositionSnapshot> AdminAddSectionAsync(
        Guid tenantId,
        string? locale,
        AddHomeSectionCommand command,
        CancellationToken cancellationToken) =>
        Guard(() => _pageComposition.AdminAddSectionAsync(tenantId, locale, command, cancellationToken));

    /// <summary>حذف section.</summary>
    public Task<AdminHomeCompositionSnapshot> AdminRemoveSectionAsync(
        Guid tenantId,
        string? locale,
        Guid sectionId,
        CancellationToken cancellationToken) =>
        Guard(() => _pageComposition.AdminRemoveSectionAsync(tenantId, locale, sectionId, cancellationToken));

    /// <summary>بازگردانی پیش‌فرض.</summary>
    public Task<AdminHomeCompositionSnapshot> AdminRestoreDefaultHomeAsync(
        Guid tenantId,
        string? locale,
        CancellationToken cancellationToken) =>
        Guard(() => _pageComposition.AdminRestoreDefaultHomeAsync(tenantId, locale, cancellationToken));

    /// <summary>Tenant جاری را به Guid پایدار نگاشت می‌کند.</summary>
    public static Guid RequireTenantId(ICurrentTenant tenant)
    {
        try
        {
            return PageCompositionTenantIds.FromTenantKey(
                tenant.Current?.TenantId.Value
                ?? throw new InvalidOperationException("Tenant resolve نشده است."));
        }
        catch (InvalidOperationException)
        {
            throw new SemanticException(new SemanticError(PageCompositionErrorCodes.TenantMissing));
        }
    }

    private static async Task<T> Guard<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (SemanticException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            throw PageCompositionFailureMapper.ToSemantic(ex);
        }
    }
}
