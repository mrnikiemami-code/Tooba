namespace Tooba.PageComposition.Application.Models;

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
