using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Cart.Infrastructure;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Application;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Infrastructure;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure;
using Tooba.Promotion.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T022-R19: زمینهٔ کمپین روی CartLine + قیمت canonical کمپین بدون اعتماد به مبلغ کلاینت.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CampaignCartPriceIntegrityTests : IAsyncLifetime
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
                .WithDatabase("tooba_cart_campaign")
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
    public void Cart_line_carries_merchandising_campaign_context_not_client_price()
    {
        Assert.Contains(nameof(Tooba.Cart.Domain.CartLine.MerchandisingCampaignId), typeof(Tooba.Cart.Domain.CartLine).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("DiscountPercent", typeof(Tooba.Cart.Domain.CartLine).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("PromoAmount", typeof(Tooba.Cart.Domain.CartLine).GetProperties().Select(p => p.Name));
        Assert.Contains("ICampaignCartPriceAuthority", File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Pricing", "Tooba.Pricing.Contracts", "CampaignCartPriceAuthority.cs")));
        Assert.Contains("Tooba.Pricing.Contracts", File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Cart", "Tooba.Cart.Application", "Tooba.Cart.Application.csproj")));
        Assert.DoesNotContain("Tooba.Pricing.Application", File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Cart", "Tooba.Cart.Application", "Tooba.Cart.Application.csproj")));
    }

    [SkippableFact]
    public async Task Campaign_context_add_wrong_id_expiry_and_reload_reprice_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "store-alpha"));

        await using var catalogDb = CreateCatalogDb(cs, commerce);
        await using var partyDb = CreatePartyDb(cs, commerce);
        await using var offerDb = CreateOfferDb(cs, commerce);
        await using var pricingDb = CreatePricingDb(cs, commerce);
        await using var inventoryDb = CreateInventoryDb(cs, commerce);
        await using var promoDb = CreatePromotionDb(cs, commerce);
        await using var cartDb = CreateCartDb(cs, commerce);
        await catalogDb.Database.MigrateAsync();
        await partyDb.Database.MigrateAsync();
        await offerDb.Database.MigrateAsync();
        await pricingDb.Database.MigrateAsync();
        await inventoryDb.Database.MigrateAsync();
        await promoDb.Database.MigrateAsync();
        await cartDb.Database.MigrateAsync();

        var catalogDir = new CatalogDirectory(catalogDb, new OpenCatalogUseCaseGuard());
        var partyDir = new PartyDirectory(partyDb);
        var offerDir = new OfferDirectory(offerDb, new OpenOfferUseCaseGuard(), catalogDir, partyDir);
        var priceDir = new PriceDirectory(pricingDb, new OpenPricingUseCaseGuard(), offerDir);
        var inventoryDir = new InventoryDirectory(inventoryDb, new OpenInventoryUseCaseGuard(), offerDir, catalogDir);
        var merchDir = new MerchandisingCampaignDirectory(promoDb);
        var campaignPrices = new CampaignCartPriceAuthority(promoDb, priceDir, commerce);
        var cartDir = new CartDirectory(
            cartDb,
            new OpenCartUseCaseGuard(),
            offerDir,
            priceDir,
            inventoryDir,
            inventoryDir,
            campaignPrices: campaignPrices);

        var storeId = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");
        var now = DateTimeOffset.UtcNow;
        var type = await merchDir.EnsureAmazingTypeSeededAsync(CancellationToken.None);

        var names = new Dictionary<string, string> { ["fa-IR"] = "کالای کمپین سبد" };
        var product = await catalogDir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "cart-camp-item",
            null,
            names,
            CancellationToken.None);
        var colorId = await catalogDir.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await catalogDir.AddAttributeOptionAsync(
            colorId,
            "black",
            new Dictionary<string, string> { ["fa-IR"] = "سیاه" },
            CancellationToken.None);
        var variant = await catalogDir.CreateVariantAsync(
            product.ProductId,
            "CCP-1",
            [(colorId, "ignored", black)],
            CancellationToken.None);
        var seller = await partyDir.CreateOrganizationAsync("فروشنده کمپین سبد", null, CancellationToken.None);
        var offer = await offerDir.CreateOfferAsync(
            variant.VariantId,
            seller.PartyId,
            SalesChannel.Marketplace,
            "CCP-OFFER",
            CancellationToken.None);
        await offerDir.ActivateAsync(offer.OfferId, CancellationToken.None);
        var basePrice = await priceDir.CreatePriceAsync(
            offer.OfferId,
            "IR",
            SalesChannel.Marketplace,
            100000m,
            "IRR",
            now.AddDays(-1),
            null,
            CancellationToken.None);
        await priceDir.ActivateAsync(basePrice.PriceId, CancellationToken.None);

        var campaign = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a19a0-0001-7000-8000-000000000019"),
            type.Id,
            storeId,
            now.AddHours(-1),
            now.AddDays(1),
            priority: 50,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        await merchDir.UpsertCampaignTranslationAsync(
            campaign.Id,
            "fa-IR",
            "کمپین سبد",
            null,
            "Amazing",
            CancellationToken.None);
        await merchDir.SyncSeedMembersAsync(campaign.Id, storeId, [offer.OfferId], CancellationToken.None);
        var campaignPrice = await priceDir.CreateCampaignPriceAsync(
            offer.OfferId,
            campaign.Id,
            "IR",
            SalesChannel.Marketplace,
            70000m,
            "IRR",
            now.AddHours(-1),
            now.AddDays(1),
            CancellationToken.None);
        await priceDir.ActivateAsync(campaignPrice.PriceId, CancellationToken.None);

        var loc = await inventoryDir.CreateLocationAsync("WH-CCP", "انبار کمپین سبد", CancellationToken.None);
        var stock = await inventoryDir.OpenPositionAsync(offer.OfferId, loc, CancellationToken.None);
        await inventoryDir.AdjustAsync(stock, StockAdjustmentKind.Increase, 20, "seed", null, CancellationToken.None);

        var guest = await cartDir.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var access = new CartAccess(null, guest.GuestSecret);

        // Normal ATC — base price, no campaign context
        var baseLine = await cartDir.AddOrIncreaseLineAsync(
            guest.Cart.CartId,
            access,
            guest.Cart.Version,
            offer.OfferId,
            1,
            CancellationToken.None);
        Assert.Null(baseLine.Lines[0].MerchandisingCampaignId);
        Assert.Equal(100000m, baseLine.Lines[0].QuotedAmount);

        await cartDir.RemoveLineAsync(
            guest.Cart.CartId,
            access,
            baseLine.Version,
            baseLine.Lines[0].LineId,
            CancellationToken.None);
        var empty = await cartDir.GetCartAsync(guest.Cart.CartId, access, CancellationToken.None);
        Assert.NotNull(empty);

        // Valid campaign context → campaign price
        var promoLine = await cartDir.AddOrIncreaseLineAsync(
            empty!.CartId,
            access,
            empty.Version,
            offer.OfferId,
            1,
            CancellationToken.None,
            campaign.Id);
        Assert.Equal(campaign.Id, promoLine.Lines[0].MerchandisingCampaignId);
        Assert.Equal(70000m, promoLine.Lines[0].QuotedAmount);
        Assert.Equal(campaignPrice.PriceId, promoLine.Lines[0].PriceId);

        // Quantity change preserves campaign context
        var qty = await cartDir.ChangeLineQuantityAsync(
            promoLine.CartId,
            access,
            promoLine.Version,
            promoLine.Lines[0].LineId,
            2,
            CancellationToken.None);
        Assert.Equal(2, qty.Lines[0].Quantity);
        Assert.Equal(campaign.Id, qty.Lines[0].MerchandisingCampaignId);
        Assert.Equal(70000m, qty.Lines[0].QuotedAmount);

        // Wrong campaign id → base price, no campaign context retained
        var otherCampaign = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a19a0-0002-7000-8000-000000000019"),
            type.Id,
            storeId,
            now.AddHours(-1),
            now.AddDays(1),
            priority: 1,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        await merchDir.SyncSeedMembersAsync(otherCampaign.Id, storeId, [], CancellationToken.None);
        await cartDir.RemoveLineAsync(qty.CartId, access, qty.Version, qty.Lines[0].LineId, CancellationToken.None);
        var afterRemove = await cartDir.GetCartAsync(qty.CartId, access, CancellationToken.None);
        var wrong = await cartDir.AddOrIncreaseLineAsync(
            afterRemove!.CartId,
            access,
            afterRemove.Version,
            offer.OfferId,
            1,
            CancellationToken.None,
            otherCampaign.Id);
        Assert.Null(wrong.Lines[0].MerchandisingCampaignId);
        Assert.Equal(100000m, wrong.Lines[0].QuotedAmount);

        // Reload revalidates: expire campaign window → base reprice
        await cartDir.RemoveLineAsync(wrong.CartId, access, wrong.Version, wrong.Lines[0].LineId, CancellationToken.None);
        var cleared = await cartDir.GetCartAsync(wrong.CartId, access, CancellationToken.None);
        var activePromo = await cartDir.AddOrIncreaseLineAsync(
            cleared!.CartId,
            access,
            cleared.Version,
            offer.OfferId,
            1,
            CancellationToken.None,
            campaign.Id);
        Assert.Equal(70000m, activePromo.Lines[0].QuotedAmount);

        var tracked = await promoDb.MerchandisingCampaigns.SingleAsync(x => x.Id == campaign.Id);
        tracked.UpdateWindow(now.AddHours(-2), now.AddMinutes(-5), priority: 50, now);
        await promoDb.SaveChangesAsync();

        var reloaded = await cartDir.GetCartAsync(activePromo.CartId, access, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Null(reloaded!.Lines[0].MerchandisingCampaignId);
        Assert.Equal(100000m, reloaded.Lines[0].QuotedAmount);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tooba.sln")) || File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
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

    private static InventoryDbContext CreateInventoryDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new InventoryOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, InventoryDbContext.Schema, typeof(InventoryDbContext));
        options.AddInterceptors(interceptor);
        return new InventoryDbContext(options.Options);
    }

    private static PromotionDbContext CreatePromotionDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PromotionOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PromotionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PromotionDbContext.Schema, typeof(PromotionDbContext));
        options.AddInterceptors(interceptor);
        return new PromotionDbContext(options.Options);
    }

    private static CartDbContext CreateCartDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CartOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CartDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CartDbContext.Schema, typeof(CartDbContext));
        options.AddInterceptors(interceptor);
        return new CartDbContext(options.Options);
    }
}
