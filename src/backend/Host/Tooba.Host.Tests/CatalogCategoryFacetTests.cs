using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.AccessControl.Application;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.Host.Tests;

/// <summary>
/// پوشش پیکربندی facet رده و resolver مؤثر.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogCategoryFacetTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_catalog_facet_a")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Only_filterable_effective_attributes_can_be_configured()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-facet-elig", "tenant-facet-elig"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var category = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "الکترونیک" }, CancellationToken.None);
        var filterableId = await dir.CreateAttributeDefinitionAsync(
            "brand",
            CatalogAttributeValueKind.Enumeration,
            true,
            new Dictionary<string, string> { ["fa-IR"] = "برند" },
            CancellationToken.None);
        var notFilterableId = await dir.CreateAttributeDefinitionAsync(
            "weight",
            CatalogAttributeValueKind.Number,
            false,
            new Dictionary<string, string> { ["fa-IR"] = "وزن" },
            CancellationToken.None);

        await dir.BindCategoryAttributeAsync(
            category.CategoryId,
            filterableId,
            0,
            new CategoryAttributeAssignmentFlags(false, true, false, false),
            CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            category.CategoryId,
            notFilterableId,
            1,
            new CategoryAttributeAssignmentFlags(false, false, false, false),
            CancellationToken.None);

        await dir.UpsertCategoryFacetConfigurationAsync(
            category.CategoryId,
            filterableId,
            new CategoryFacetConfigurationInput(CatalogFacetDisplayType.CheckboxList, 0, true, false, false, true),
            CancellationToken.None);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.UpsertCategoryFacetConfigurationAsync(
                category.CategoryId,
                notFilterableId,
                new CategoryFacetConfigurationInput(CatalogFacetDisplayType.Range, 1, true, false, false, false),
                CancellationToken.None));
    }

    [SkippableFact]
    public async Task Facet_inheritance_child_override_and_remove_fallback()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-facet-inh", "tenant-facet-inh"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var parent = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "دیجیتال" }, CancellationToken.None);
        var child = await dir.CreateCategoryAsync(parent.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "موبایل" }, CancellationToken.None);
        var sibling = await dir.CreateCategoryAsync(parent.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "تبلت" }, CancellationToken.None);
        var brandId = await dir.CreateAttributeDefinitionAsync(
            "brand",
            CatalogAttributeValueKind.Enumeration,
            true,
            new Dictionary<string, string> { ["fa-IR"] = "برند" },
            CancellationToken.None);

        foreach (var catId in new[] { parent.CategoryId, child.CategoryId, sibling.CategoryId })
        {
            await dir.BindCategoryAttributeAsync(
                catId,
                brandId,
                0,
                new CategoryAttributeAssignmentFlags(false, true, false, false),
                CancellationToken.None);
        }

        await dir.UpsertCategoryFacetConfigurationAsync(
            parent.CategoryId,
            brandId,
            new CategoryFacetConfigurationInput(CatalogFacetDisplayType.CheckboxList, 0, true, false, false, true),
            CancellationToken.None);

        var inheritedChild = await dir.GetEffectiveCategoryFacetsAsync(child.CategoryId, "fa-IR", CancellationToken.None);
        var inheritedSibling = await dir.GetEffectiveCategoryFacetsAsync(sibling.CategoryId, "fa-IR", CancellationToken.None);
        Assert.Single(inheritedChild);
        Assert.True(inheritedChild[0].IsInherited);
        Assert.Equal(CatalogFacetDisplayType.CheckboxList, inheritedChild[0].DisplayType);
        Assert.Single(inheritedSibling);

        await dir.UpsertCategoryFacetConfigurationAsync(
            child.CategoryId,
            brandId,
            new CategoryFacetConfigurationInput(CatalogFacetDisplayType.SearchableSelect, 0, true, true, false, false),
            CancellationToken.None);

        var overriddenChild = await dir.GetEffectiveCategoryFacetsAsync(child.CategoryId, "fa-IR", CancellationToken.None);
        var siblingAfter = await dir.GetEffectiveCategoryFacetsAsync(sibling.CategoryId, "fa-IR", CancellationToken.None);
        Assert.Equal(CatalogFacetDisplayType.SearchableSelect, Assert.Single(overriddenChild).DisplayType);
        Assert.False(Assert.Single(overriddenChild).IsInherited);
        Assert.Equal(CatalogFacetDisplayType.CheckboxList, Assert.Single(siblingAfter).DisplayType);

        await dir.RemoveCategoryFacetOverrideAsync(child.CategoryId, brandId, CancellationToken.None);
        var fallbackChild = await dir.GetEffectiveCategoryFacetsAsync(child.CategoryId, "fa-IR", CancellationToken.None);
        Assert.True(Assert.Single(fallbackChild).IsInherited);
        Assert.Equal(CatalogFacetDisplayType.CheckboxList, fallbackChild[0].DisplayType);
    }

    [SkippableFact]
    public async Task Invalid_display_type_for_value_kind_is_rejected()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-facet-val", "tenant-facet-val"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var dir = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var category = await dir.CreateCategoryAsync(null, new Dictionary<string, string> { ["fa-IR"] = "کتاب" }, CancellationToken.None);
        var boolId = await dir.CreateAttributeDefinitionAsync(
            "digital",
            CatalogAttributeValueKind.Boolean,
            false,
            new Dictionary<string, string> { ["fa-IR"] = "دیجیتال" },
            CancellationToken.None);
        await dir.BindCategoryAttributeAsync(
            category.CategoryId,
            boolId,
            0,
            new CategoryAttributeAssignmentFlags(false, true, false, false),
            CancellationToken.None);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dir.UpsertCategoryFacetConfigurationAsync(
                category.CategoryId,
                boolId,
                new CategoryFacetConfigurationInput(CatalogFacetDisplayType.Range, 0, true, false, false, false),
                CancellationToken.None));
    }

    [SkippableFact]
    public async Task FacetDirectory_result_codes_and_behavior_matrix()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-facet-w6", "tenant-facet-w6"));
        await using var db = CreateCatalogDb(cs, commerce);
        await db.Database.EnsureCreatedAsync();
        var guard = new OpenCatalogUseCaseGuard();
        var catalog = new CatalogDirectory(db, guard);
        var facets = new FacetDirectory(db, guard);

        var missingCat = await facets.GetEffectiveFacetsAsync(Guid.NewGuid(), "fa-IR", CancellationToken.None);
        Assert.True(missingCat.IsFailure);
        Assert.Equal(CatalogErrorCodes.FacetCategoryMissing, missingCat.FirstError.Code);

        var category = await catalog.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "فیلتر" }, CancellationToken.None);
        var brandId = await catalog.CreateAttributeDefinitionAsync(
            "brand",
            CatalogAttributeValueKind.Enumeration,
            true,
            new Dictionary<string, string> { ["fa-IR"] = "برند" },
            CancellationToken.None);
        var weightId = await catalog.CreateAttributeDefinitionAsync(
            "weight",
            CatalogAttributeValueKind.Number,
            false,
            new Dictionary<string, string> { ["fa-IR"] = "وزن" },
            CancellationToken.None);
        await catalog.BindCategoryAttributeAsync(
            category.CategoryId,
            brandId,
            0,
            new CategoryAttributeAssignmentFlags(false, true, false, false),
            CancellationToken.None);
        await catalog.BindCategoryAttributeAsync(
            category.CategoryId,
            weightId,
            1,
            new CategoryAttributeAssignmentFlags(false, false, false, false),
            CancellationToken.None);

        var emptyEffective = await facets.GetEffectiveFacetsAsync(category.CategoryId, "fa-IR", CancellationToken.None);
        Assert.True(emptyEffective.IsSuccess);
        Assert.Empty(emptyEffective.Value);

        var storefrontEmpty = await facets.GetEffectiveFacetsAsync(category.CategoryId, "en-US", CancellationToken.None);
        Assert.True(storefrontEmpty.IsSuccess);

        var notFilterable = await facets.UpsertConfigurationAsync(
            category.CategoryId,
            weightId,
            new CategoryFacetConfigurationInput(CatalogFacetDisplayType.Range, 0, true, false, false, false),
            CancellationToken.None);
        Assert.True(notFilterable.IsFailure);
        Assert.Equal(CatalogErrorCodes.FacetNotFilterable, notFilterable.FirstError.Code);

        var missingDef = await facets.UpsertConfigurationAsync(
            category.CategoryId,
            Guid.NewGuid(),
            new CategoryFacetConfigurationInput(CatalogFacetDisplayType.CheckboxList, 0, true, false, false, true),
            CancellationToken.None);
        Assert.True(missingDef.IsFailure);
        Assert.Equal(CatalogErrorCodes.FacetDefinitionMissing, missingDef.FirstError.Code);

        var unboundDef = await catalog.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var schemaMissing = await facets.UpsertConfigurationAsync(
            category.CategoryId,
            unboundDef,
            new CategoryFacetConfigurationInput(CatalogFacetDisplayType.CheckboxList, 0, true, false, false, true),
            CancellationToken.None);
        Assert.True(schemaMissing.IsFailure);
        Assert.Equal(CatalogErrorCodes.FacetSchemaMissing, schemaMissing.FirstError.Code);

        var badDisplay = await facets.UpsertConfigurationAsync(
            category.CategoryId,
            brandId,
            new CategoryFacetConfigurationInput(CatalogFacetDisplayType.Range, 0, true, false, false, false),
            CancellationToken.None);
        Assert.True(badDisplay.IsFailure);
        Assert.Equal(CatalogErrorCodes.FacetDisplayTypeInvalid, badDisplay.FirstError.Code);

        var upsert = await facets.UpsertConfigurationAsync(
            category.CategoryId,
            brandId,
            new CategoryFacetConfigurationInput(CatalogFacetDisplayType.SearchableSelect, 5, true, true, false, true),
            CancellationToken.None);
        Assert.True(upsert.IsSuccess);

        var local = await facets.ListLocalConfigurationsAsync(category.CategoryId, CancellationToken.None);
        Assert.True(local.IsSuccess);
        Assert.Single(local.Value);
        Assert.True(local.Value[0].IsSearchable);

        var effective = await facets.GetEffectiveFacetsAsync(category.CategoryId, "fa-IR", CancellationToken.None);
        Assert.True(effective.IsSuccess);
        Assert.Single(effective.Value);
        Assert.Equal("برند", effective.Value[0].LocalizedName);
        Assert.False(effective.Value[0].IsInherited);

        var removeMissing = await facets.RemoveOverrideAsync(category.CategoryId, Guid.NewGuid(), CancellationToken.None);
        Assert.True(removeMissing.IsFailure);
        Assert.Equal(CatalogErrorCodes.FacetOverrideMissing, removeMissing.FirstError.Code);

        var badReorder = await facets.ReorderConfigurationsAsync(
            category.CategoryId,
            [Guid.NewGuid()],
            CancellationToken.None);
        Assert.True(badReorder.IsFailure);
        Assert.Equal(CatalogErrorCodes.FacetReorderInvalid, badReorder.FirstError.Code);

        var goodReorder = await facets.ReorderConfigurationsAsync(
            category.CategoryId,
            [brandId],
            CancellationToken.None);
        Assert.True(goodReorder.IsSuccess);
        var afterReorder = await facets.ListLocalConfigurationsAsync(category.CategoryId, CancellationToken.None);
        Assert.Equal(0, Assert.Single(afterReorder.Value).SortOrder);

        var removeOk = await facets.RemoveOverrideAsync(category.CategoryId, brandId, CancellationToken.None);
        Assert.True(removeOk.IsSuccess);
        Assert.Empty((await facets.ListLocalConfigurationsAsync(category.CategoryId, CancellationToken.None)).Value);
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }
}
