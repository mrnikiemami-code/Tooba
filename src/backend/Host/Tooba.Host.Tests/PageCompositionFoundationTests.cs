using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.PageComposition.Application;
using Tooba.PageComposition.Domain;
using Tooba.PageComposition.Infrastructure;
using Tooba.PageComposition.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>پوشش foundation Page Composition: catalog، ترتیب پیش‌فرض، reorder، visibility، config و tenant isolation.</summary>
[Collection("PostgresSerial")]
public sealed class PageCompositionFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_page_composition")
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

    /// <summary>مرز schema و ثبت دایرکتوری Page Composition.</summary>
    [Fact]
    public void PageComposition_module_boundary_static_checks()
    {
        Assert.Equal("page_composition", PageCompositionDbContext.Schema);
        Assert.NotNull(typeof(IPageCompositionDirectory).GetMethod(nameof(IPageCompositionDirectory.GetCatalogAsync)));
        Assert.NotNull(typeof(IPageCompositionDirectory).GetMethod(nameof(IPageCompositionDirectory.AdminRestoreDefaultHomeAsync)));
        Assert.Equal(SectionCatalog.Hero, SectionCatalog.DefaultHomeSectionTypes[0]);
    }

    /// <summary>نوع section ناشناخته، ترتیب پیش‌فرض، reorder، hide/show، config ممنوع، restore و tenant isolation.</summary>
    [SkippableFact]
    public async Task Catalog_order_visibility_config_restore_and_tenant_isolation_behave()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var directory = new PageCompositionDirectory(db);
        var tenantAlpha = PageCompositionTenantIds.StoreAlpha;
        var tenantBeta = PageCompositionTenantIds.StoreBeta;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.AdminAddSectionAsync(
                tenantAlpha,
                null,
                new AddHomeSectionCommand("unknown_widget", SectionCatalog.DefaultVariant, null),
                CancellationToken.None));

        var alpha = await directory.AdminGetHomeAsync(tenantAlpha, null, CancellationToken.None);
        Assert.Equal(SectionCatalog.DefaultHomeSectionTypes.Count, alpha.Sections.Count);
        Assert.Equal(SectionCatalog.DefaultHomeSectionTypes, alpha.Sections.Select(section => section.SectionType).ToList());

        var reorderedIds = alpha.Sections
            .OrderByDescending(section => section.DisplayOrder)
            .Select(section => section.PageSectionId)
            .ToList();
        var reordered = await directory.AdminReorderHomeAsync(tenantAlpha, null, reorderedIds, CancellationToken.None);
        Assert.Equal(reorderedIds, reordered.Sections.Select(section => section.PageSectionId).ToList());

        var hero = alpha.Sections[0];
        await directory.AdminUpdateSectionAsync(
            tenantAlpha,
            null,
            hero.PageSectionId,
            new UpdateHomeSectionCommand(IsVisible: false, ConfigurationJson: null, Variant: null),
            CancellationToken.None);
        var publicAlpha = await directory.GetHomeCompositionAsync(tenantAlpha, null, CancellationToken.None);
        Assert.DoesNotContain(publicAlpha.Sections, section => section.PageSectionId == hero.PageSectionId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.AdminUpdateSectionAsync(
                tenantAlpha,
                null,
                hero.PageSectionId,
                new UpdateHomeSectionCommand(null, """{"className":"danger"}""", null),
                CancellationToken.None));

        var restored = await directory.AdminRestoreDefaultHomeAsync(tenantAlpha, null, CancellationToken.None);
        Assert.Equal(SectionCatalog.DefaultHomeSectionTypes, restored.Sections.Select(section => section.SectionType).ToList());
        Assert.All(restored.Sections, section => Assert.True(section.IsVisible));

        await PageCompositionDevelopmentSeed.EnsureHomeAsync(db, tenantBeta, DateTimeOffset.UtcNow, CancellationToken.None);
        var beta = await directory.AdminGetHomeAsync(tenantBeta, null, CancellationToken.None);
        var betaHero = beta.Sections[0];
        await directory.AdminUpdateSectionAsync(
            tenantBeta,
            null,
            betaHero.PageSectionId,
            new UpdateHomeSectionCommand(IsVisible: false, ConfigurationJson: null, Variant: null),
            CancellationToken.None);

        var alphaAfter = await directory.AdminGetHomeAsync(tenantAlpha, null, CancellationToken.None);
        Assert.True(alphaAfter.Sections[0].IsVisible);
        var betaAfter = await directory.AdminGetHomeAsync(tenantBeta, null, CancellationToken.None);
        Assert.False(betaAfter.Sections[0].IsVisible);
    }

    private static PageCompositionDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PageCompositionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            PageCompositionDbContext.Schema,
            typeof(PageCompositionDbContext));
        return new PageCompositionDbContext(options.Options);
    }
}
