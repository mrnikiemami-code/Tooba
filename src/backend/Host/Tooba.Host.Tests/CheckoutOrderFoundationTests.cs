using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Cart.Domain;
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
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Events;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Application;
using Tooba.Pricing.Infrastructure;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure;
using Tooba.Tax.Infrastructure.Persistence;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure;
using Tooba.Promotion.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation Checkout/Order: سبد با سفارش یکی نیست، رزرو با خرید آنلاین یکی نیست،
/// خریدار با کاربر عامل فرق دارد، قیمت در تسویه دوباره خوانده می‌شود، و تصویر تاریخی با Pricing جاری عوض نمی‌شود.
/// </summary>
[Collection("PostgresSerial")]
public sealed class CheckoutOrderFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_order_a")
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

    /// <summary>
    /// سبد، سفارش، پرداخت و موجودی هویت جدا دارند؛ خط سفارش از Offer می‌آید نه از Product.
    /// </summary>
    [Fact]
    public void Cart_is_not_order_and_order_is_not_payment_or_inventory()
    {
        Assert.NotEqual(typeof(ShoppingCart), typeof(CheckoutGroup));
        Assert.DoesNotContain("OrderId", typeof(ShoppingCart).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("Paid", typeof(SellerOrder).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("PaymentId", typeof(CheckoutGroup).GetProperties().Select(p => p.Name));
        Assert.Contains("BuyerPartyId", typeof(CheckoutGroup).GetProperties().Select(p => p.Name));
        Assert.Contains("PlacedByUserId", typeof(CheckoutGroup).GetProperties().Select(p => p.Name));
        Assert.Contains("OfferId", typeof(OrderLine).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("ProductId", typeof(OrderLine).GetProperties().Select(p => p.Name));
        Assert.Equal(OrderMode.RequestToReserve, (OrderMode)0);
        Assert.Equal(OrderMode.OnlinePurchase, (OrderMode)1);
    }

    /// <summary>
    /// Domain و Application به MassTransit، Authzed و SDK پرداخت وصل نیستند؛ persistence متعلق به schema order است.
    /// </summary>
    [Fact]
    public void Order_projects_do_not_reference_masstransit_authzed_payment_or_foreign_infrastructure()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application"),
                     Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("MassTransit", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Stripe", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PayPal", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tooba.Cart.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Inventory.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Pricing.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Tax.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Promotion.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity", csproj, StringComparison.Ordinal);
        }

        var application = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Tooba.Order.Application.csproj"));
        Assert.Contains("Tooba.Cart.Application", application);
        Assert.Contains("Tooba.Offer.Application", application);
        Assert.Contains("Tooba.Pricing.Application", application);
        Assert.Contains("Tooba.Inventory.Application", application);
        Assert.Contains("Tooba.Tax.Application", application);
        Assert.Contains("Tooba.Promotion.Application", application);
        Assert.DoesNotContain("Tooba.Tax.Infrastructure", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Tooba.Order.Infrastructure.csproj")), StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Infrastructure", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Tooba.Order.Infrastructure.csproj")), StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Infrastructure", File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Tooba.Order.Infrastructure.csproj")), StringComparison.Ordinal);
        Assert.Equal("order", OrderDbContext.Schema);
        Assert.DoesNotContain("MassTransit", typeof(CheckoutGroup).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("MassTransit", typeof(ICheckoutDirectory).Assembly.GetReferencedAssemblies().Select(a => a.Name));
        Assert.DoesNotContain("Authzed", typeof(ICheckoutDirectory).Assembly.GetReferencedAssemblies().Select(a => a.Name));

        var migration = File.ReadAllText(Directory.GetFiles(
            Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Persistence", "Migrations"),
            "*_InitialOrder.cs").Single());
        Assert.DoesNotContain("References(\"cart", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("schema: \"cart\"", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("schema: \"inventory\"", migration, StringComparison.Ordinal);
    }

    /// <summary>
    /// تسویه روی Postgres: دو حالت سفارش، تمایز خریدار و عامل، PRICE_CHANGED، تصویر ثابت، idempotency، چندفروشنده و ایزولهٔ Tenant.
    /// </summary>
    [SkippableFact]
    public async Task Checkout_revalidates_price_splits_sellers_and_isolates_tenants_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_order_b'";
            if (await cmd.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_order_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_order_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-a", "tenant-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-b", "tenant-b"));

        await using var catalogA = CreateCatalogDb(csA, commerceA);
        await using var partyA = CreatePartyDb(csA, commerceA);
        await using var offerA = CreateOfferDb(csA, commerceA);
        await using var pricingA = CreatePricingDb(csA, commerceA);
        await using var inventoryA = CreateInventoryDb(csA, commerceA);
        await using var cartA = CreateCartDb(csA, commerceA);
        await using var orderA = CreateOrderDb(csA, commerceA);
        await using var taxA = CreateTaxDb(csA, commerceA);
        await using var promotionA = CreatePromotionDb(csA, commerceA);
        await using var catalogB = CreateCatalogDb(csB, commerceB);
        await using var partyB = CreatePartyDb(csB, commerceB);
        await using var offerB = CreateOfferDb(csB, commerceB);
        await using var pricingB = CreatePricingDb(csB, commerceB);
        await using var inventoryB = CreateInventoryDb(csB, commerceB);
        await using var cartB = CreateCartDb(csB, commerceB);
        await using var orderB = CreateOrderDb(csB, commerceB);
        await using var taxB = CreateTaxDb(csB, commerceB);
        await using var promotionB = CreatePromotionDb(csB, commerceB);
        await catalogA.Database.MigrateAsync();
        await partyA.Database.MigrateAsync();
        await offerA.Database.MigrateAsync();
        await pricingA.Database.MigrateAsync();
        await inventoryA.Database.MigrateAsync();
        await cartA.Database.MigrateAsync();
        await orderA.Database.MigrateAsync();
        await taxA.Database.MigrateAsync();
        await promotionA.Database.MigrateAsync();
        await catalogB.Database.MigrateAsync();
        await partyB.Database.MigrateAsync();
        await offerB.Database.MigrateAsync();
        await pricingB.Database.MigrateAsync();
        await inventoryB.Database.MigrateAsync();
        await cartB.Database.MigrateAsync();
        await orderB.Database.MigrateAsync();
        await taxB.Database.MigrateAsync();
        await promotionB.Database.MigrateAsync();

        var catalogDirA = new CatalogDirectory(catalogA, new OpenCatalogUseCaseGuard());
        var partyDirA = new PartyDirectory(partyA);
        var offerDirA = new OfferDirectory(offerA, new OpenOfferUseCaseGuard(), catalogDirA, partyDirA);
        var priceDirA = new PriceDirectory(pricingA, new OpenPricingUseCaseGuard(), offerDirA);
        var inventoryDirA = new InventoryDirectory(inventoryA, new OpenInventoryUseCaseGuard(), offerDirA, catalogDirA);
        var cartDirA = new CartDirectory(cartA, new OpenCartUseCaseGuard(), offerDirA, priceDirA, inventoryDirA, inventoryDirA);
        var taxDirA = new TaxDirectory(taxA, new OpenTaxUseCaseGuard());
        var promoDirA = new PromotionDirectory(promotionA, new OpenPromotionUseCaseGuard(), new DeferredPromotionRedemptionLedger());
        var checkoutA = new CheckoutDirectory(orderA, new OpenOrderUseCaseGuard(), cartDirA, cartDirA, offerDirA, priceDirA, inventoryDirA, taxDirA, promoDirA);

        var names = new Dictionary<string, string> { ["fa-IR"] = "پیراهن سفارش", ["en-US"] = "Order shirt" };
        var product = await catalogDirA.CreateProductAsync(CatalogProductKind.PhysicalGood, "shirt-order", null, names, CancellationToken.None);
        var colorId = await catalogDirA.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await catalogDirA.AddAttributeOptionAsync(colorId, "black", new Dictionary<string, string> { ["fa-IR"] = "سیاه" }, CancellationToken.None);
        var variant = await catalogDirA.CreateVariantAsync(product.ProductId, "SHIRT-ORDER", [(colorId, "ignored", black)], CancellationToken.None);
        var sellerA = await partyDirA.CreateOrganizationAsync("فروشنده سفارش الف", null, CancellationToken.None);
        var sellerB = await partyDirA.CreateOrganizationAsync("فروشنده سفارش ب", null, CancellationToken.None);
        var buyer = await partyDirA.CreatePersonAsync("خریدار اقتصادی", CancellationToken.None);
        var actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var stranger = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var offer1 = await offerDirA.CreateOfferAsync(variant.VariantId, sellerA.PartyId, SalesChannel.Marketplace, "ORD-A", CancellationToken.None);
        var offer2 = await offerDirA.CreateOfferAsync(variant.VariantId, sellerB.PartyId, SalesChannel.Marketplace, "ORD-B", CancellationToken.None);
        await offerDirA.ActivateAsync(offer1.OfferId, CancellationToken.None);
        await offerDirA.ActivateAsync(offer2.OfferId, CancellationToken.None);
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var price1 = await priceDirA.CreatePriceAsync(offer1.OfferId, "IR", SalesChannel.Marketplace, 100000, "IRR", start, null, CancellationToken.None);
        var price2 = await priceDirA.CreatePriceAsync(offer2.OfferId, "IR", SalesChannel.Marketplace, 90000, "IRR", start, null, CancellationToken.None);
        await priceDirA.ActivateAsync(price1.PriceId, CancellationToken.None);
        await priceDirA.ActivateAsync(price2.PriceId, CancellationToken.None);
        var standard = await taxDirA.CreateCategoryAsync("standard", "استاندارد", CancellationToken.None);
        var taxRule = await taxDirA.CreateRuleAsync(
            "IR-NAT",
            "IR",
            standard.CategoryId,
            TaxRuleKind.Percentage,
            0.09m,
            start,
            null,
            10,
            TaxOverridePolicy.Disabled,
            CancellationToken.None);
        await taxDirA.ActivateRuleAsync(taxRule.RuleId, CancellationToken.None);
        await taxDirA.AssignOfferCategoryAsync(offer1.OfferId, standard.CategoryId, CancellationToken.None);
        await taxDirA.AssignOfferCategoryAsync(offer2.OfferId, standard.CategoryId, CancellationToken.None);
        var loc = await inventoryDirA.CreateLocationAsync("WH-O", "انبار سفارش", CancellationToken.None);
        var stock1 = await inventoryDirA.OpenPositionAsync(offer1.OfferId, loc, CancellationToken.None);
        var stock2 = await inventoryDirA.OpenPositionAsync(offer2.OfferId, loc, CancellationToken.None);
        await inventoryDirA.AdjustAsync(stock1, StockAdjustmentKind.Increase, 10, "رسید سفارش", null, CancellationToken.None);
        await inventoryDirA.AdjustAsync(stock2, StockAdjustmentKind.Increase, 10, "رسید فروشنده دوم", null, CancellationToken.None);

        var access = new CartAccess(actor, null);
        var orderAccess = new OrderAccess(buyer.PartyId, actor);
        var onlineCart = await cartDirA.CreateAuthenticatedAsync(actor, "IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var withFirst = await cartDirA.AddOrIncreaseLineAsync(onlineCart.CartId, access, onlineCart.Version, offer1.OfferId, 1, CancellationToken.None);
        var multi = await cartDirA.AddOrIncreaseLineAsync(onlineCart.CartId, access, withFirst.Version, offer2.OfferId, 1, CancellationToken.None);
        var availableBefore = (await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None))!.Available;

        var command = new SubmitCheckoutCommand(
            multi.CartId,
            access,
            multi.Version,
            OrderMode.OnlinePurchase,
            buyer.PartyId,
            actor,
            "idem-online-1",
            "IR-NAT");
        var submitted = await checkoutA.SubmitAsync(command, CancellationToken.None);
        Assert.Equal(OrderMode.OnlinePurchase, submitted.Mode);
        Assert.Equal(buyer.PartyId, submitted.BuyerPartyId);
        Assert.Equal(actor, submitted.PlacedByUserId);
        Assert.NotEqual(submitted.BuyerPartyId, submitted.PlacedByUserId);
        Assert.Equal(2, submitted.SellerOrders.Count);
        Assert.Contains(submitted.SellerOrders, x => x.SellerPartyId == sellerA.PartyId);
        Assert.Contains(submitted.SellerOrders, x => x.SellerPartyId == sellerB.PartyId);
        Assert.All(submitted.SellerOrders, x => Assert.Equal(SellerOrderStatus.PendingPayment, x.Status));
        Assert.DoesNotContain("Paid", submitted.SellerOrders.Select(x => x.Status.ToString()));
        Assert.Equal(2, submitted.SellerOrders.Select(x => x.SellerPartyId).Distinct().Count());
        Assert.Equal(9000m, submitted.SellerOrders.Single(x => x.SellerPartyId == sellerA.PartyId).TaxSnapshot);
        Assert.Equal(8100m, submitted.SellerOrders.Single(x => x.SellerPartyId == sellerB.PartyId).TaxSnapshot);
        Assert.Equal(109000m, submitted.SellerOrders.Single(x => x.SellerPartyId == sellerA.PartyId).GrandTotalSnapshot);
        Assert.All(submitted.SellerOrders.SelectMany(x => x.Lines), line =>
        {
            Assert.True(line.ReservationId.HasValue);
            Assert.True(line.UnitPriceSnapshot > 0);
            Assert.Equal(line.UnitPriceSnapshot * line.Quantity, line.LineTotalSnapshot);
        });
        Assert.Equal(offer1.OfferId, submitted.SellerOrders.Single(x => x.SellerPartyId == sellerA.PartyId).Lines.Single().OfferId);
        var convertedCart = await cartDirA.GetCartAsync(multi.CartId, access, CancellationToken.None);
        Assert.Equal(CartStatus.Converted, convertedCart!.Status);
        Assert.Equal(CartConversionIntent.OnlinePurchase, convertedCart.ConversionIntent);
        Assert.Equal(availableBefore, (await inventoryDirA.GetAvailabilityAsync(offer1.OfferId, CancellationToken.None))!.Available);

        var replay = await checkoutA.SubmitAsync(command, CancellationToken.None);
        Assert.Equal(submitted.CheckoutId, replay.CheckoutId);
        Assert.Equal(2, await orderA.SellerOrders.CountAsync());
        Assert.Equal(2, await orderA.Lines.CountAsync());

        var staleCart = await cartDirA.CreateAuthenticatedAsync(actor, "IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var staleLined = await cartDirA.AddOrIncreaseLineAsync(staleCart.CartId, access, staleCart.Version, offer1.OfferId, 1, CancellationToken.None);

        await priceDirA.ChangeAmountAsync(price1.PriceId, 250000, "IRR", CancellationToken.None);
        var afterPriceChange = await checkoutA.GetCheckoutAsync(submitted.CheckoutId, orderAccess, CancellationToken.None);
        var sellerALine = afterPriceChange!.SellerOrders.Single(x => x.SellerPartyId == sellerA.PartyId).Lines.Single();
        Assert.Equal(100000m, sellerALine.UnitPriceSnapshot);
        Assert.Equal(100000m, sellerALine.LineTotalSnapshot);
        Assert.Equal(9000m, afterPriceChange.SellerOrders.Single(x => x.SellerPartyId == sellerA.PartyId).TaxSnapshot);
        await taxDirA.ChangeRuleRateAsync(taxRule.RuleId, 0.20m, CancellationToken.None);
        var afterTaxRuleChange = await checkoutA.GetCheckoutAsync(submitted.CheckoutId, orderAccess, CancellationToken.None);
        Assert.Equal(9000m, afterTaxRuleChange!.SellerOrders.Single(x => x.SellerPartyId == sellerA.PartyId).TaxSnapshot);

        Assert.Null(await checkoutA.GetCheckoutAsync(submitted.CheckoutId, new OrderAccess(null, stranger), CancellationToken.None));
        var number = submitted.SellerOrders[0].OrderNumber;
        Assert.Null(await checkoutA.GetSellerOrderByNumberAsync(number, new OrderAccess(null, stranger), CancellationToken.None));
        Assert.NotNull(await checkoutA.GetSellerOrderByNumberAsync(number, orderAccess, CancellationToken.None));

        var priceChanged = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            checkoutA.SubmitAsync(
                new SubmitCheckoutCommand(
                    staleLined.CartId,
                    access,
                    staleLined.Version,
                    OrderMode.OnlinePurchase,
                    buyer.PartyId,
                    actor,
                    "idem-price-changed",
                    "IR-NAT"),
                CancellationToken.None));
        Assert.Equal("PRICE_CHANGED", priceChanged.Message);

        var requestCart = await cartDirA.CreateAuthenticatedAsync(actor, "IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var requestLined = await cartDirA.AddOrIncreaseLineAsync(requestCart.CartId, access, requestCart.Version, offer2.OfferId, 1, CancellationToken.None);
        var reserved = await checkoutA.SubmitAsync(
            new SubmitCheckoutCommand(
                requestLined.CartId,
                access,
                requestLined.Version,
                OrderMode.RequestToReserve,
                buyer.PartyId,
                actor,
                "idem-reserve-1",
                "IR-NAT"),
            CancellationToken.None);
        Assert.Equal(OrderMode.RequestToReserve, reserved.Mode);
        Assert.All(reserved.SellerOrders, x => Assert.Equal(SellerOrderStatus.ReservationRequested, x.Status));
        var requestCartAfter = await cartDirA.GetCartAsync(requestLined.CartId, access, CancellationToken.None);
        Assert.Equal(CartConversionIntent.RequestToReserve, requestCartAfter!.ConversionIntent);

        var cancelTarget = submitted.SellerOrders.Single(x => x.SellerPartyId == sellerA.PartyId);
        await checkoutA.CancelSellerOrderAsync(cancelTarget.SellerOrderId, orderAccess, CancellationToken.None);
        var cancelled = await checkoutA.GetSellerOrderByNumberAsync(cancelTarget.OrderNumber, orderAccess, CancellationToken.None);
        Assert.Equal(SellerOrderStatus.Cancelled, cancelled!.Status);

        var outbox = await orderA.OutboxMessages.AsNoTracking().ToListAsync();
        Assert.Contains(outbox, row => row.EventType == CheckoutSubmittedIntegrationEvent.EventTypeName);
        Assert.Contains(outbox, row => row.EventType == SellerOrderCreatedIntegrationEvent.EventTypeName);

        var catalogDirB = new CatalogDirectory(catalogB, new OpenCatalogUseCaseGuard());
        var partyDirB = new PartyDirectory(partyB);
        var offerDirB = new OfferDirectory(offerB, new OpenOfferUseCaseGuard(), catalogDirB, partyDirB);
        var priceDirB = new PriceDirectory(pricingB, new OpenPricingUseCaseGuard(), offerDirB);
        var inventoryDirB = new InventoryDirectory(inventoryB, new OpenInventoryUseCaseGuard(), offerDirB, catalogDirB);
        var cartDirB = new CartDirectory(cartB, new OpenCartUseCaseGuard(), offerDirB, priceDirB, inventoryDirB, inventoryDirB);
        var taxDirB = new TaxDirectory(taxB, new OpenTaxUseCaseGuard());
        var promoDirB = new PromotionDirectory(promotionB, new OpenPromotionUseCaseGuard(), new DeferredPromotionRedemptionLedger());
        var checkoutB = new CheckoutDirectory(orderB, new OpenOrderUseCaseGuard(), cartDirB, cartDirB, offerDirB, priceDirB, inventoryDirB, taxDirB, promoDirB);
        Assert.Null(await checkoutB.GetCheckoutAsync(submitted.CheckoutId, orderAccess, CancellationToken.None));
        Assert.Null(await checkoutA.GetCheckoutAsync(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), orderAccess, CancellationToken.None));

        var repairCart = await cartDirA.CreateAuthenticatedAsync(actor, "IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var repairLined = await cartDirA.AddOrIncreaseLineAsync(repairCart.CartId, access, repairCart.Version, offer2.OfferId, 1, CancellationToken.None);
        var failOnce = new FailOnceCartDirectory(cartDirA);
        var checkoutFail = new CheckoutDirectory(orderA, new OpenOrderUseCaseGuard(), cartDirA, failOnce, offerDirA, priceDirA, inventoryDirA, taxDirA, promoDirA);
        var repairCommand = new SubmitCheckoutCommand(
            repairLined.CartId,
            access,
            repairLined.Version,
            OrderMode.OnlinePurchase,
            buyer.PartyId,
            actor,
            "idem-repair-fail",
            "IR-NAT");
        var persistedWithoutConvert = await checkoutFail.SubmitAsync(repairCommand, CancellationToken.None);
        Assert.Equal(CartStatus.Active, (await cartDirA.GetCartAsync(repairLined.CartId, access, CancellationToken.None))!.Status);
        var reconciled = await checkoutA.SubmitAsync(repairCommand, CancellationToken.None);
        Assert.Equal(persistedWithoutConvert.CheckoutId, reconciled.CheckoutId);
        Assert.Equal(CartStatus.Converted, (await cartDirA.GetCartAsync(repairLined.CartId, access, CancellationToken.None))!.Status);
        Assert.Equal(90000m, persistedWithoutConvert.SellerOrders.SelectMany(x => x.Lines).First().UnitPriceSnapshot);

        var differentKey = repairCommand with { IdempotencyKey = "idem-repair-other-key" };
        var reused = await checkoutA.SubmitAsync(differentKey, CancellationToken.None);
        Assert.Equal(persistedWithoutConvert.CheckoutId, reused.CheckoutId);
        Assert.Equal(1, await orderA.Checkouts.CountAsync(x => x.CartId == repairLined.CartId));

        var concCart = await cartDirA.CreateAuthenticatedAsync(actor, "IR", "IRR", SalesChannel.Marketplace, CancellationToken.None);
        var concLined = await cartDirA.AddOrIncreaseLineAsync(concCart.CartId, access, concCart.Version, offer2.OfferId, 1, CancellationToken.None);
        await using var orderA2 = CreateOrderDb(csA, commerceA);
        await using var cartA2 = CreateCartDb(csA, commerceA);
        await using var taxA2 = CreateTaxDb(csA, commerceA);
        await using var promotionA2 = CreatePromotionDb(csA, commerceA);
        var cartDirA2 = new CartDirectory(cartA2, new OpenCartUseCaseGuard(), offerDirA, priceDirA, inventoryDirA, inventoryDirA);
        var taxDirA2 = new TaxDirectory(taxA2, new OpenTaxUseCaseGuard());
        var promoDirA2 = new PromotionDirectory(promotionA2, new OpenPromotionUseCaseGuard(), new DeferredPromotionRedemptionLedger());
        var checkoutA2 = new CheckoutDirectory(orderA2, new OpenOrderUseCaseGuard(), cartDirA2, cartDirA2, offerDirA, priceDirA, inventoryDirA, taxDirA2, promoDirA2);
        var concLeft = checkoutA.SubmitAsync(
            new SubmitCheckoutCommand(concLined.CartId, access, concLined.Version, OrderMode.OnlinePurchase, buyer.PartyId, actor, "idem-conc-a", "IR-NAT"),
            CancellationToken.None);
        var concRight = checkoutA2.SubmitAsync(
            new SubmitCheckoutCommand(concLined.CartId, access, concLined.Version, OrderMode.OnlinePurchase, buyer.PartyId, actor, "idem-conc-b", "IR-NAT"),
            CancellationToken.None);
        var concResults = await Task.WhenAll(concLeft, concRight);
        Assert.Equal(concResults[0].CheckoutId, concResults[1].CheckoutId);
        Assert.Equal(1, await orderA.Checkouts.CountAsync(x => x.CartId == concLined.CartId));
        Assert.Equal(CartStatus.Converted, (await cartDirA.GetCartAsync(concLined.CartId, access, CancellationToken.None))!.Status);
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

    private static OrderDbContext CreateOrderDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new OrderOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        options.AddInterceptors(interceptor);
        return new OrderDbContext(options.Options);
    }

    private static TaxDbContext CreateTaxDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new TaxOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<TaxDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, TaxDbContext.Schema, typeof(TaxDbContext));
        options.AddInterceptors(interceptor);
        return new TaxDbContext(options.Options);
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

    /// <summary>
    /// یک‌بار تبدیل سبد را شکست می‌دهد تا پنجرهٔ Order ذخیره‌شده و Cart هنوز Active پوشش داده شود.
    /// </summary>
    private sealed class FailOnceCartDirectory : ICartDirectory
    {
        private readonly ICartDirectory _inner;
        private int _converts;

        public FailOnceCartDirectory(ICartDirectory inner) => _inner = inner;

        public Task<CartSnapshot> CreateAuthenticatedAsync(Guid userId, string market, string currency, SalesChannel channel, CancellationToken cancellationToken) =>
            _inner.CreateAuthenticatedAsync(userId, market, currency, channel, cancellationToken);

        public Task<GuestCartCreated> CreateGuestAsync(string market, string currency, SalesChannel channel, CancellationToken cancellationToken) =>
            _inner.CreateGuestAsync(market, currency, channel, cancellationToken);

        public Task<CartSnapshot> AddOrIncreaseLineAsync(Guid cartId, CartAccess access, int expectedVersion, Guid offerId, int quantity, CancellationToken cancellationToken) =>
            _inner.AddOrIncreaseLineAsync(cartId, access, expectedVersion, offerId, quantity, cancellationToken);

        public Task<CartSnapshot> ChangeLineQuantityAsync(Guid cartId, CartAccess access, int expectedVersion, Guid lineId, int quantity, CancellationToken cancellationToken) =>
            _inner.ChangeLineQuantityAsync(cartId, access, expectedVersion, lineId, quantity, cancellationToken);

        public Task<CartSnapshot> RemoveLineAsync(Guid cartId, CartAccess access, int expectedVersion, Guid lineId, CancellationToken cancellationToken) =>
            _inner.RemoveLineAsync(cartId, access, expectedVersion, lineId, cancellationToken);

        public Task AbandonAsync(Guid cartId, CartAccess access, int expectedVersion, CancellationToken cancellationToken) =>
            _inner.AbandonAsync(cartId, access, expectedVersion, cancellationToken);

        public Task ExpireDueCartsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken) =>
            _inner.ExpireDueCartsAsync(utcNow, cancellationToken);

        public Task<CartSnapshot> ConvertAsync(Guid cartId, CartAccess access, int expectedVersion, CartConversionIntent intent, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _converts) == 1)
            {
                throw new InvalidOperationException("تبدیل سبد عمداً برای آزمون آشتی شکست خورد.");
            }

            return _inner.ConvertAsync(cartId, access, expectedVersion, intent, cancellationToken);
        }
    }
}
