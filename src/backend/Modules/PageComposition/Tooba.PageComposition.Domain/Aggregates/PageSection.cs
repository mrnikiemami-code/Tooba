using Tooba.BuildingBlocks;
using Tooba.PageComposition.Domain.Catalog;

namespace Tooba.PageComposition.Domain.Aggregates;

/// <summary>یک section در ترکیب صفحه.</summary>
public sealed class PageSection
{
    /// <summary>حداکثر طول SectionType.</summary>
    public const int SectionTypeMaxLength = 64;
    /// <summary>حداکثر طول Variant.</summary>
    public const int VariantMaxLength = 64;
    /// <summary>حداکثر طول ConfigurationJson.</summary>
    public const int ConfigurationJsonMaxLength = 4000;

    private PageSection() { }

    /// <summary>شناسهٔ پایدار section.</summary>
    public Guid PageSectionId { get; init; }
    /// <summary>شناسهٔ PageDefinition والد.</summary>
    public Guid PageDefinitionId { get; init; }
    /// <summary>نوع section از کاتالوگ ثابت.</summary>
    public string SectionType { get; private set; } = string.Empty;
    /// <summary>ترتیب نمایش.</summary>
    public int DisplayOrder { get; private set; }
    /// <summary>آیا section قابل نمایش است.</summary>
    public bool IsVisible { get; private set; }
    /// <summary>variant تأییدشده.</summary>
    public string Variant { get; private set; } = SectionCatalog.DefaultVariant;
    /// <summary>پیکربندی JSON امن.</summary>
    public string ConfigurationJson { get; private set; } = "{}";

    /// <summary>section جدید می‌سازد.</summary>
    internal static PageSection Create(
        Guid pageDefinitionId,
        string sectionType,
        string variant,
        int displayOrder,
        string configurationJson,
        DateTimeOffset now)
    {
        SectionCatalog.EnsureKnownSectionType(sectionType);
        SectionCatalog.EnsureAllowedVariant(sectionType, variant);
        var normalizedConfig = SectionCatalog.ValidateAndNormalizeConfiguration(sectionType, configurationJson);
        return new PageSection
        {
            PageSectionId = UuidV7.New(),
            PageDefinitionId = pageDefinitionId,
            SectionType = sectionType,
            DisplayOrder = displayOrder,
            IsVisible = true,
            Variant = variant,
            ConfigurationJson = normalizedConfig,
        };
    }

    /// <summary>ترتیب نمایش را تنظیم می‌کند.</summary>
    internal void SetDisplayOrder(int displayOrder, DateTimeOffset now) => DisplayOrder = displayOrder;

    /// <summary>visibility را تغییر می‌دهد.</summary>
    internal void SetVisibility(bool isVisible, DateTimeOffset now) => IsVisible = isVisible;

    /// <summary>پیکربندی را به‌روزرسانی می‌کند.</summary>
    internal void UpdateConfiguration(string configurationJson, DateTimeOffset now)
    {
        ConfigurationJson = SectionCatalog.ValidateAndNormalizeConfiguration(SectionType, configurationJson);
    }

    /// <summary>variant را به‌روزرسانی می‌کند.</summary>
    internal void UpdateVariant(string variant, DateTimeOffset now)
    {
        SectionCatalog.EnsureAllowedVariant(SectionType, variant);
        Variant = variant;
    }
}
