using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain.Products;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-112 — source-level reproduction of the TB-111 Development startup collision.
/// <para>
/// The Catalog attribute-schema Development seed owns attribute code <c>color</c> and its <c>black</c> option
/// first; the legacy workspace demo seed used to unconditionally create the same code and failed with
/// <c>catalog.attribute.code.duplicate</c> (<c>WorkspaceDemoProductSeed.SeedProductAsync</c>). This test runs
/// the real affected operation — <see cref="WorkspaceDemoProductSeed.SeedProductAsync"/> — after the schema
/// seed, and proves the existing definition/option are reused with the SAME persistent identifiers instead of
/// being re-created. It does not run the whole legacy bootstrap chain (that is TB-113 runtime proof).
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class WorkspaceDemoProductSeedCollisionRegressionTests : IAsyncLifetime
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
                .WithDatabase("tooba_workspace_attribute_collision")
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
    public async Task Legacy_workspace_seed_completes_by_reusing_the_schema_seed_attribute_and_option()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateCatalogDb(_container.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());
        await using var provider = new ServiceCollection()
            .AddSingleton<ICatalogDirectory>(catalog)
            .AddSingleton(db)
            .BuildServiceProvider();

        // The Catalog attribute-schema Development seed owns the shared code first (the TB-111 precondition).
        await CatalogAttributeSchemaDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);

        var schemaSeedColor = await db.AttributeDefinitions.AsNoTracking()
            .SingleAsync(x => x.Code == WorkspaceDemoProductSeed.ColorAttributeCode);
        var schemaSeedBlack = await db.AttributeOptions.AsNoTracking()
            .SingleAsync(x => x.DefinitionId == schemaSeedColor.DefinitionId
                && x.Code == WorkspaceDemoProductSeed.BlackOptionCode);

        // Before TB-112 this threw InvalidOperationException("catalog.attribute.code.duplicate").
        var seed = new WorkspaceDemoProductSeed(db, catalog);
        var variantId = await seed.SeedProductAsync(CancellationToken.None);
        Assert.NotEqual(Guid.Empty, variantId);

        // The legacy seed completed, and it reused rather than re-created the shared schema.
        var workspaceProduct = await db.Products.AsNoTracking()
            .SingleAsync(x => x.SlugSeam == WorkspaceDemoProductSeed.SeedSlug);
        Assert.Equal(CatalogPublicationStatus.Published, workspaceProduct.Status);
        Assert.Equal(
            1,
            await db.AttributeDefinitions.AsNoTracking()
                .CountAsync(x => x.Code == WorkspaceDemoProductSeed.ColorAttributeCode));
        Assert.Equal(
            schemaSeedColor.DefinitionId,
            (await db.AttributeDefinitions.AsNoTracking()
                .SingleAsync(x => x.Code == WorkspaceDemoProductSeed.ColorAttributeCode)).DefinitionId);
        Assert.Equal(
            schemaSeedBlack.OptionId,
            (await db.AttributeOptions.AsNoTracking()
                .SingleAsync(x => x.DefinitionId == schemaSeedColor.DefinitionId
                    && x.Code == WorkspaceDemoProductSeed.BlackOptionCode)).OptionId);

        // The workspace variant still points at the reused axis definition.
        Assert.Single(
            await db.VariantAttributeValues.AsNoTracking()
                .Where(x => x.VariantId == variantId && x.DefinitionId == schemaSeedColor.DefinitionId)
                .ToListAsync());
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString)
    {
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-workspace-collision", "tenant-workspace-collision"));
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }
}
