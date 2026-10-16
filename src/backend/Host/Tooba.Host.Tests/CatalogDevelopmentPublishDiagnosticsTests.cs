using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Models;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.Catalog.Infrastructure.Events;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش تشخیص Development-only رد شدن انتشار در دانهٔ Catalog:
/// کدهای واقعی Missing[] باید منتشر شوند، رد شدن باید رد بماند و خارج از Development چیزی منتشر نشود.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CatalogDevelopmentPublishDiagnosticsTests : IAsyncLifetime
{
    private const string SeedStage = CatalogDevelopmentPublishDiagnostics.AttributeSchemaStage;

    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_publish_diagnostics")
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

    [Fact]
    public void Summarize_projects_only_the_real_missing_rule_codes()
    {
        var productId = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var readiness = new ProductPublishReadiness(
            false,
            true,
            true,
            true,
            true,
            false,
            false,
            [
                new ProductPublishMissingRequirement("media", ProductPublishRules.MessageMediaIncompleteFa, "media"),
                new ProductPublishMissingRequirement("seo", ProductPublishRules.MessageSeoIncompleteFa, "seo"),
            ],
            ProductPublishRules.SummarizeMissingFa(2));

        var summary = CatalogDevelopmentPublishDiagnostics.Summarize(SeedStage, productId, readiness);

        Assert.Equal(
            $"stage={SeedStage}; product={productId:D}; isReady=False; missing=[media,seo]",
            summary);
        Assert.Contains(SeedStage, summary, StringComparison.Ordinal);
        Assert.Contains(productId.ToString("D"), summary, StringComparison.Ordinal);
        Assert.DoesNotContain("offer", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("price", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stock", summary, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Refused_publish_emits_real_missing_codes_and_still_refuses_in_development()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-publish-diag", "tenant-publish-diag"));
        await using var db = CreateCatalogDb(_container!.GetConnectionString(), commerce);
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var (productId, _) = await SeedDraftProductMissingMediaAndSeoAsync(catalog, db);

        var sink = new LogSink();
        var provider = BuildProvider(Environments.Development, sink);

        var refusal = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            CatalogDevelopmentPublishDiagnostics.PublishWithDiagnosticsAsync(
                catalog, provider, productId, SeedStage, CancellationToken.None));

        // The refusal itself is unchanged: same classification and same public message.
        Assert.Equal(typeof(InvalidOperationException), refusal.GetType());
        Assert.Equal(ProductPublishRules.MessageNotReadyFa, refusal.Message);

        // The product was NOT force-published.
        Assert.Equal(
            CatalogPublicationStatus.Draft,
            (await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == productId)).Status);

        // The emitted codes are the REAL ones produced by the existing readiness contract for that product.
        var realReadiness = await catalog.GetProductPublishReadinessAsync(productId, "fa-IR", CancellationToken.None);
        var realCodes = realReadiness.MissingRequirements.Select(m => m.Code).ToArray();
        Assert.NotEmpty(realCodes);

        var emitted = Assert.Single(sink.Messages);
        Assert.Contains(CatalogDevelopmentPublishDiagnostics.LoggerCategory, emitted, StringComparison.Ordinal);
        Assert.Contains(
            CatalogDevelopmentPublishDiagnostics.Summarize(SeedStage, productId, realReadiness),
            emitted,
            StringComparison.Ordinal);
        foreach (var code in realCodes)
        {
            Assert.Contains(code, emitted, StringComparison.Ordinal);
        }

        // Diagnostics stay Catalog-publish-only: no commercial dependencies leak into the readiness codes.
        Assert.DoesNotContain("offer", emitted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("price", emitted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stock", emitted, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Refused_publish_emits_nothing_outside_development()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-publish-diag-prod", "tenant-publish-diag-prod"));
        await using var db = CreateCatalogDb(_container!.GetConnectionString(), commerce);
        await db.Database.EnsureCreatedAsync();
        var catalog = new CatalogDirectory(db, new OpenCatalogUseCaseGuard());

        var (productId, _) = await SeedDraftProductMissingMediaAndSeoAsync(catalog, db);

        var sink = new LogSink();
        var provider = BuildProvider(Environments.Production, sink);

        var refusal = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            CatalogDevelopmentPublishDiagnostics.PublishWithDiagnosticsAsync(
                catalog, provider, productId, SeedStage, CancellationToken.None));

        Assert.Equal(ProductPublishRules.MessageNotReadyFa, refusal.Message);
        Assert.Empty(sink.Messages);
        Assert.Equal(
            CatalogPublicationStatus.Draft,
            (await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == productId)).Status);
    }

    private static async Task<(Guid ProductId, Guid CategoryId)> SeedDraftProductMissingMediaAndSeoAsync(
        ICatalogDirectory catalog,
        CatalogDbContext db)
    {
        var l1 = await catalog.CreateCategoryAsync(
            null, new Dictionary<string, string> { ["fa-IR"] = "ریشه" }, CancellationToken.None);
        var l2 = await catalog.CreateCategoryAsync(
            l1.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "میانی" }, CancellationToken.None);
        var l3 = await catalog.CreateCategoryAsync(
            l2.CategoryId, new Dictionary<string, string> { ["fa-IR"] = "برگ" }, CancellationToken.None);

        var product = await catalog.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "diagnostics-draft",
            null,
            new Dictionary<string, string> { ["fa-IR"] = "کالای تشخیص" },
            CancellationToken.None);
        await catalog.AssignCategoryAsync(product.ProductId, l3.CategoryId, CancellationToken.None);
        await db.SaveChangesAsync();

        return (product.ProductId, l3.CategoryId);
    }

    private static ServiceProvider BuildProvider(string environmentName, LogSink sink)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment(environmentName));
        services.AddLogging(builder => builder.AddProvider(new SinkLoggerProvider(sink)));
        return services.BuildServiceProvider();
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

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Tooba.Host.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class LogSink
    {
        public List<string> Messages { get; } = [];
    }

    private sealed class SinkLoggerProvider(LogSink sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new SinkLogger(sink, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class SinkLogger(LogSink sink, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Messages.Add($"{categoryName}|{formatter(state, exception)}");
        }
    }
}
