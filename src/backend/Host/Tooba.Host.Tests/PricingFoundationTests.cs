using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Application;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure;
using Tooba.Pricing.Infrastructure.Events;
using Tooba.Pricing.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation Pricing: مبلغ نوشته‌شده جدا از Product/Offer، بازار/ارز/کانال صریح، و ایزولهٔ Tenant.
/// </summary>
[Collection("PostgresSerial")]
public sealed class PricingFoundationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <summary>
    /// Postgres واقعی را برای isolation بالا می‌آورد.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_pricing_a")
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
    public void Product_and_offer_have_no_price_fields()
    {
        var product = typeof(CatalogProduct).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var offer = typeof(SellerOffer).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Price", product);
        Assert.DoesNotContain("Amount", product);
        Assert.DoesNotContain("Currency", product);
        Assert.DoesNotContain("Price", offer);
        Assert.DoesNotContain("Amount", offer);
        Assert.DoesNotContain("Money", offer);
        Assert.DoesNotContain("Currency", offer);
        Assert.Contains("Amount", typeof(AuthoredPrice).GetProperties().Select(p => p.Name));
        Assert.Contains("Currency", typeof(AuthoredPrice).GetProperties().Select(p => p.Name));
        Assert.True(Money.Create(10.555m, "IRR").Amount == 11m);
        Assert.ThrowsAny<Exception>(() => CurrencyCode.Parse("TOMAN"));
        Assert.ThrowsAny<Exception>(() => CurrencyCode.Parse("fa-IR"));
    }

    [Fact]
    public void Pricing_projects_do_not_reference_masstransit_authzed_or_foreign_infrastructure()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Pricing", "Tooba.Pricing.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Pricing", "Tooba.Pricing.Application"),
                     Path.Combine(root, "src", "backend", "Modules", "Pricing", "Tooba.Pricing.Infrastructure"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tooba.Offer.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity", csproj, StringComparison.Ordinal);
        }

        Assert.Contains("Tooba.Offer.Application", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Pricing", "Tooba.Pricing.Infrastructure", "Tooba.Pricing.Infrastructure.csproj")));
        Assert.Equal("pricing", PricingDbContext.Schema);
        Assert.DoesNotContain("MassTransit", typeof(AuthoredPrice).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("MassTransit", typeof(IPriceDirectory).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.Equal(typeof(SalesChannel), typeof(AuthoredPrice).GetProperty("Channel")!.PropertyType);
    }

    [SkippableFact]
    public async Task Pricing_selection_overlap_tax_exclusive_and_tenant_isolation_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_pricing_b'";
            if (await cmd.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_pricing_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_pricing_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-a", "tenant-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-b", "tenant-b"));

        await using var catalogA = CreateCatalogDb(csA, commerceA);
        await using var partyA = CreatePartyDb(csA, commerceA);
        await using var offerA = CreateOfferDb(csA, commerceA);
        await using var pricingA = CreatePricingDb(csA, commerceA);
        await using var catalogB = CreateCatalogDb(csB, commerceB);
        await using var partyB = CreatePartyDb(csB, commerceB);
        await using var offerB = CreateOfferDb(csB, commerceB);
        await using var pricingB = CreatePricingDb(csB, commerceB);
        await catalogA.Database.MigrateAsync();
        await partyA.Database.MigrateAsync();
        await offerA.Database.MigrateAsync();
        await pricingA.Database.MigrateAsync();
        await catalogB.Database.MigrateAsync();
        await partyB.Database.MigrateAsync();
        await offerB.Database.MigrateAsync();
        await pricingB.Database.MigrateAsync();

        var catalogDirA = new CatalogDirectory(catalogA, new OpenCatalogUseCaseGuard());
        var partyDirA = new PartyDirectory(partyA);
        var offerDirA = new OfferDirectory(offerA, new OpenOfferUseCaseGuard(), catalogDirA, partyDirA);
        var priceDirA = new PriceDirectory(pricingA, new OpenPricingUseCaseGuard(), offerDirA);
        var catalogDirB = new CatalogDirectory(catalogB, new OpenCatalogUseCaseGuard());
        var partyDirB = new PartyDirectory(partyB);
        var offerDirB = new OfferDirectory(offerB, new OpenOfferUseCaseGuard(), catalogDirB, partyDirB);
        var priceDirB = new PriceDirectory(pricingB, new OpenPricingUseCaseGuard(), offerDirB);

        var names = new Dictionary<string, string> { ["fa-IR"] = "پیراهن", ["en-US"] = "Shirt" };
        var product = await catalogDirA.CreateProductAsync(CatalogProductKind.PhysicalGood, "shirt-p", null, names, CancellationToken.None);
        var colorId = await catalogDirA.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await catalogDirA.AddAttributeOptionAsync(colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "سیاه" }, CancellationToken.None);
        var variant = await catalogDirA.CreateVariantAsync(product.ProductId, "SHIRT-P", [(colorId, "ignored", black)], CancellationToken.None);
        var seller = await partyDirA.CreateOrganizationAsync("فروشنده قیمت", null, CancellationToken.None);
        var offer = await offerDirA.CreateOfferAsync(variant.VariantId, seller.PartyId, SalesChannel.Marketplace, "SKU-P", CancellationToken.None);

        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var mid = DateTimeOffset.Parse("2026-06-01T00:00:00Z");
        var end = DateTimeOffset.Parse("2026-12-31T00:00:00Z");
        Assert.ThrowsAny<Exception>(() => AuthoredPrice.Create(offer.OfferId, "IR", SalesChannel.Marketplace, 10, "IRR", start, start, DateTimeOffset.UtcNow));

        var created = await priceDirA.CreatePriceAsync(offer.OfferId, "IR", SalesChannel.Marketplace, 100000, "IRR", start, end, CancellationToken.None);
        Assert.True(created.TaxExclusive);
        Assert.True(created.IsAuthored);
        await priceDirA.ActivateAsync(created.PriceId, CancellationToken.None);

        var resolved = await priceDirA.ResolvePriceAsync(
            new PriceResolutionQuery(offer.OfferId, "IR", SalesChannel.Marketplace, "IRR", mid, null, null, null),
            CancellationToken.None);
        Assert.NotNull(resolved);
        Assert.Equal(100000m, resolved!.Amount);
        Assert.Equal("IRR", resolved.Currency);
        Assert.Null(await priceDirA.ResolvePriceAsync(
            new PriceResolutionQuery(offer.OfferId, "UK", SalesChannel.Marketplace, "IRR", mid, null, null, null),
            CancellationToken.None));
        Assert.Null(await priceDirA.ResolvePriceAsync(
            new PriceResolutionQuery(offer.OfferId, "IR", SalesChannel.Direct, "IRR", mid, null, null, null),
            CancellationToken.None));
        Assert.Null(await priceDirA.ResolvePriceAsync(
            new PriceResolutionQuery(offer.OfferId, "IR", SalesChannel.Marketplace, "USD", mid, null, null, null),
            CancellationToken.None));
        Assert.Null(await priceDirA.ResolvePriceAsync(
            new PriceResolutionQuery(offer.OfferId, "IR", SalesChannel.Marketplace, "IRR", DateTimeOffset.Parse("2025-01-01T00:00:00Z"), null, null, null),
            CancellationToken.None));

        var overlap = await priceDirA.CreatePriceAsync(offer.OfferId, "IR", SalesChannel.Marketplace, 200000, "IRR", DateTimeOffset.Parse("2026-03-01T00:00:00Z"), null, CancellationToken.None);
        await Assert.ThrowsAnyAsync<Exception>(() => priceDirA.ActivateAsync(overlap.PriceId, CancellationToken.None));

        var uk = await priceDirA.CreatePriceAsync(offer.OfferId, "UK", SalesChannel.Marketplace, 12, "GBP", start, null, CancellationToken.None);
        await priceDirA.ActivateAsync(uk.PriceId, CancellationToken.None);
        Assert.Equal(12m, (await priceDirA.ResolvePriceAsync(
            new PriceResolutionQuery(offer.OfferId, "UK", SalesChannel.Marketplace, "GBP", mid, null, null, 2m),
            CancellationToken.None))!.Amount);

        await priceDirA.ChangeAmountAsync(created.PriceId, 110000, "IRR", CancellationToken.None);
        await priceDirA.ExpireAsync(created.PriceId, CancellationToken.None);
        Assert.Null(await priceDirA.ResolvePriceAsync(
            new PriceResolutionQuery(offer.OfferId, "IR", SalesChannel.Marketplace, "IRR", DateTimeOffset.UtcNow, null, null, null),
            CancellationToken.None));

        var outbox = await pricingA.OutboxMessages.AsNoTracking().ToListAsync();
        Assert.Contains(outbox, row => row.EventType == PriceCreatedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == PriceActivatedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == PriceChangedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == PriceExpiredIntegrationEvent.EventTypeName);

        var productB = await catalogDirB.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "other-p",
            null,
            new Dictionary<string, string> { ["en-US"] = "Other" },
            CancellationToken.None);
        var sizeId = await catalogDirB.CreateAttributeDefinitionAsync(
            "size",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["en-US"] = "Size" },
            CancellationToken.None);
        var m = await catalogDirB.AddAttributeOptionAsync(sizeId, "m", new Dictionary<string, string> { ["en-US"] = "M" }, CancellationToken.None);
        var variantB = await catalogDirB.CreateVariantAsync(productB.ProductId, "OTHER-P", [(sizeId, "ignored", m)], CancellationToken.None);
        var sellerB = await partyDirB.CreateOrganizationAsync("Tenant B Seller", null, CancellationToken.None);
        var offerBRef = await offerDirB.CreateOfferAsync(variantB.VariantId, sellerB.PartyId, SalesChannel.Direct, "B-P", CancellationToken.None);
        var priceB = await priceDirB.CreatePriceAsync(offerBRef.OfferId, "UK", SalesChannel.Direct, 5, "GBP", start, null, CancellationToken.None);
        await priceDirB.ActivateAsync(priceB.PriceId, CancellationToken.None);
        Assert.Null(await priceDirB.ResolvePriceAsync(
            new PriceResolutionQuery(offer.OfferId, "IR", SalesChannel.Marketplace, "IRR", mid, null, null, null),
            CancellationToken.None));
        Assert.Null(await priceDirA.ResolvePriceAsync(
            new PriceResolutionQuery(offerBRef.OfferId, "UK", SalesChannel.Direct, "GBP", mid, null, null, null),
            CancellationToken.None));
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

    private static PartyDbContext CreatePartyDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PartyOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PartyDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PartyDbContext.Schema, typeof(PartyDbContext));
        options.AddInterceptors(interceptor);
        return new PartyDbContext(options.Options);
    }

    private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new OfferOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<OfferDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));
        options.AddInterceptors(interceptor);
        return new OfferDbContext(options.Options);
    }

    private static PricingDbContext CreatePricingDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PricingOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PricingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PricingDbContext.Schema, typeof(PricingDbContext));
        options.AddInterceptors(interceptor);
        return new PricingDbContext(options.Options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
