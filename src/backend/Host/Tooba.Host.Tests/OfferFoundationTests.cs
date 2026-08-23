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
using Tooba.Offer.Infrastructure.Events;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Domain;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation Offer: listing فروشنده جدا از Product/قیمت/موجودی و ایزولهٔ Tenant.
/// Single-Store هم از Offer استفاده می‌کند و Price را روی CatalogProduct نمی‌گذارد.
/// </summary>
public sealed class OfferFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_offer_a")
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
    public void Offer_has_no_price_stock_or_user_id()
    {
        var names = typeof(SellerOffer).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("Amount", names);
        Assert.DoesNotContain("Money", names);
        Assert.DoesNotContain("Currency", names);
        Assert.DoesNotContain("Stock", names);
        Assert.DoesNotContain("Quantity", names);
        Assert.DoesNotContain("Inventory", names);
        Assert.DoesNotContain("UserId", names);
        Assert.Contains("SellerPartyId", names);
        Assert.Contains("CatalogVariantId", names);
        Assert.Contains("Status", names);
        Assert.Contains("Channel", names);
    }

    [Fact]
    public void Catalog_product_still_has_no_offer_or_price()
    {
        var names = typeof(CatalogProduct).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("Stock", names);
        Assert.DoesNotContain("OfferId", names);
        Assert.DoesNotContain("SellerId", names);
    }

    [Fact]
    public void Offer_projects_do_not_reference_masstransit_authzed_or_foreign_infrastructure()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Offer", "Tooba.Offer.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Offer", "Tooba.Offer.Application"),
                     Path.Combine(root, "src", "backend", "Modules", "Offer", "Tooba.Offer.Infrastructure"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tooba.Catalog.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Party.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity", csproj, StringComparison.Ordinal);
        }

        Assert.Contains("Tooba.Catalog.Application", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Offer", "Tooba.Offer.Infrastructure", "Tooba.Offer.Infrastructure.csproj")));
        Assert.Contains("Tooba.Party.Application", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Offer", "Tooba.Offer.Infrastructure", "Tooba.Offer.Infrastructure.csproj")));
        Assert.Equal("offer", OfferDbContext.Schema);
        Assert.DoesNotContain("MassTransit", typeof(SellerOffer).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("Authzed.Net", typeof(SellerOffer).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("MassTransit", typeof(IOfferDirectory).Assembly.GetReferencedAssemblies().Select(a => a.Name));
    }

    [SkippableFact]
    public async Task Offer_invariants_multi_seller_sku_scope_and_tenant_isolation_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_offer_b'";
            var exists = await cmd.ExecuteScalarAsync();
            if (exists is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_offer_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_offer_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-a", "tenant-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-b", "tenant-b"));

        await using var catalogA = CreateCatalogDb(csA, commerceA);
        await using var partyA = CreatePartyDb(csA, commerceA);
        await using var offerA = CreateOfferDb(csA, commerceA);
        await using var catalogB = CreateCatalogDb(csB, commerceB);
        await using var partyB = CreatePartyDb(csB, commerceB);
        await using var offerB = CreateOfferDb(csB, commerceB);
        await catalogA.Database.MigrateAsync();
        await partyA.Database.MigrateAsync();
        await offerA.Database.MigrateAsync();
        await catalogB.Database.MigrateAsync();
        await partyB.Database.MigrateAsync();
        await offerB.Database.MigrateAsync();

        var catalogDirA = new CatalogDirectory(catalogA, new OpenCatalogUseCaseGuard());
        var partyDirA = new PartyDirectory(partyA);
        var offerDirA = new OfferDirectory(offerA, new OpenOfferUseCaseGuard(), catalogDirA, partyDirA);
        var catalogDirB = new CatalogDirectory(catalogB, new OpenCatalogUseCaseGuard());
        var partyDirB = new PartyDirectory(partyB);
        var offerDirB = new OfferDirectory(offerB, new OpenOfferUseCaseGuard(), catalogDirB, partyDirB);

        var names = new Dictionary<string, string> { ["fa-IR"] = "پیراهن", ["en-US"] = "Shirt" };
        var product = await catalogDirA.CreateProductAsync(CatalogProductKind.PhysicalGood, "shirt-x", null, names, CancellationToken.None);
        var colorId = await catalogDirA.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await catalogDirA.AddAttributeOptionAsync(colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "سیاه" }, CancellationToken.None);
        var variant = await catalogDirA.CreateVariantAsync(product.ProductId, "SHIRT-BLK", [(colorId, "ignored", black)], CancellationToken.None);

        var sellerA = await partyDirA.CreateOrganizationAsync("فروشنده الف", null, CancellationToken.None);
        var sellerB = await partyDirA.CreateOrganizationAsync("فروشنده ب", null, CancellationToken.None);
        var person = await partyDirA.CreatePersonAsync("شخص غیر فروشنده", CancellationToken.None);

        await Assert.ThrowsAnyAsync<Exception>(() => offerDirA.CreateOfferAsync(
            variant.VariantId,
            person.PartyId,
            SalesChannel.Marketplace,
            "SKU-1",
            CancellationToken.None));

        var offer1 = await offerDirA.CreateOfferAsync(variant.VariantId, sellerA.PartyId, SalesChannel.Marketplace, "SKU-SHARED", CancellationToken.None);
        var offer2 = await offerDirA.CreateOfferAsync(variant.VariantId, sellerB.PartyId, SalesChannel.Marketplace, "SKU-SHARED", CancellationToken.None);
        Assert.NotEqual(offer1.OfferId, offer2.OfferId);
        Assert.Equal(variant.VariantId, offer1.CatalogVariantId);
        Assert.Equal(sellerA.PartyId, offer1.SellerPartyId);
        Assert.NotEqual(offer1.SellerPartyId, offer2.SellerPartyId);

        await Assert.ThrowsAnyAsync<Exception>(() => offerDirA.CreateOfferAsync(
            variant.VariantId,
            sellerA.PartyId,
            SalesChannel.Marketplace,
            "SKU-OTHER",
            CancellationToken.None));
        await Assert.ThrowsAnyAsync<Exception>(() => offerDirA.CreateOfferAsync(
            variant.VariantId,
            sellerA.PartyId,
            SalesChannel.Direct,
            "SKU-SHARED",
            CancellationToken.None));

        var direct = await offerDirA.CreateOfferAsync(variant.VariantId, sellerA.PartyId, SalesChannel.Direct, "SKU-DIRECT", CancellationToken.None);
        await offerDirA.ActivateAsync(offer1.OfferId, CancellationToken.None);
        await offerDirA.SuspendAsync(offer1.OfferId, CancellationToken.None);
        await offerDirA.ArchiveAsync(offer1.OfferId, CancellationToken.None);
        var reopened = await offerDirA.CreateOfferAsync(variant.VariantId, sellerA.PartyId, SalesChannel.Marketplace, "SKU-NEW", CancellationToken.None);
        Assert.NotEqual(offer1.OfferId, reopened.OfferId);
        Assert.NotNull(await offerDirA.FindOfferAsync(direct.OfferId, CancellationToken.None));

        var outbox = await offerA.OutboxMessages.AsNoTracking().ToListAsync();
        Assert.Contains(outbox, row => row.EventType == OfferCreatedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == OfferActivatedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == OfferSuspendedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == OfferArchivedIntegrationEvent.EventTypeName);
        Assert.DoesNotContain(outbox, row => row.Payload.Contains("99.9") && row.Payload.Contains("Price", StringComparison.OrdinalIgnoreCase));

        var productB = await catalogDirB.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "other",
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
        var variantB = await catalogDirB.CreateVariantAsync(productB.ProductId, "OTHER-M", [(sizeId, "ignored", m)], CancellationToken.None);
        var sellerTenantB = await partyDirB.CreateOrganizationAsync("Tenant B Seller", null, CancellationToken.None);
        var isolated = await offerDirB.CreateOfferAsync(variantB.VariantId, sellerTenantB.PartyId, SalesChannel.Direct, "B-SKU", CancellationToken.None);
        Assert.Null(await offerDirB.FindOfferAsync(offer2.OfferId, CancellationToken.None));
        Assert.Null(await offerDirA.FindOfferAsync(isolated.OfferId, CancellationToken.None));
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
