#pragma warning disable CS1591
namespace Tooba.Catalog.Domain;

/// <summary>آینه‌های باقی‌ماندهٔ Product/Category/Brand برای parity ساختاری کامل (TB-P10-T022-R6).</summary>
public sealed class TemplateTag
{
    public Guid TagId { get; init; }
    public Guid TemplateId { get; init; }
    public string Code { get; init; } = "";
    public string? SlugSeam { get; set; }
    public CatalogPublicationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class TemplateProductTagAssignment
{
    public Guid AssignmentId { get; init; }
    public Guid ProductId { get; init; }
    public Guid TagId { get; init; }
}

public sealed class TemplateCategoryTagAssignment
{
    public Guid AssignmentId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid TagId { get; init; }
}

public sealed class TemplateAttributeDefinition
{
    public Guid DefinitionId { get; init; }
    public Guid TemplateId { get; init; }
    public string Code { get; init; } = "";
    public CatalogAttributeValueKind ValueKind { get; init; }
    public bool IsVariantAxis { get; set; }
    public string? Unit { get; set; }
    public bool IsRequired { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsComparable { get; set; }
    public bool IsMultivalue { get; set; }
    public int DisplayOrder { get; set; }
    public decimal? ValidationMin { get; set; }
    public decimal? ValidationMax { get; set; }
    public int? ValidationMaxLength { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class TemplateAttributeOption
{
    public Guid OptionId { get; init; }
    public Guid DefinitionId { get; init; }
    public string Code { get; init; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TemplateCategoryAttributeBinding
{
    public Guid BindingId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid DefinitionId { get; init; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsVariantAxis { get; set; }
    public bool IsComparable { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class TemplateCategoryFacetConfiguration
{
    public Guid FacetConfigurationId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid DefinitionId { get; init; }
    public CatalogFacetDisplayType DisplayType { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsCollapsedByDefault { get; set; }
    public bool ShowCounts { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class TemplateMegaMenuItem
{
    public Guid MegaMenuItemId { get; init; }
    public CatalogMegaMenuItemType ItemType { get; init; }
    public Guid CategoryId { get; init; }
    public Guid? ParentMegaMenuItemId { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsFeatured { get; set; }
    public Guid? ImageMediaAssetId { get; set; }
    public Guid? IconMediaAssetId { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class TemplateMegaMenuItemTranslation
{
    public Guid MegaMenuItemTranslationId { get; init; }
    public Guid MegaMenuItemId { get; init; }
    public string Locale { get; init; } = "";
    public string? TitleOverride { get; set; }
    public string? BadgeText { get; set; }
    public string? ShortLabel { get; set; }
}

public sealed class TemplateProductVariantAxis
{
    public Guid AxisId { get; init; }
    public Guid ProductId { get; init; }
    public Guid DefinitionId { get; init; }
    public int DisplayOrder { get; set; }
}

public sealed class TemplateProductAttributeValue
{
    public Guid ValueId { get; init; }
    public Guid ProductId { get; init; }
    public Guid DefinitionId { get; init; }
    public string CanonicalValue { get; init; } = "";
}

public sealed class TemplateVariantAttributeValue
{
    public Guid ValueId { get; init; }
    public Guid VariantId { get; init; }
    public Guid DefinitionId { get; init; }
    public string CanonicalValue { get; init; } = "";
}

public sealed class TemplateProductHistoryEntry
{
    public Guid HistoryId { get; init; }
    public Guid ProductId { get; init; }
    public string EventType { get; init; } = "";
    public string Section { get; init; } = "";
    public string SummaryFa { get; init; } = "";
    public string? BeforeSummary { get; set; }
    public string? AfterSummary { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public DateTimeOffset OccurredAt { get; init; }
}

public sealed class TemplateCategorySlugHistory
{
    public Guid HistoryId { get; init; }
    public Guid CategoryId { get; init; }
    public string Locale { get; init; } = "";
    public string OldSlug { get; init; } = "";
    public DateTimeOffset ChangedAt { get; init; }
}

/// <summary>شناسه‌های پایدار seed ساختاری Fashion برای R6.</summary>
public static class FashionTemplateParityIds
{
    // last group always 12 hex chars
    public static Guid Tag(int index) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000000a{index:000}");

    public static Guid AttributeDefinition(int index) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000000b{index:000}");

    public static Guid AttributeOption(int definition, int option) =>
        Guid.Parse($"019022a6-0000-7000-8000-0000000c{definition:00}{option:00}");

    public static Guid LocalizedAttrName(int index) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000000d{index:000}");

    public static Guid LocalizedTagName(int index) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000000e{index:000}");

    public static Guid LocalizedOptionName(int definition, int option) =>
        Guid.Parse($"019022a6-0000-7000-8000-0000000f{definition:00}{option:00}");

    public static Guid ProductTagAssignment(int product, int tag) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000a{product:000}{tag:000}");

    public static Guid CategoryTagAssignment(int root, int tag) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000b{root:000}{tag:000}");

    public static Guid CategoryBinding(int root, int def) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000c{root:000}{def:000}");

    public static Guid CategoryFacet(int root, int def) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000d{root:000}{def:000}");

    public static Guid MegaMenuItem(int root) =>
        Guid.Parse($"019022a6-0000-7000-8000-000000002{root:000}");

    public static Guid MegaMenuTranslation(int root) =>
        Guid.Parse($"019022a6-0000-7000-8000-000000003{root:000}");

    public static Guid ProductAttrValue(int product, int def) =>
        Guid.Parse($"019022a6-0000-7000-8000-00000e{product:000}{def:000}");

    public static Guid VariantAxis(int product) =>
        Guid.Parse($"019022a6-0000-7000-8000-000000004{product:000}");

    public static Guid Variant(int product) =>
        Guid.Parse($"019022a6-0000-7000-8000-000000005{product:000}");

    public static Guid VariantAttrValue(int product) =>
        Guid.Parse($"019022a6-0000-7000-8000-000000006{product:000}");

    public static Guid ProductHistory(int product) =>
        Guid.Parse($"019022a6-0000-7000-8000-000000007{product:000}");

    public static Guid CategorySlugHistory(int root) =>
        Guid.Parse($"019022a6-0000-7000-8000-000000008{root:000}");
}
