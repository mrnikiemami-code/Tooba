using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Application.ProductIdentity.Commands;
using Tooba.Catalog.Application.ProductIdentity.Models;
using Tooba.Catalog.Application.ProductIdentity.Validators;
using Tooba.Catalog.Domain.Aggregates;
using Tooba.Catalog.Domain.Enums;
using Tooba.Catalog.Domain.Rules;
using CatalogProductKind = Tooba.Catalog.Domain.Enums.CatalogProductKind;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Infrastructure.Integrations.Shipping;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

/// <summary>
/// TB-249-R5 cases f–g: Catalog physical-spec create/update→PG→query + validator;
/// legacy/override policy (domain) and Catalog change does not rewrite accepted Order.
/// </summary>
// Share the package-execution collection so TB249R6 Catalog + package cases never wipe the same DB in parallel.
[Collection(ShippingCarrierPackageExecutionRelationalCollection.Name)]
public sealed class CatalogPhysicalSpecificationRelationalTests
{
    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveVerified;

    [Fact]
    [Trait("Category", "TB249R5")]
    [Trait("Category", "TB249R6")]
    [Trait("Case", "f")]
    public async Task CaseF_catalog_create_update_pg_query_exact_g_cm_and_validator()
    {
        var target = ShippingCarrierDispatchCompletionRelationalTestTarget.FromEnvironment();
        await EnsureMigratedAsync(target);

        await using var db = CreateCatalogDb(target.ConnectionString);
        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='catalog' AND table_name='products') THEN
                DELETE FROM catalog.variants;
                DELETE FROM catalog.products;
              END IF;
            END $$;
            """);

        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "tb249r5-phys", DateTimeOffset.UtcNow);
        // Weight writes require whole grams; centimetre decimals remain lawful.
        product.SetPhysicalSpecification(1500m, 20.5m, 15m, 10.25m, DateTimeOffset.UtcNow);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var reloaded = await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(1500m, reloaded.WeightGrams);
        Assert.Equal(20.5m, reloaded.LengthCentimeters);
        Assert.Equal(15m, reloaded.WidthCentimeters);
        Assert.Equal(10.25m, reloaded.HeightCentimeters);

        reloaded = await db.Products.SingleAsync(x => x.ProductId == product.ProductId);
        reloaded.SetPhysicalSpecification(900m, 30m, 20m, 12m, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var updated = await db.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(900m, updated.WeightGrams);
        Assert.Equal(30m, updated.LengthCentimeters);

        var validator = new UpdateProductPhysicalSpecificationCommandValidator();
        var ok = await validator.ValidateAsync(new UpdateProductPhysicalSpecificationCommand(
            product.ProductId,
            new WorkspaceProductPhysicalSpecificationWriteModel(500m, 10m, 10m, 10m, updated.UpdatedAt)));
        Assert.True(ok.IsValid, string.Join(",", ok.Errors.Select(e => e.ErrorMessage)));

        var bad = await validator.ValidateAsync(new UpdateProductPhysicalSpecificationCommand(
            product.ProductId,
            new WorkspaceProductPhysicalSpecificationWriteModel(-1m, 10m, 10m, 10m, updated.UpdatedAt)));
        Assert.False(bad.IsValid);
        Assert.Contains(bad.Errors, e => e.PropertyName.Contains("WeightGrams", StringComparison.Ordinal));

        var fractional = await validator.ValidateAsync(new UpdateProductPhysicalSpecificationCommand(
            product.ProductId,
            new WorkspaceProductPhysicalSpecificationWriteModel(500.5m, 10m, 10m, 10m, updated.UpdatedAt)));
        Assert.False(fractional.IsValid);
        Assert.Contains(fractional.Errors, e => e.PropertyName.Contains("WeightGrams", StringComparison.Ordinal));

        var overflow = await validator.ValidateAsync(new UpdateProductPhysicalSpecificationCommand(
            product.ProductId,
            new WorkspaceProductPhysicalSpecificationWriteModel(
                CatalogPhysicalSpecification.WeightGramsMaxValue + 1m, null, null, null, updated.UpdatedAt)));
        Assert.False(overflow.IsValid);

        var partialOk = await validator.ValidateAsync(new UpdateProductPhysicalSpecificationCommand(
            product.ProductId,
            new WorkspaceProductPhysicalSpecificationWriteModel(null, null, null, null, updated.UpdatedAt)));
        Assert.True(partialOk.IsValid);
    }

    [Fact]
    [Trait("Category", "TB249R5")]
    [Trait("Category", "TB249R6")]
    [Trait("Case", "g")]
    [Trait("Case", "weight-regression")]
    public async Task CaseG_legacy_override_policy_and_catalog_change_does_not_rewrite_order()
    {
        // Domain legacy/override policy (authoritative unit path already covered in Order.Tests).
        var conflict = CatalogPhysicalSpecification.ResolveEffectiveWeightGrams(1500m, 1400L);
        Assert.Null(conflict);
        var equal = CatalogPhysicalSpecification.ResolveEffectiveWeightGrams(500m, 500L);
        Assert.Equal(500L, equal);
        var legacyOnly = CatalogPhysicalSpecification.ResolveEffectiveWeightGrams(null, 800L);
        Assert.Equal(800L, legacyOnly);
        // Defect B: typed 500.5 + legacy 500 must NEVER resolve to 500.
        Assert.Null(CatalogPhysicalSpecification.ResolveEffectiveWeightGrams(500.5m, 500L));
        Assert.Null(CatalogPhysicalSpecification.ResolveEffectiveFacts(
            500.5m, null, null, null, null, null, null, null, 500L).WeightGrams);
        // Variant null inherits product fractional → still fail-closed (no legacy heal).
        Assert.Null(CatalogPhysicalSpecification.ResolveEffectiveFacts(
            500.5m, null, null, null,
            variantWeightGrams: null,
            variantLengthCentimeters: null,
            variantWidthCentimeters: null,
            variantHeightCentimeters: null,
            legacyAttributeGrams: 500L).WeightGrams);
        // Explicit invalid variant never heals from parent/legacy.
        Assert.Null(CatalogPhysicalSpecification.ResolveEffectiveFacts(
            1000m, null, null, null,
            variantWeightGrams: 500.5m,
            variantLengthCentimeters: null,
            variantWidthCentimeters: null,
            variantHeightCentimeters: null,
            legacyAttributeGrams: 1000L).WeightGrams);
        var overrideFacts = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            1000m, 30m, 20m, 10m,
            variantWeightGrams: 1200m,
            variantLengthCentimeters: 35m,
            variantWidthCentimeters: null,
            variantHeightCentimeters: null,
            legacyAttributeGrams: null);
        Assert.Equal(1200L, overrideFacts.WeightGrams);
        Assert.Equal(35m, overrideFacts.LengthCentimeters);
        Assert.Equal(20m, overrideFacts.WidthCentimeters);

        var target = ShippingCarrierDispatchCompletionRelationalTestTarget.FromEnvironment();
        await EnsureMigratedAsync(target);

        await using var orderDb = CreateOrderDb(target.ConnectionString);
        await using var catalogDb = CreateCatalogDb(target.ConnectionString);

        await orderDb.Database.ExecuteSqlRawAsync(
            """
            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='order' AND table_name='order_shipping_groups') THEN
                DELETE FROM "order".order_shipping_group_lines;
                DELETE FROM "order".order_shipping_groups;
                DELETE FROM "order".order_lines;
                DELETE FROM "order".seller_orders;
                DELETE FROM "order".checkouts;
              END IF;
            END $$;
            """);
        await catalogDb.Database.ExecuteSqlRawAsync(
            """
            DO $$ BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='catalog' AND table_name='products') THEN
                DELETE FROM catalog.variants;
                DELETE FROM catalog.products;
              END IF;
            END $$;
            """);

        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "tb249r5-order-immut", DateTimeOffset.UtcNow);
        product.SetPhysicalSpecification(1500m, 20m, 15m, 10m, DateTimeOffset.UtcNow);
        catalogDb.Products.Add(product);
        await catalogDb.SaveChangesAsync();

        var checkoutId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.Parse("44444444-4444-4444-8444-444444444444");
        var actor = Guid.Parse("88888888-8888-4888-8888-888888888888");
        var buyer = Guid.Parse("99999999-9999-4999-8999-999999999999");
        var now = DateTimeOffset.Parse("2026-11-03T00:00:00Z");
        var accountId = Guid.Parse("33333333-3333-4333-8333-333333333333");
        var cartLineId = Guid.NewGuid();

        var line = OrderLine.FromCheckout(
            sellerOrderId, Guid.NewGuid(), Guid.NewGuid(), sellerPartyId,
            1m, 100_000m, "IRR", true, Guid.NewGuid(), null, "Taxable", 0.09m, 9_000m, 109_000m, null);
        var sellerOrder = SellerOrder.Open(
            checkoutId, sellerPartyId, $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase, "IRR", [line]);
        sellerOrder.RecordVerifiedPayment();
        var checkout = CheckoutGroup.Submit(
            checkoutId, $"idem-{checkoutId:N}", Guid.NewGuid(), OrderMode.OnlinePurchase,
            buyer, actor, "IR", "IRR", SalesChannel.Marketplace, [sellerOrder], now,
            "گیرنده", "09121111111", "تهران", "تهران", "نشانی", "1111111111", "tipax", "Tipax");
        orderDb.Checkouts.Add(checkout);

        // Accepted quote embeds Catalog-era package weight 1500g (frozen at accept).
        var quote = ShippingGroupAcceptedCarrierQuote.Create(
            "tipax", accountId, 1, "7", new string('a', 64), "IRR",
            137_500m, 120_000m, 12_500m, 5_000m, 0m, 0m,
            now, now.AddMinutes(30),
            "101", "202", 1500m, 2_000_000m, 20m, 15m, 10m, null);
        var evidence = ShippingGroupAcceptedEvidence.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, ShippingGroupAcceptedEvidence.SupportedFormatVersion, 1,
            ShippingGroupOwner.Store("store-alpha"),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            132_500m, 5_000m, "IRR", 137_500m, 0m,
            new DateOnly(2026, 11, 4),
            9 * TimeSpan.TicksPerHour,
            17 * TimeSpan.TicksPerHour,
            "Asia/Tehran", 1, 3, quote);
        var materialized = OrderShippingGroupMaterializer.Materialize(
            [new OrderShippingGroupPlan(
                groupId, sellerPartyId, "tipax", null, null, "Tipax", null,
                137_500m, "IRR", new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4), "09:00-17:00", 1,
                [cartLineId], evidence)],
            [(line.LineId, sellerPartyId)],
            new Dictionary<Guid, Guid> { [sellerPartyId] = sellerOrderId },
            new Dictionary<Guid, Guid> { [cartLineId] = line.LineId },
            checkoutId,
            now);
        orderDb.AddRange(materialized);
        await orderDb.SaveChangesAsync();

        // Catalog measurement change after accept.
        var tracked = await catalogDb.Products.SingleAsync(x => x.ProductId == product.ProductId);
        tracked.SetPhysicalSpecification(9999m, 99m, 88m, 77m, DateTimeOffset.UtcNow);
        await catalogDb.SaveChangesAsync();

        orderDb.ChangeTracker.Clear();
        var group = await orderDb.OrderShippingGroups.AsNoTracking()
            .SingleAsync(x => x.ShippingGroupId == groupId);
        Assert.Equal(1500m, group.AcceptedEvidence!.CarrierQuote!.WeightGrams);
        Assert.Equal(20m, group.AcceptedEvidence.CarrierQuote.LengthCentimeters);
        Assert.Equal(100_000m, (await orderDb.Lines.AsNoTracking().SingleAsync(x => x.LineId == line.LineId)).UnitPriceSnapshot);

        catalogDb.ChangeTracker.Clear();
        var catalogAfter = await catalogDb.Products.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId);
        Assert.Equal(9999m, catalogAfter.WeightGrams);
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(builder, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        return new CatalogDbContext(builder.Options);
    }

    private static OrderDbContext CreateOrderDb(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(builder, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(builder.Options);
    }

    private static async Task EnsureMigratedAsync(ShippingCarrierDispatchCompletionRelationalTestTarget target)
    {
        await MigrationGate.WaitAsync();
        try
        {
            await using var catalog = CreateCatalogDb(target.ConnectionString);
            if (!_liveVerified)
            {
                var facts = await catalog.Database
                    .SqlQueryRaw<string>("SELECT (current_database() || '|' || current_user) AS \"Value\"")
                    .SingleAsync();
                var parts = facts.Split('|');
                Assert.Equal(ShippingCarrierDispatchCompletionRelationalTestTarget.ExpectedDatabase, parts[0]);
                Assert.Equal(ShippingCarrierDispatchCompletionRelationalTestTarget.ExpectedUser, parts[1]);
                _liveVerified = true;
            }

            if (!_migrated)
            {
                await using var order = CreateOrderDb(target.ConnectionString);
                var shipBuilder = new DbContextOptionsBuilder<Tooba.Shipping.Infrastructure.Persistence.ShippingDbContext>();
                ToobaNpgsql.ConfigureModuleContext(
                    shipBuilder,
                    target.ConnectionString,
                    Tooba.Shipping.Infrastructure.Persistence.ShippingDbContext.Schema,
                    typeof(Tooba.Shipping.Infrastructure.Persistence.ShippingDbContext));
                await using var shipping = new Tooba.Shipping.Infrastructure.Persistence.ShippingDbContext(shipBuilder.Options);
                var fulBuilder = new DbContextOptionsBuilder<Tooba.Fulfillment.Infrastructure.Persistence.FulfillmentDbContext>();
                ToobaNpgsql.ConfigureModuleContext(
                    fulBuilder,
                    target.ConnectionString,
                    Tooba.Fulfillment.Infrastructure.Persistence.FulfillmentDbContext.Schema,
                    typeof(Tooba.Fulfillment.Infrastructure.Persistence.FulfillmentDbContext));
                await using var fulfillment = new Tooba.Fulfillment.Infrastructure.Persistence.FulfillmentDbContext(fulBuilder.Options);
                await order.Database.MigrateAsync();
                await shipping.GetService<IMigrator>().MigrateAsync();
                await fulfillment.Database.MigrateAsync();
                await catalog.GetService<IMigrator>().MigrateAsync();
                _migrated = true;
            }
        }
        finally
        {
            MigrationGate.Release();
        }
    }
}
