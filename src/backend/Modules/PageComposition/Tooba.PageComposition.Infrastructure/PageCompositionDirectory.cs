using Microsoft.EntityFrameworkCore;
using Tooba.PageComposition.Application;
using Tooba.PageComposition.Domain;
using Tooba.PageComposition.Infrastructure.Persistence;

namespace Tooba.PageComposition.Infrastructure;

/// <summary>دایرکتوری Page Composition با schema مستقل.</summary>
public sealed class PageCompositionDirectory : IPageCompositionDirectory
{
    private readonly PageCompositionDbContext _db;

    /// <summary>DbContext مالک را تزریق می‌کند.</summary>
    public PageCompositionDirectory(PageCompositionDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<SectionCatalogSnapshot> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var entries = SectionCatalog.AllSectionTypes
            .Select(sectionType => new SectionCatalogEntry(
                sectionType,
                SectionCatalog.GetAllowedVariants(sectionType),
                GetSupportedConfigKeys(sectionType)))
            .ToList();
        return Task.FromResult(new SectionCatalogSnapshot(entries, SectionCatalog.ConfigSchemaMetadata));
    }

    /// <inheritdoc />
    public async Task<HomeCompositionSnapshot> GetHomeCompositionAsync(
        Guid tenantId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var definition = await LoadHomeDefinitionAsync(tenantId, locale, track: false, cancellationToken);
        var sections = definition.Sections
            .Where(section => section.IsVisible)
            .OrderBy(section => section.DisplayOrder)
            .Select(MapPublicSection)
            .ToList();
        return new HomeCompositionSnapshot(
            definition.PageKey,
            definition.TenantId,
            definition.Locale,
            definition.VersionToken,
            sections);
    }

    /// <inheritdoc />
    public async Task<AdminHomeCompositionSnapshot> AdminGetHomeAsync(
        Guid tenantId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var definition = await LoadHomeDefinitionAsync(tenantId, locale, track: false, cancellationToken);
        return MapAdmin(definition);
    }

    /// <inheritdoc />
    public async Task<AdminHomeCompositionSnapshot> AdminReorderHomeAsync(
        Guid tenantId,
        string? locale,
        IReadOnlyList<Guid> sectionIdsInOrder,
        CancellationToken cancellationToken)
    {
        var definition = await LoadHomeDefinitionAsync(tenantId, locale, track: true, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        definition.ReorderSections(sectionIdsInOrder, now);
        await SaveDefinitionAsync(definition, cancellationToken);
        return MapAdmin(definition);
    }

    /// <inheritdoc />
    public async Task<AdminHomeCompositionSnapshot> AdminUpdateSectionAsync(
        Guid tenantId,
        string? locale,
        Guid sectionId,
        UpdateHomeSectionCommand command,
        CancellationToken cancellationToken)
    {
        var definition = await LoadHomeDefinitionAsync(tenantId, locale, track: true, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (command.IsVisible.HasValue)
            definition.SetSectionVisibility(sectionId, command.IsVisible.Value, now);
        if (command.ConfigurationJson is not null)
            definition.UpdateSectionConfiguration(sectionId, command.ConfigurationJson, now);
        if (command.Variant is not null)
            definition.UpdateSectionVariant(sectionId, command.Variant, now);
        await SaveDefinitionAsync(definition, cancellationToken);
        return MapAdmin(definition);
    }

    /// <inheritdoc />
    public async Task<AdminHomeCompositionSnapshot> AdminAddSectionAsync(
        Guid tenantId,
        string? locale,
        AddHomeSectionCommand command,
        CancellationToken cancellationToken)
    {
        var definition = await LoadHomeDefinitionAsync(tenantId, locale, track: true, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var section = definition.AddApprovedSection(
            command.SectionType,
            command.Variant,
            command.ConfigurationJson,
            now);
        if (!command.IsVisible)
            definition.SetSectionVisibility(section.PageSectionId, false, now);
        await SaveDefinitionAsync(definition, cancellationToken);
        return MapAdmin(definition);
    }

    /// <inheritdoc />
    public async Task<AdminHomeCompositionSnapshot> AdminRemoveSectionAsync(
        Guid tenantId,
        string? locale,
        Guid sectionId,
        CancellationToken cancellationToken)
    {
        var definition = await LoadHomeDefinitionAsync(tenantId, locale, track: true, cancellationToken);
        definition.RemoveSection(sectionId, DateTimeOffset.UtcNow);
        await SaveDefinitionAsync(definition, cancellationToken);
        return MapAdmin(definition);
    }

    /// <inheritdoc />
    public async Task<AdminHomeCompositionSnapshot> AdminRestoreDefaultHomeAsync(
        Guid tenantId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var definition = await LoadHomeDefinitionAsync(tenantId, locale, track: true, cancellationToken);
        definition.RestoreDefaultSections(DateTimeOffset.UtcNow);
        await SaveDefinitionAsync(definition, cancellationToken);
        return MapAdmin(definition);
    }

    internal static AdminHomeCompositionSnapshot MapAdmin(PageDefinition definition) => new(
        definition.PageDefinitionId,
        definition.PageKey,
        definition.TenantId,
        definition.Locale,
        definition.VersionToken,
        definition.UpdatedAt,
        definition.Sections
            .OrderBy(section => section.DisplayOrder)
            .Select(MapAdminSection)
            .ToList());

    private async Task<PageDefinition> LoadHomeDefinitionAsync(
        Guid tenantId,
        string? locale,
        bool track,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var query = track ? _db.PageDefinitions : _db.PageDefinitions.AsNoTracking();
        var definition = await query.FirstOrDefaultAsync(
            row => row.TenantId == tenantId
                && row.PageKey == PageKeys.Home
                && row.Locale == normalizedLocale,
            cancellationToken);

        if (definition is null)
        {
            var now = DateTimeOffset.UtcNow;
            var created = PageDefinition.CreateDefaultHome(tenantId, normalizedLocale, now);
            if (track)
            {
                _db.PageDefinitions.Add(created);
                foreach (var section in created.Sections)
                    _db.PageSections.Add(section);
                await _db.SaveChangesAsync(cancellationToken);
            }
            return created;
        }

        var sectionQuery = track ? _db.PageSections : _db.PageSections.AsNoTracking();
        var sections = await sectionQuery
            .Where(section => section.PageDefinitionId == definition.PageDefinitionId)
            .OrderBy(section => section.DisplayOrder)
            .ToListAsync(cancellationToken);
        definition.AttachSections(sections);
        return definition;
    }

    private static string? NormalizeLocale(string? locale) =>
        string.IsNullOrWhiteSpace(locale) ? null : locale.Trim();

    private static HomeCompositionSectionItem MapPublicSection(PageSection section) => new(
        section.PageSectionId,
        section.SectionType,
        section.DisplayOrder,
        section.Variant,
        section.ConfigurationJson);

    private static AdminHomeCompositionSectionItem MapAdminSection(PageSection section) => new(
        section.PageSectionId,
        section.SectionType,
        section.DisplayOrder,
        section.IsVisible,
        section.Variant,
        section.ConfigurationJson);

    private static IReadOnlyList<string> GetSupportedConfigKeys(string sectionType)
    {
        var keys = new List<string> { "title" };
        if (sectionType is SectionCatalog.ProductRailFlash
            or SectionCatalog.ProductRailMostViewed
            or SectionCatalog.BestSellers
            or SectionCatalog.NewestProducts
            or SectionCatalog.LatestArticles)
        {
            keys.Add("href");
        }
        if (sectionType is SectionCatalog.ProductRailFlash
            or SectionCatalog.ProductRailMostViewed
            or SectionCatalog.BestSellers
            or SectionCatalog.NewestProducts
            or SectionCatalog.LatestArticles)
        {
            keys.Add("itemCount");
        }
        if (sectionType is SectionCatalog.ProductRailFlash
            or SectionCatalog.ProductRailMostViewed
            or SectionCatalog.NewestProducts)
        {
            keys.Add("sourceKind");
        }
        return keys;
    }

    private async Task SaveDefinitionAsync(PageDefinition definition, CancellationToken cancellationToken)
    {
        _db.PageDefinitions.Update(definition);
        var existingSections = await _db.PageSections
            .Where(section => section.PageDefinitionId == definition.PageDefinitionId)
            .ToListAsync(cancellationToken);
        var currentIds = definition.Sections.Select(section => section.PageSectionId).ToHashSet();
        foreach (var removed in existingSections.Where(section => !currentIds.Contains(section.PageSectionId)))
            _db.PageSections.Remove(removed);
        foreach (var section in definition.Sections)
        {
            if (existingSections.Any(existing => existing.PageSectionId == section.PageSectionId))
                _db.PageSections.Update(section);
            else
                _db.PageSections.Add(section);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}
