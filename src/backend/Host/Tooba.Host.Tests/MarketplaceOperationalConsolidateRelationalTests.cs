using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Order.Domain.Enums;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Messaging;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Guards;
using Tooba.Order.Infrastructure.Integrations.Fulfillment;
using Tooba.Order.Infrastructure.Messaging;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-253-R1 disposable PostgreSQL matrix: Marketplace operational consolidate authority → Fulfillment package.
/// Controlled local Postgres only; never Tipax live.
/// </summary>
[Collection("PostgresSerial")]
public sealed class MarketplaceOperationalConsolidateRelationalTests : IAsyncLifetime
{
    public const string EnvVar = "TOOBA_TB253R1_POSTGRES";
    public const string ExpectedHost = "127.0.0.1";
    public const int ExpectedPort = 15433;
    public const string ExpectedDatabase = "shipping_tb253r1";
    public const string ExpectedUser = "tb253r1";

    private string? _connectionString;
    private bool _ready;

    public Task InitializeAsync()
    {
        _connectionString = Environment.GetEnvironmentVariable(EnvVar);
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _ready = false;
            return Task.CompletedTask;
        }

        AssertTarget(_connectionString);
        _ready = true;
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "TB253R1")]
    [Trait("Case", "authority-refusals")]
    public void Case_authority_refuses_incompatible_before_allocation()
    {
        var checkout = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var a = Fact(checkout, Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "WH-1");
        var seller = a with
        {
            ShippingGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd2"),
            SellerPartyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            SellerOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
            OwnerKind = MarketplaceOperationalDeliveryGroupAuthority.OwnerKindSeller,
            OwnerSellerPartyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
        };
        var store = a with
        {
            ShippingGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd3"),
            SellerPartyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            SellerOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3"),
            OwnerKind = MarketplaceOperationalDeliveryGroupAuthority.OwnerKindStore,
            OwnerTenantKey = "store-alpha",
        };
        var crossCheckout = a with
        {
            ShippingGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd4"),
            SellerPartyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            SellerOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc4"),
            CheckoutId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab"),
        };
        var missingOrigin = a with
        {
            ShippingGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd5"),
            SellerPartyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            SellerOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc5"),
            OriginLocationCode = null,
            OriginPostalAddress = null,
        };
        var missingDate = a with
        {
            ShippingGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd6"),
            SellerPartyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            SellerOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc6"),
            ReceivingDate = null,
        };
        var differentHour = a with
        {
            ShippingGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd7"),
            SellerPartyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            SellerOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc7"),
            ReceivingOpenTicks = 15L * TimeSpan.TicksPerHour,
            ReceivingCloseTicks = 16L * TimeSpan.TicksPerHour,
        };
        var differentCurrency = a with
        {
            ShippingGroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd8"),
            SellerPartyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            SellerOrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc8"),
            Currency = "USD",
        };
        var b = Fact(
            checkout,
            Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd9"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            "WH-1",
            sellerOrderId: Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc9"));

        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed([a, seller]));
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed([a, store]));
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed([a, crossCheckout]));
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed([a, missingOrigin]));
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed([a, missingDate]));
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed([a, differentHour]));
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed([a, differentCurrency]));
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.DeriveAcceptedMethodCode([a, b], "heavy"));
        Assert.Equal("post", MarketplaceOperationalDeliveryGroupAuthority.DeriveAcceptedMethodCode([a, b], null));
    }

    [SkippableFact]
    [Trait("Category", "TB253R1")]
    [Trait("Case", "1")]
    public async Task Case01_compatible_marketplace_creates_package_with_derived_method()
    {
        Skip.If(!_ready || string.IsNullOrWhiteSpace(_connectionString), $"{EnvVar} disposable target not ready.");
        var (directory, fulfillmentDb, checkoutId, actor, ready, members, _) =
            await SeedTwoSellerReadyShipmentsAsync(_connectionString!, "c1");

        MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed(members);
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.DeriveAcceptedMethodCode(members, "heavy"));
        var method = MarketplaceOperationalDeliveryGroupAuthority.DeriveAcceptedMethodCode(members, null);
        Assert.Equal("post", method);

        var created = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [ready[0].ShipmentId, ready[1].ShipmentId],
            method,
            "TRK-253R1",
            null,
            actor,
            CancellationToken.None);

        Assert.Equal(ConsolidatedPackageStatus.Created, created.Status);
        Assert.Equal("post", created.ShippingMethodCode);
        Assert.Equal(2, created.Members.Count);
        Assert.Contains(created.Members, m => m.ShipmentId == ready[0].ShipmentId);
        Assert.Contains(created.Members, m => m.ShipmentId == ready[1].ShipmentId);
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync(x =>
            x.CheckoutId == checkoutId && x.Status == ConsolidatedPackageStatus.Created));
    }

    [SkippableFact]
    [Trait("Category", "TB253R1")]
    [Trait("Case", "2")]
    public async Task Case02_concurrent_twin_allocates_one_package()
    {
        Skip.If(!_ready || string.IsNullOrWhiteSpace(_connectionString), $"{EnvVar} disposable target not ready.");
        var (directory, fulfillmentDb, checkoutId, actor, ready, members, commerce) =
            await SeedTwoSellerReadyShipmentsAsync(_connectionString!, "c2");
        var method = MarketplaceOperationalDeliveryGroupAuthority.DeriveAcceptedMethodCode(members, null);

        var dbB = CreateFulfillmentDb(_connectionString!, commerce);
        var directoryB = new FulfillmentDirectory(
            dbB,
            new OpenFulfillmentUseCaseGuard(),
            new OrderFulfillmentBridge(CreateOrderDb(_connectionString!, commerce)),
            new NoopInventoryGateway(),
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

        var shipmentIds = new[] { ready[0].ShipmentId, ready[1].ShipmentId };
        var results = await Task.WhenAll(
            TryCreateAsync(directory, checkoutId, shipmentIds, method, actor),
            TryCreateAsync(directoryB, checkoutId, shipmentIds, method, actor));

        Assert.Contains(results, x => x.Ok);
        Assert.Contains(results, x => !x.Ok);
        // Re-query through a fresh context so concurrent twin writes are visible.
        await using var probe = CreateFulfillmentDb(_connectionString!, commerce);
        Assert.Equal(1, await probe.ConsolidatedPackages.CountAsync(x =>
            x.CheckoutId == checkoutId && x.Status == ConsolidatedPackageStatus.Created));
    }

    [SkippableFact]
    [Trait("Category", "TB253R1")]
    [Trait("Case", "3")]
    public async Task Case03_cancel_then_rebuild_reuses_identity_without_duplicate_active()
    {
        Skip.If(!_ready || string.IsNullOrWhiteSpace(_connectionString), $"{EnvVar} disposable target not ready.");
        var (directory, _, checkoutId, actor, ready, members, _) =
            await SeedTwoSellerReadyShipmentsAsync(_connectionString!, "c3");
        var method = MarketplaceOperationalDeliveryGroupAuthority.DeriveAcceptedMethodCode(members, null);

        var created = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [ready[0].ShipmentId, ready[1].ShipmentId],
            method,
            null,
            null,
            actor,
            CancellationToken.None);
        await directory.CancelConsolidatedPackageAsync(created.ConsolidatedPackageId, actor, CancellationToken.None);

        var rebuilt = await directory.CreateConsolidatedPackageAsync(
            checkoutId,
            [ready[0].ShipmentId, ready[1].ShipmentId],
            method,
            null,
            null,
            actor,
            CancellationToken.None);
        Assert.Equal(ConsolidatedPackageStatus.Created, rebuilt.Status);
        Assert.Equal("post", rebuilt.ShippingMethodCode);
        Assert.NotEqual(created.ConsolidatedPackageId, rebuilt.ConsolidatedPackageId);
        var all = await directory.GetPackagesForCheckoutAsync(checkoutId, CancellationToken.None);
        Assert.Equal(1, all.Count(x => x.Status == ConsolidatedPackageStatus.Created));
        Assert.Equal(1, all.Count(x => x.Status == ConsolidatedPackageStatus.Cancelled));
    }

    private static async Task<(bool Ok, string? Error)> TryCreateAsync(
        FulfillmentDirectory directory,
        Guid checkoutId,
        IReadOnlyList<Guid> shipmentIds,
        string method,
        Guid actor)
    {
        try
        {
            await directory.CreateConsolidatedPackageAsync(
                checkoutId, shipmentIds, method, null, null, actor, CancellationToken.None);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<(
        FulfillmentDirectory Directory,
        FulfillmentDbContext FulfillmentDb,
        Guid CheckoutId,
        Guid Actor,
        IReadOnlyList<(Guid FulfillmentId, Guid ShipmentId)> Ready,
        IReadOnlyList<MarketplaceOperationalDeliveryGroupAuthority.GroupFact> Members,
        FixedCommerceContext Commerce)> SeedTwoSellerReadyShipmentsAsync(
        string connectionString,
        string dbSuffix)
    {
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore($"store-tb253r1-{dbSuffix}", $"tenant-tb253r1-{dbSuffix}"));

        var orderDb = CreateOrderDb(connectionString, commerce);
        var fulfillmentDb = CreateFulfillmentDb(connectionString, commerce);
        await orderDb.Database.MigrateAsync();
        await fulfillmentDb.Database.MigrateAsync();

        var buyer = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var checkoutId = Guid.NewGuid();
        var orderAId = Guid.NewGuid();
        var orderBId = Guid.NewGuid();
        var lineA = OrderLine.FromCheckout(
            orderAId, Guid.NewGuid(), Guid.NewGuid(), sellerA, 1, 50000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 9000m, 59000m, null);
        var lineB = OrderLine.FromCheckout(
            orderBId, Guid.NewGuid(), Guid.NewGuid(), sellerB, 1, 40000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 3600m, 43600m, null);
        var soA = SellerOrder.Open(checkoutId, sellerA, $"SO-{orderAId:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineA]);
        var soB = SellerOrder.Open(checkoutId, sellerB, $"SO-{orderBId:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineB]);
        var group = CheckoutGroup.Submit(
            checkoutId, $"idem-{checkoutId:N}", Guid.NewGuid(), OrderMode.OnlinePurchase, buyer, actor,
            "IR", "IRR", SalesChannel.Marketplace, [soA, soB], now);
        foreach (var sellerOrder in group.SellerOrders)
        {
            sellerOrder.RecordVerifiedPayment();
        }

        orderDb.Checkouts.Add(group);
        await orderDb.SaveChangesAsync();

        var directory = new FulfillmentDirectory(
            fulfillmentDb,
            new OpenFulfillmentUseCaseGuard(),
            new OrderFulfillmentBridge(orderDb),
            new NoopInventoryGateway(),
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [soA.SellerOrderId, soB.SellerOrderId],
            CancellationToken.None);

        var ready = new List<(Guid FulfillmentId, Guid ShipmentId)>();
        const string postMeta = """{"recipientName":"Test Buyer","destinationAddress":"Tehran Address 1","recipientPhone":"09121234567","postalCode":"1234567890"}""";
        foreach (var sellerOrderId in new[] { soA.SellerOrderId, soB.SellerOrderId })
        {
            var unit = await directory.GetBySellerOrderAsync(sellerOrderId, CancellationToken.None);
            Assert.NotNull(unit);
            await directory.MarkProcessingAsync(unit!.FulfillmentId, actor, CancellationToken.None);
            await directory.MarkPackedAsync(unit.FulfillmentId, actor, CancellationToken.None);
            var shipped = (await directory.CreateShipmentAsync(
                unit.FulfillmentId,
                actor,
                "پست",
                [new ShipmentLineCommand(unit.Items[0].OrderLineId, 1)],
                CancellationToken.None,
                shippingMethodCode: "post",
                providerMetadataJson: postMeta)).Snapshot;
            ready.Add((unit.FulfillmentId, shipped.Shipments.Single().ShipmentId));
        }

        var day = new DateOnly(2026, 11, 10);
        var open = 14L * TimeSpan.TicksPerHour;
        var close = 15L * TimeSpan.TicksPerHour;
        var members = new List<MarketplaceOperationalDeliveryGroupAuthority.GroupFact>
        {
            Fact(checkoutId, Guid.NewGuid(), sellerA, "WH-1", sellerOrderId: soA.SellerOrderId, day: day, open: open, close: close),
            Fact(checkoutId, Guid.NewGuid(), sellerB, "WH-1", sellerOrderId: soB.SellerOrderId, day: day, open: open, close: close),
        };

        return (directory, fulfillmentDb, checkoutId, actor, ready, members, commerce);
    }

    private static MarketplaceOperationalDeliveryGroupAuthority.GroupFact Fact(
        Guid checkoutId,
        Guid groupId,
        Guid sellerPartyId,
        string origin,
        Guid? sellerOrderId = null,
        DateOnly? day = null,
        long? open = null,
        long? close = null) =>
        new(
            groupId,
            checkoutId,
            sellerOrderId ?? Guid.NewGuid(),
            sellerPartyId,
            "post",
            null,
            null,
            "پیشتاز",
            "IRR",
            125000m,
            MarketplaceOperationalDeliveryGroupAuthority.OwnerKindMarketplace,
            null,
            null,
            day ?? new DateOnly(2026, 11, 10),
            open ?? 14L * TimeSpan.TicksPerHour,
            close ?? 15L * TimeSpan.TicksPerHour,
            "Asia/Tehran",
            "Manual",
            origin,
            null,
            null,
            null,
            null,
            null,
            "تهران\u001fتهران\u001f1234567890\u001fخیابان نمونه",
            [Guid.NewGuid()],
            true,
            null);

    private static void AssertTarget(string connectionString)
    {
        Assert.Contains(ExpectedHost, connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ExpectedPort.ToString(), connectionString, StringComparison.Ordinal);
        Assert.Contains(ExpectedDatabase, connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ExpectedUser, connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tooba_alpha", connectionString, StringComparison.OrdinalIgnoreCase);
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

    private static FulfillmentDbContext CreateFulfillmentDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new FulfillmentOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
        options.AddInterceptors(interceptor);
        return new FulfillmentDbContext(options.Options);
    }

    private sealed class NoopInventoryGateway : IFulfillmentInventoryGateway
    {
        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
