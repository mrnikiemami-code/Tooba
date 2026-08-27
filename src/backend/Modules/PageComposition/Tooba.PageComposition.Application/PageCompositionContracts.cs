namespace Tooba.PageComposition.Application;

/// <summary>متادیتای یک نوع section در catalog.</summary>
public sealed record SectionCatalogEntry(
    string SectionType,
    IReadOnlyList<string> AllowedVariants,
    IReadOnlyList<string> SupportedConfigKeys);

/// <summary>نمای catalog section types.</summary>
public sealed record SectionCatalogSnapshot(
    IReadOnlyList<SectionCatalogEntry> SectionTypes,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ConfigSchemaMetadata);

/// <summary>section قابل نمایش در storefront.</summary>
public sealed record HomeCompositionSectionItem(
    Guid PageSectionId,
    string SectionType,
    int DisplayOrder,
    string Variant,
    string ConfigurationJson);

/// <summary>ترکیب عمومی خانه.</summary>
public sealed record HomeCompositionSnapshot(
    string PageKey,
    Guid TenantId,
    string? Locale,
    int VersionToken,
    IReadOnlyList<HomeCompositionSectionItem> Sections);

/// <summary>section مدیریتی شامل visibility.</summary>
public sealed record AdminHomeCompositionSectionItem(
    Guid PageSectionId,
    string SectionType,
    int DisplayOrder,
    bool IsVisible,
    string Variant,
    string ConfigurationJson);

/// <summary>ترکیب مدیریتی خانه.</summary>
public sealed record AdminHomeCompositionSnapshot(
    Guid PageDefinitionId,
    string PageKey,
    Guid TenantId,
    string? Locale,
    int VersionToken,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminHomeCompositionSectionItem> Sections);

/// <summary>فرمان افزودن section.</summary>
public sealed record AddHomeSectionCommand(
    string SectionType,
    string Variant,
    string? ConfigurationJson,
    bool IsVisible = true);

/// <summary>فرمان به‌روزرسانی section.</summary>
public sealed record UpdateHomeSectionCommand(
    bool? IsVisible,
    string? ConfigurationJson,
    string? Variant);

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
