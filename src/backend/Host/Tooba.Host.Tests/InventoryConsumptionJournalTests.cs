using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Directories;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Events;
using Tooba.Inventory.Infrastructure.Messaging;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Offer.Infrastructure.Outbox;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Directories;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003 — مصرف اتمی رزرو، ژورنال مصرف و replay اثبات‌شده روی
/// PostgreSQL واقعی (Testcontainers).
/// <para>
/// این تست‌ها یک تراکنش فیزیکی Inventory، CAS وضعیت رزرو، یکتا بودن ژورنال و rollback کامل
/// (شامل outbox) را با provider تولیدی اثبات می‌کنند؛ نه روی fake درون‌حافظه‌ای.
/// </para>
/// </summary>
[Collection("PostgresSerial")]
public sealed class InventoryConsumptionJournalTests : IAsyncLifetime
{
    private static readonly Dictionary<string, string> LocalizedNames = new() { ["fa-IR"] = "کالا", ["en-US"] = "Item" };

    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_inventory_journal")
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
    public async Task Same_consumer_replay_deducts_stock_once_and_keeps_one_journal_row()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("replay", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-replay", "jr-replay-idem", null, CancellationToken.None);

        await harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-A", CancellationToken.None);
        var afterFirst = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(3, afterFirst!.OnHand);
        Assert.Equal(0, afterFirst.Reserved);

        // replay اثبات‌شدهٔ همان مصرف‌کننده: بدون کسر دوباره و بدون ردیف ژورنال تکراری.
        await harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-A", CancellationToken.None);
        var afterReplay = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(3, afterReplay!.OnHand);
        Assert.Equal(0, afterReplay.Reserved);

        var row = Assert.Single(await harness.Inventory.ReservationConsumptions.AsNoTracking()
            .Where(x => x.ReservationId == reservation.ReservationId)
            .ToListAsync());
        Assert.Equal("consumer-A", row.ConsumerKey);
        Assert.Equal(harness.StockItemId, row.StockItemId);
        Assert.Equal(2m, row.Quantity);
        Assert.Equal(
            StockReservationStatus.Consumed,
            (await harness.Inventory.Reservations.AsNoTracking()
                .SingleAsync(x => x.ReservationId == reservation.ReservationId)).Status);
    }

    [SkippableFact]
    public async Task Conflicting_consumer_is_rejected_without_a_second_deduction()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("conflict", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-conflict", "jr-conflict-idem", null, CancellationToken.None);
        await harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-A", CancellationToken.None);

        var failure = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-B", CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationConsumeMismatch, failure.Code);

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(3, availability!.OnHand);
        Assert.Equal(0, availability.Reserved);
        Assert.Equal(
            1,
            await harness.Inventory.ReservationConsumptions.CountAsync(x => x.ReservationId == reservation.ReservationId));
    }

    [SkippableFact]
    public async Task Legacy_consumed_reservation_without_journal_is_a_typed_conflict()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("legacy", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-legacy", "jr-legacy-idem", null, CancellationToken.None);
        await harness.Directory.ConsumeAsync(reservation.ReservationId, CancellationToken.None);
        Assert.Equal(3, (await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None))!.OnHand);

        // شبیه‌سازی وضعیت تاریخی پیش از مهاجرت: رزرو مصرف‌شدهٔ قدیمی بدون هیچ ردیف ژورنال.
        // این حذف فقط دادهٔ تست را دست می‌زند؛ هیچ backfill یا provenance جعلی ساخته نمی‌شود.
        await harness.Inventory.Database.ExecuteSqlRawAsync(
            "DELETE FROM inventory.reservation_consumptions WHERE reservation_id = {0}",
            reservation.ReservationId);
        Assert.Equal(0, await harness.Inventory.ReservationConsumptions.CountAsync());

        var failure = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-A", CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationConsumeMismatch, failure.Code);

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(3, availability!.OnHand);
        Assert.Equal(0, availability.Reserved);
        Assert.Equal(0, await harness.Inventory.ReservationConsumptions.CountAsync());
    }

    [SkippableFact]
    public async Task Legacy_unkeyed_consume_stays_strict_and_never_double_deducts()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("strict", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-strict", "jr-strict-idem", null, CancellationToken.None);

        await harness.Directory.ConsumeAsync(reservation.ReservationId, CancellationToken.None);

        var failure = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(reservation.ReservationId, CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationNotActive, failure.Code);

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(3, availability!.OnHand);
        Assert.Equal(0, availability.Reserved);
        Assert.Equal(1, await harness.Inventory.ReservationConsumptions.CountAsync());
    }

    [SkippableFact]
    public async Task Failure_after_deduction_rolls_back_stock_reservation_journal_and_outbox()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("rollback", onHand: 5m, injectSaveFailure: true);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-rollback", "jr-rollback-idem", null, CancellationToken.None);
        var outboxBefore = await harness.Inventory.OutboxMessages.CountAsync();

        harness.ArmSaveFailure();

        await Assert.ThrowsAsync<InjectedSaveFailureException>(() =>
            harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-A", CancellationToken.None));

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(5, availability!.OnHand);
        Assert.Equal(2, availability.Reserved);
        Assert.Equal(
            StockReservationStatus.Held,
            (await harness.Inventory.Reservations.AsNoTracking()
                .SingleAsync(x => x.ReservationId == reservation.ReservationId)).Status);
        Assert.Equal(0, await harness.Inventory.ReservationConsumptions.CountAsync());
        Assert.Equal(outboxBefore, await harness.Inventory.OutboxMessages.CountAsync());
    }

    [SkippableFact]
    public async Task Concurrent_same_consumer_deducts_once_with_journal_backed_replay_or_conflict()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("concurrent-same", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-concurrent-same", "jr-concurrent-same-idem", null, CancellationToken.None);

        var first = harness.NewDirectory();
        var second = harness.NewDirectory();
        var outcomes = await Task.WhenAll(
            AttemptAsync(first, reservation.ReservationId, "consumer-A"),
            AttemptAsync(second, reservation.ReservationId, "consumer-A"));

        Assert.Contains(outcomes, x => x is null);
        var conflicts = outcomes.Where(x => x is not null).ToList();
        Assert.All(conflicts, x => Assert.Equal(InventoryErrorCodes.ReservationConsumeMismatch, x!.Code));

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(3, availability!.OnHand);
        Assert.Equal(0, availability.Reserved);
        Assert.Equal(1, await harness.Inventory.ReservationConsumptions.CountAsync());
    }

    [SkippableFact]
    public async Task Concurrent_different_consumers_produce_one_deduction_and_one_conflict()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("concurrent-diff", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-concurrent-diff", "jr-concurrent-diff-idem", null, CancellationToken.None);

        var first = harness.NewDirectory();
        var second = harness.NewDirectory();
        var outcomes = await Task.WhenAll(
            AttemptAsync(first, reservation.ReservationId, "consumer-A"),
            AttemptAsync(second, reservation.ReservationId, "consumer-B"));

        var conflicts = outcomes.Where(x => x is not null).ToList();
        Assert.Single(outcomes.Where(x => x is null));
        var conflict = Assert.Single(conflicts);
        Assert.Equal(InventoryErrorCodes.ReservationConsumeMismatch, conflict!.Code);

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(3, availability!.OnHand);
        Assert.Equal(0, availability.Reserved);
        Assert.Equal(1, await harness.Inventory.ReservationConsumptions.CountAsync());
    }

    [SkippableFact]
    public async Task Released_and_unknown_reservations_keep_their_domain_errors()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("domain-errors", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-released", "jr-released-idem", null, CancellationToken.None);
        await harness.Directory.ReleaseAsync(reservation.ReservationId, CancellationToken.None);

        var keyedReleased = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-A", CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationNotActive, keyedReleased.Code);

        var legacyReleased = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(reservation.ReservationId, CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationNotActive, legacyReleased.Code);

        var keyedUnknown = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(Guid.NewGuid(), "consumer-A", CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationNotFound, keyedUnknown.Code);

        var legacyUnknown = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationNotFound, legacyUnknown.Code);

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(5, availability!.OnHand);
        Assert.Equal(0, availability.Reserved);
        Assert.Equal(0, await harness.Inventory.ReservationConsumptions.CountAsync());
    }

    [SkippableFact]
    public async Task Insufficient_reserved_stock_keeps_the_typed_error_and_rolls_back()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("mismatch", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-mismatch", "jr-mismatch-idem", null, CancellationToken.None);

        // انحراف کنترل‌شدهٔ دادهٔ تست تا گارد اتمی کسر موجودی رد کند.
        await harness.Inventory.Database.ExecuteSqlRawAsync(
            "UPDATE inventory.stock_positions SET reserved = 0 WHERE stock_item_id = {0}",
            harness.StockItemId);

        var failure = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-A", CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationConsumeMismatch, failure.Code);

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(5, availability!.OnHand);
        Assert.Equal(0, availability.Reserved);
        Assert.Equal(
            StockReservationStatus.Held,
            (await harness.Inventory.Reservations.AsNoTracking()
                .SingleAsync(x => x.ReservationId == reservation.ReservationId)).Status);
        Assert.Equal(0, await harness.Inventory.ReservationConsumptions.CountAsync());
    }

    [SkippableFact]
    public async Task Blank_consumer_key_is_rejected_without_touching_stock()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("blank-key", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-blank", "jr-blank-idem", null, CancellationToken.None);

        var failure = await Assert.ThrowsAsync<ContractOperationException>(() =>
            harness.Directory.ConsumeAsync(reservation.ReservationId, "   ", CancellationToken.None));
        Assert.Equal(InventoryErrorCodes.ReservationConsumeMismatch, failure.Code);

        var availability = await harness.Directory.GetAvailabilityAsync(harness.OfferId, CancellationToken.None);
        Assert.Equal(5, availability!.OnHand);
        Assert.Equal(2, availability.Reserved);
        Assert.Equal(0, await harness.Inventory.ReservationConsumptions.CountAsync());
    }

    [SkippableFact]
    public async Task Journal_primary_key_is_a_durable_unique_constraint()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var harness = await SeedAsync("durable-pk", onHand: 5m);
        var reservation = await harness.Directory.ReserveAsync(
            harness.StockItemId, 2, "cart-durable-pk", "jr-durable-pk-idem", null, CancellationToken.None);
        await harness.Directory.ConsumeAsync(reservation.ReservationId, "consumer-A", CancellationToken.None);

        // اثبات محدودیت یکتای واقعی روی پایگاه: PK ژورنال در pg_constraint و رد درج تکراری.
        var constraint = await harness.Inventory.Database.SqlQueryRaw<string>(
                "select conname as \"Value\" from pg_constraint where conrelid = 'inventory.reservation_consumptions'::regclass and contype = 'p'")
            .ToListAsync();
        Assert.Equal("pk_reservation_consumptions", Assert.Single(constraint));

        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => harness.Inventory.Database.ExecuteSqlRawAsync(
            "insert into inventory.reservation_consumptions (reservation_id, stock_item_id, quantity, consumer_key, consumed_at) values ({0}, {1}, {2}, {3}, now())",
            reservation.ReservationId,
            harness.StockItemId,
            2m,
            "consumer-A"));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        Assert.Equal(1, await harness.Inventory.ReservationConsumptions.CountAsync());
    }

    private static async Task<ContractOperationException?> AttemptAsync(
        InventoryDirectory directory,
        Guid reservationId,
        string consumerKey)
    {
        try
        {
            await directory.ConsumeAsync(reservationId, consumerKey, CancellationToken.None);
            return null;
        }
        catch (ContractOperationException ex)
        {
            return ex;
        }
    }

    private async Task<ConsumptionHarness> SeedAsync(string suffix, decimal onHand, bool injectSaveFailure = false)
    {
        var connectionString = _container!.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore($"tenant-jr-{suffix}", $"tenant-jr-{suffix}"));

        var catalog = CreateCatalogDb(connectionString, commerce);
        var party = CreatePartyDb(connectionString, commerce);
        var offer = CreateOfferDb(connectionString, commerce);
        var saveFailure = injectSaveFailure ? new InjectedSaveFailureInterceptor() : null;
        var inventory = CreateInventoryDb(connectionString, commerce, saveFailure);

        await catalog.Database.MigrateAsync();
        await party.Database.MigrateAsync();
        await offer.Database.MigrateAsync();
        await inventory.Database.MigrateAsync();

        var catalogDirectory = new CatalogDirectory(catalog, new OpenCatalogUseCaseGuard());
        var partyDirectory = new PartyDirectory(party);
        var offerDirectory = new OfferDirectory(
            offer, new OpenOfferUseCaseGuard(), catalogDirectory, partyDirectory, new SystemUtcClock(), new UuidV7IdGenerator());
        var directory = new InventoryDirectory(
            inventory,
            new OpenInventoryUseCaseGuard(),
            offerDirectory,
            catalogDirectory,
            new SystemUtcClock(),
            new UuidV7IdGenerator(),
            new ModuleCallTracer());

        var product = await catalogDirectory.CreateProductAsync(
            CatalogProductKind.PhysicalGood, $"jr-{suffix}", null, LocalizedNames, CancellationToken.None);
        var axis = await catalogDirectory.CreateAttributeDefinitionAsync(
            $"size-jr-{suffix}",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["en-US"] = "Size" },
            CancellationToken.None);
        var medium = await catalogDirectory.AddAttributeOptionAsync(
            axis, "m", new Dictionary<string, string> { ["en-US"] = "M" }, CancellationToken.None);
        var variant = await catalogDirectory.CreateVariantAsync(
            product.ProductId, $"JR-{suffix}", [(axis, "ignored", medium)], CancellationToken.None);
        var seller = await partyDirectory.CreateOrganizationAsync($"فروشنده jr {suffix}", null, CancellationToken.None);
        var offerRef = await offerDirectory.CreateOfferAsync(
            variant.VariantId, seller.PartyId, SalesChannel.Marketplace, $"JR-OFFER-{suffix}", CancellationToken.None);
        var location = await directory.CreateLocationAsync($"WH-{suffix}", $"JR WH {suffix}", CancellationToken.None);
        var stockItemId = await directory.OpenPositionAsync(offerRef.OfferId, location, CancellationToken.None);
        if (onHand > 0)
        {
            await directory.AdjustAsync(stockItemId, StockAdjustmentKind.Increase, onHand, "seed", null, CancellationToken.None);
        }

        return new ConsumptionHarness(
            connectionString,
            commerce,
            offerRef.OfferId,
            stockItemId,
            directory,
            inventory,
            catalog,
            party,
            offer,
            offerDirectory,
            catalogDirectory,
            saveFailure);
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

    private static InventoryDbContext CreateInventoryDb(
        string connectionString,
        ICurrentCommerceContext commerce,
        InjectedSaveFailureInterceptor? saveFailure)
    {
        var modules = new IOutboxModuleRegistration[] { new InventoryOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, InventoryDbContext.Schema, typeof(InventoryDbContext));
        options.AddInterceptors(interceptor);
        if (saveFailure is not null)
        {
            options.AddInterceptors(saveFailure);
        }

        return new InventoryDbContext(options.Options);
    }

    /// <summary>
    /// خرابی تزریقی در لحظهٔ ذخیره‌سازی تا rollback کامل تراکنش Inventory اثبات شود.
    /// </summary>
    private sealed class InjectedSaveFailureInterceptor : SaveChangesInterceptor
    {
        private bool _armed;

        public void Arm() => _armed = true;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_armed)
            {
                throw new InjectedSaveFailureException();
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class InjectedSaveFailureException : Exception
    {
    }

    /// <summary>
    /// داده و seams لازم برای یک سناریوی مصرف؛ هر مصرف‌کنندهٔ هم‌زمان DbContext مستقل می‌گیرد.
    /// </summary>
    private sealed class ConsumptionHarness : IAsyncDisposable
    {
        private readonly CatalogDbContext _catalog;
        private readonly PartyDbContext _party;
        private readonly OfferDbContext _offer;
        private readonly OfferDirectory _offerDirectory;
        private readonly CatalogDirectory _catalogDirectory;
        private readonly InjectedSaveFailureInterceptor? _saveFailure;
        private readonly List<InventoryDbContext> _owned = [];

        public ConsumptionHarness(
            string connectionString,
            ICurrentCommerceContext commerce,
            Guid offerId,
            Guid stockItemId,
            InventoryDirectory directory,
            InventoryDbContext inventory,
            CatalogDbContext catalog,
            PartyDbContext party,
            OfferDbContext offer,
            OfferDirectory offerDirectory,
            CatalogDirectory catalogDirectory,
            InjectedSaveFailureInterceptor? saveFailure)
        {
            ConnectionString = connectionString;
            Commerce = commerce;
            OfferId = offerId;
            StockItemId = stockItemId;
            Directory = directory;
            Inventory = inventory;
            _catalog = catalog;
            _party = party;
            _offer = offer;
            _offerDirectory = offerDirectory;
            _catalogDirectory = catalogDirectory;
            _saveFailure = saveFailure;
            _owned.Add(inventory);
        }

        public string ConnectionString { get; }

        public ICurrentCommerceContext Commerce { get; }

        public Guid OfferId { get; }

        public Guid StockItemId { get; }

        public InventoryDirectory Directory { get; }

        public InventoryDbContext Inventory { get; }

        public void ArmSaveFailure() =>
            (_saveFailure ?? throw new InvalidOperationException("Save failure interceptor was not configured.")).Arm();

        /// <summary>
        /// Directory با DbContext مستقل برای یک مصرف‌کنندهٔ هم‌زمان.
        /// </summary>
        public InventoryDirectory NewDirectory()
        {
            var database = CreateInventoryDb(ConnectionString, Commerce, saveFailure: null);
            _owned.Add(database);
            return new InventoryDirectory(
                database,
                new OpenInventoryUseCaseGuard(),
                _offerDirectory,
                _catalogDirectory,
                new SystemUtcClock(),
                new UuidV7IdGenerator(),
                new ModuleCallTracer());
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            foreach (var database in _owned)
            {
                await database.DisposeAsync();
            }

            await _catalog.DisposeAsync();
            await _party.DisposeAsync();
            await _offer.DisposeAsync();
        }
    }
}
