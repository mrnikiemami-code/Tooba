using Tooba.BuildingBlocks;
using Tooba.PageComposition.Contracts.Errors;
using Tooba.PageComposition.Domain.Catalog;
using Tooba.PageComposition.Domain.Constants;

namespace Tooba.PageComposition.Domain.Aggregates;

/// <summary>تعریف صفحهٔ قابل ترکیب برای یک Tenant/locale.</summary>
public sealed class PageDefinition
{
    /// <summary>حداکثر طول PageKey.</summary>
    public const int PageKeyMaxLength = 64;
    /// <summary>حداکثر طول locale.</summary>
    public const int LocaleMaxLength = 16;

    private readonly List<PageSection> _sections = [];

    private PageDefinition() { }

    /// <summary>شناسهٔ پایدار تعریف صفحه.</summary>
    public Guid PageDefinitionId { get; init; }
    /// <summary>کلید صفحه مثل home.</summary>
    public string PageKey { get; private set; } = string.Empty;
    /// <summary>Tenant مالک.</summary>
    public Guid TenantId { get; init; }
    /// <summary>locale اختیاری؛ null یعنی همه localeها.</summary>
    public string? Locale { get; private set; }
    /// <summary>توکن همزمانی.</summary>
    public int VersionToken { get; private set; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>زمان آخرین به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }
    /// <summary>sectionهای صفحه.</summary>
    public IReadOnlyCollection<PageSection> Sections => _sections;

    /// <summary>تعریف صفحهٔ home با sectionهای پیش‌فرض می‌سازد.</summary>
    public static PageDefinition CreateDefaultHome(Guid tenantId, string? locale, DateTimeOffset now)
    {
        ValidatePageKey(PageKeys.Home);
        ValidateLocale(locale);
        var definition = new PageDefinition
        {
            PageDefinitionId = UuidV7.New(),
            PageKey = PageKeys.Home,
            TenantId = tenantId,
            Locale = NormalizeLocale(locale),
            VersionToken = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        definition.RestoreDefaultSections(now);
        return definition;
    }

    /// <summary>sectionها را با شناسه‌های داده‌شده مرتب می‌کند.</summary>
    public void ReorderSections(IReadOnlyList<Guid> sectionIdsInOrder, DateTimeOffset now)
    {
        if (sectionIdsInOrder.Count == 0)
            throw new SemanticException(new SemanticError(PageCompositionErrorCodes.MutationRejected));
        if (sectionIdsInOrder.Count != _sections.Count)
            throw new SemanticException(new SemanticError(PageCompositionErrorCodes.MutationRejected));
        if (sectionIdsInOrder.Distinct().Count() != sectionIdsInOrder.Count)
            throw new SemanticException(new SemanticError(PageCompositionErrorCodes.MutationRejected));

        var lookup = _sections.ToDictionary(section => section.PageSectionId);
        for (var index = 0; index < sectionIdsInOrder.Count; index++)
        {
            if (!lookup.TryGetValue(sectionIdsInOrder[index], out var section))
                throw new SemanticException(new SemanticError(PageCompositionErrorCodes.SectionMissing));
            section.SetDisplayOrder(index, now);
        }

        _sections.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
        Touch(now);
    }

    /// <summary>نمایش section را تغییر می‌دهد.</summary>
    public void SetSectionVisibility(Guid sectionId, bool isVisible, DateTimeOffset now)
    {
        var section = RequireSection(sectionId);
        section.SetVisibility(isVisible, now);
        Touch(now);
    }

    /// <summary>پیکربندی section را به‌روزرسانی می‌کند.</summary>
    public void UpdateSectionConfiguration(Guid sectionId, string configurationJson, DateTimeOffset now)
    {
        var section = RequireSection(sectionId);
        section.UpdateConfiguration(configurationJson, now);
        Touch(now);
    }

    /// <summary>variant section را به‌روزرسانی می‌کند.</summary>
    public void UpdateSectionVariant(Guid sectionId, string variant, DateTimeOffset now)
    {
        var section = RequireSection(sectionId);
        section.UpdateVariant(variant, now);
        Touch(now);
    }

    /// <summary>section تأییدشدهٔ جدید اضافه می‌کند.</summary>
    public PageSection AddApprovedSection(string sectionType, string variant, string? configurationJson, DateTimeOffset now)
    {
        SectionCatalog.EnsureKnownSectionType(sectionType);
        SectionCatalog.EnsureAllowedVariant(sectionType, variant);
        var normalizedConfig = SectionCatalog.ValidateAndNormalizeConfiguration(sectionType, configurationJson);
        var order = _sections.Count == 0 ? 0 : _sections.Max(section => section.DisplayOrder) + 1;
        var section = PageSection.Create(
            PageDefinitionId,
            sectionType,
            variant,
            order,
            normalizedConfig,
            now);
        _sections.Add(section);
        Touch(now);
        return section;
    }

    /// <summary>section را حذف می‌کند.</summary>
    public void RemoveSection(Guid sectionId, DateTimeOffset now)
    {
        var index = _sections.FindIndex(section => section.PageSectionId == sectionId);
        if (index < 0)
            throw new SemanticException(new SemanticError(PageCompositionErrorCodes.SectionMissing));
        _sections.RemoveAt(index);
        ReindexSections(now);
        Touch(now);
    }

    /// <summary>sectionهای پیش‌فرض خانه را بازمی‌گرداند.</summary>
    public void RestoreDefaultSections(DateTimeOffset now)
    {
        _sections.Clear();
        var order = 0;
        foreach (var sectionType in SectionCatalog.DefaultHomeSectionTypes)
        {
            _sections.Add(PageSection.Create(
                PageDefinitionId,
                sectionType,
                SectionCatalog.DefaultVariant,
                order++,
                "{}",
                now));
        }
        Touch(now);
    }

    /// <summary>sectionهای بارگذاری‌شده را به aggregate متصل می‌کند.</summary>
    public void AttachSections(IEnumerable<PageSection> sections)
    {
        _sections.Clear();
        _sections.AddRange(sections.OrderBy(section => section.DisplayOrder));
    }

    private PageSection RequireSection(Guid sectionId) =>
        _sections.FirstOrDefault(section => section.PageSectionId == sectionId)
        ?? throw new SemanticException(new SemanticError(PageCompositionErrorCodes.SectionMissing));

    private void ReindexSections(DateTimeOffset now)
    {
        var ordered = _sections.OrderBy(section => section.DisplayOrder).ToList();
        for (var index = 0; index < ordered.Count; index++)
            ordered[index].SetDisplayOrder(index, now);
    }

    private void Touch(DateTimeOffset now)
    {
        VersionToken++;
        UpdatedAt = now;
    }

    private static void ValidatePageKey(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey) || pageKey.Trim().Length > PageKeyMaxLength)
            throw new SemanticException(new SemanticError(PageCompositionErrorCodes.MutationRejected));
    }

    private static void ValidateLocale(string? locale)
    {
        if (locale is not null && (locale.Trim().Length == 0 || locale.Trim().Length > LocaleMaxLength))
            throw new SemanticException(new SemanticError(PageCompositionErrorCodes.MutationRejected));
    }

    private static string? NormalizeLocale(string? locale) =>
        string.IsNullOrWhiteSpace(locale) ? null : locale.Trim();
}
