using System.Globalization;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Offer.Application.Offers.Commands.AssignOfferFulfillment;
using System.Reflection;
using Tooba.Pricing.Contracts.Seller;
using Tooba.Inventory.Contracts.Seller;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.Queries.GetOfferHistory;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Offer.Infrastructure.Persistence;
using Xunit;
using OfferChannel = Tooba.Offer.Domain.ValueObjects.SalesChannel;

namespace Tooba.Offer.Tests.Application;

/// <summary>
/// R10-06: recorded Offer history is a bounded read projection; the writer stages on the same unit of work
/// and the seller handler enforces offer ownership without fabricating an actor.
/// </summary>
public sealed class OfferHistoryTests
{
    private static readonly Guid OfferId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid VariantId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid SellerId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid OtherSellerId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-26T00:00:00Z");

    [Fact]
    public async Task Reader_orders_by_occurred_at_descending_and_pages()
    {
        var options = InMemoryOptions();
        var offerId = Guid.NewGuid();
        await using (var writeDb = new OfferDbContext(options))
        {
            var writer = new OfferHistoryStore(writeDb);
            for (var i = 0; i < 5; i++)
            {
                await writer.AppendAsync(Entry(offerId, Now.AddMinutes(i)), CancellationToken.None);
            }

            await writeDb.SaveChangesAsync();
        }

        await using var readDb = new OfferDbContext(options);
        var reader = new OfferHistoryStore(readDb);

        var first = await reader.GetPageAsync(offerId, 1, 2, CancellationToken.None);

        Assert.Equal(5, first.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal(1, first.Page);
        Assert.Equal(2, first.PageSize);
        Assert.Equal(Now.AddMinutes(4), first.Items[0].OccurredAt);
        Assert.Equal(Now.AddMinutes(3), first.Items[1].OccurredAt);

        var second = await reader.GetPageAsync(offerId, 2, 2, CancellationToken.None);
        Assert.Equal(Now.AddMinutes(2), second.Items[0].OccurredAt);
        Assert.Equal(Now.AddMinutes(1), second.Items[1].OccurredAt);
    }

    [Fact]
    public async Task Reader_clamps_page_and_page_size_to_one_and_one_hundred()
    {
        var options = InMemoryOptions();
        var offerId = Guid.NewGuid();
        await using (var writeDb = new OfferDbContext(options))
        {
            var writer = new OfferHistoryStore(writeDb);
            await writer.AppendAsync(Entry(offerId, Now), CancellationToken.None);
            await writeDb.SaveChangesAsync();
        }

        await using var readDb = new OfferDbContext(options);
        var reader = new OfferHistoryStore(readDb);

        var low = await reader.GetPageAsync(offerId, 0, 0, CancellationToken.None);
        Assert.Equal(1, low.Page);
        Assert.Equal(1, low.PageSize);
        Assert.Single(low.Items);

        var high = await reader.GetPageAsync(offerId, 2, 1000, CancellationToken.None);
        Assert.Equal(2, high.Page);
        Assert.Equal(100, high.PageSize);
        Assert.Empty(high.Items);

        var overflow = await reader.GetPageAsync(offerId, int.MaxValue, 100, CancellationToken.None);
        Assert.Empty(overflow.Items);
    }

    [Fact]
    public async Task Writer_stages_the_entry_until_the_single_offer_save_changes()
    {
        var options = InMemoryOptions();
        var offerId = Guid.NewGuid();
        await using (var db = new OfferDbContext(options))
        {
            var writer = new OfferHistoryStore(db);
            await writer.AppendAsync(Entry(offerId, Now), CancellationToken.None);

            await using (var beforeSave = new OfferDbContext(options))
            {
                Assert.Equal(0, await beforeSave.OfferHistoryEntries.CountAsync());
            }

            await db.SaveChangesAsync();
        }

        await using var verify = new OfferDbContext(options);
        Assert.Equal(1, await verify.OfferHistoryEntries.CountAsync());
    }

    [Fact]
    public async Task Seller_history_read_requires_ownership_and_never_fabricates_a_reader_call()
    {
        var reader = new FakeReader();
        var sender = BuildSender(new FakeStore(OfferFor(SellerId)), reader);

        var denied = await sender.Send(new GetOfferHistoryQuery(OfferId, OtherSellerId, 1, 20));

        Assert.True(denied.IsFailure);
        Assert.Equal(OfferErrorCodes.NotFound, denied.FirstError.Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Seller_history_read_returns_the_owned_offer_page()
    {
        var reader = new FakeReader();
        var sender = BuildSender(new FakeStore(OfferFor(SellerId)), reader);

        var result = await sender.Send(new GetOfferHistoryQuery(OfferId, SellerId, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, reader.Calls);
        Assert.Equal(OfferId, reader.LastOfferId);
    }

    [Fact]
    public async Task Admin_history_read_has_no_seller_filter()
    {
        var reader = new FakeReader();
        var sender = BuildSender(new FakeStore(OfferFor(SellerId)), reader);

        var result = await sender.Send(new GetOfferHistoryQuery(OfferId, null, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task History_read_of_a_missing_offer_is_not_found_and_does_not_query_history()
    {
        var reader = new FakeReader();
        var sender = BuildSender(new FakeStore(null), reader);

        var result = await sender.Send(new GetOfferHistoryQuery(OfferId, null, 1, 20));

        Assert.True(result.IsFailure);
        Assert.Equal(OfferErrorCodes.NotFound, result.FirstError.Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public void OfferDbContext_maps_offer_history_and_the_R10_migration_is_discoverable()
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>()
            .UseInMemoryDatabase("offer-history-model-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new OfferDbContext(options);

        var entity = db.Model.FindEntityType(typeof(OfferHistoryEntry));
        Assert.NotNull(entity);
        Assert.Equal("offer_history", entity!.GetTableName());
        Assert.Equal("offer", entity.GetSchema());
        Assert.NotNull(entity.FindProperty(nameof(OfferHistoryEntry.OfferId)));
        Assert.NotNull(entity.FindProperty(nameof(OfferHistoryEntry.ChangeKind)));

        var migration = typeof(OfferDbContext).Assembly.GetTypes()
            .SingleOrDefault(type => type.Name == "R10OfferHistory");
        Assert.NotNull(migration);
        var attribute = migration!.GetCustomAttribute<MigrationAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal("20261126120000_R10OfferHistory", attribute!.Id);
    }

    [Fact]
    public void History_query_validator_rejects_out_of_range_paging()
    {
        var validator = new GetOfferHistoryQueryValidator();

        Assert.False(validator.Validate(new GetOfferHistoryQuery(OfferId, SellerId, 0, 20)).IsValid);
        Assert.False(validator.Validate(new GetOfferHistoryQuery(OfferId, SellerId, 1, 101)).IsValid);
        Assert.True(validator.Validate(new GetOfferHistoryQuery(OfferId, SellerId, 1, 20)).IsValid);
    }

    [Theory]
    [InlineData("fa-IR")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public async Task Owner_history_uses_invariant_values_and_stable_event_keys(string cultureName)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var price = new SellerPriceChangeFact(Guid.NewGuid(), OfferId, Guid.NewGuid(),
                Guid.NewGuid(), SellerId, Now, 1.25m, 2.5m, "IRR", "revision");
            var allocation = new OfferInventoryChangeFact(Guid.NewGuid(), OfferId, null, null,
                "AllocationChanged", Guid.NewGuid(), SellerId, Now, null, null, 10m, 20m, null);
            var physical = new OfferInventoryChangeFact(Guid.NewGuid(), OfferId, Guid.NewGuid(), Guid.NewGuid(),
                "PhysicalChanged", Guid.NewGuid(), SellerId, Now, 90m, 100m, null, null, null);
            var sender = BuildSender(new FakeStore(OfferFor(SellerId)), new FakeReader(),
                new FakePriceHistory([price]), new FakeInventoryHistory([allocation, physical]));

            var result = await sender.Send(new GetOfferHistoryQuery(OfferId, SellerId, 1, 20));
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.TotalCount);
            var priceItem = Assert.Single(result.Value.Items.Where(x => x.Kind == "PriceChanged"));
            Assert.Equal("PriceChanged", priceItem.Summary);
            Assert.Equal("amount=1.25;currency=IRR", priceItem.OldValue);
            Assert.Equal("amount=2.5;currency=IRR", priceItem.NewValue);
            Assert.Equal(price.FactId, priceItem.EventId);
            var allocationItem = Assert.Single(result.Value.Items.Where(x => x.Kind == "AllocationChanged"));
            Assert.Equal("AllocationChanged", allocationItem.Summary);
            Assert.Equal("20", allocationItem.NewValue);
            var inventoryItem = Assert.Single(result.Value.Items.Where(x => x.Kind == "InventoryChanged"));
            Assert.Equal("InventoryChanged", inventoryItem.Summary);
            Assert.Equal(physical.LocationId, inventoryItem.LocationId);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task Responsibility_change_requires_both_owner_proofs(
        bool inventoryAllows, bool orderExists, bool expectedSuccess)
    {
        var offer = OfferFor(SellerId);
        Assert.True(offer.AssignFulfillment(Tooba.Offer.Domain.ValueObjects.FulfillmentResponsibility.Seller,
            null, Now).IsSuccess);
        var store = new FakeStore(offer);
        var gate = new FakeFulfillmentChangeGate(inventoryAllows);
        var orders = new FakeOrderCommitmentReader(orderExists);
        var writer = new RecordingAssignmentHistory();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(AssignOfferFulfillmentCommand).Assembly);
        services.AddSingleton<IOfferStore>(store);
        services.AddSingleton<IShippingTypeReader, EmptyShippingTypes>();
        services.AddSingleton<IClock>(new AssignmentClock());
        services.AddSingleton<IIdGenerator, AssignmentIds>();
        services.AddSingleton<IOfferHistoryWriter>(writer);
        services.AddSingleton<IOfferFulfillmentChangeGate>(gate);
        services.AddSingleton<IOrderOfferCommitmentReader>(orders);
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new AssignOfferFulfillmentCommand(OfferId,
            OfferFulfillmentResponsibility.Platform, null, Now.ToString("O"), SellerId));

        Assert.Equal(expectedSuccess, result.IsSuccess);
        Assert.Equal(1, gate.Calls);
        Assert.Equal(inventoryAllows ? 1 : 0, orders.Calls);
        Assert.Equal(inventoryAllows, gate.Lease.Disposed);
        Assert.Equal(expectedSuccess ? 1 : 0, store.SaveCalls);
        Assert.Equal(expectedSuccess ? 1 : 0, writer.Calls);
        Assert.Equal(expectedSuccess ? Tooba.Offer.Domain.ValueObjects.FulfillmentResponsibility.Platform
            : Tooba.Offer.Domain.ValueObjects.FulfillmentResponsibility.Seller, offer.FulfillmentResponsibility);
        if (!expectedSuccess)
            Assert.Equal(OfferErrorCodes.FulfillmentChangeProtected, result.FirstError.Code);
    }

    private sealed class FakeFulfillmentChangeGate(bool allows) : IOfferFulfillmentChangeGate
    {
        public int Calls { get; private set; }
        public FakeLease Lease { get; } = new();
        public Task<IAsyncDisposable?> TryAcquireAsync(Guid offerId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IAsyncDisposable?>(allows ? Lease : null);
        }
    }

    private sealed class FakeLease : IAsyncDisposable
    {
        public bool Disposed { get; private set; }
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeOrderCommitmentReader(bool exists) : IOrderOfferCommitmentReader
    {
        public int Calls { get; private set; }
        public Task<bool> HasOrderCommitmentAsync(Guid offerId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(exists);
        }
    }

    private sealed class EmptyShippingTypes : IShippingTypeReader
    {
        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>([]);
        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>([]);
    }

    private sealed class AssignmentClock : IClock
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(1);
    }

    private sealed class AssignmentIds : IIdGenerator
    {
        public Guid NewId() => Guid.NewGuid();
    }

    private sealed class RecordingAssignmentHistory : IOfferHistoryWriter
    {
        public int Calls { get; private set; }
        public Task AppendAsync(OfferHistoryEntry entry, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private static DbContextOptions<OfferDbContext> InMemoryOptions()
    {
        var root = new InMemoryDatabaseRoot();
        return new DbContextOptionsBuilder<OfferDbContext>()
            .UseInMemoryDatabase("offer-history-" + Guid.NewGuid().ToString("N"), root)
            .Options;
    }

    private static OfferHistoryEntry Entry(Guid offerId, DateTimeOffset occurredAt) => new()
    {
        OfferHistoryEntryId = Guid.NewGuid(),
        OfferId = offerId,
        OccurredAt = occurredAt,
        ChangeKind = "Created",
        ActorUserId = null,
        Summary = "Offer created",
        OldValue = null,
        NewValue = "status=Draft;channel=Marketplace",
    };

    private static SellerOffer OfferFor(Guid sellerId) =>
        SellerOffer.Create(OfferId, VariantId, sellerId, OfferChannel.Marketplace, "SKU-1", Now);

    private static ISender BuildSender(IOfferStore store, IOfferHistoryReader reader,
        IOfferPriceHistoryReader? priceHistory = null, IOfferInventoryHistoryReader? inventoryHistory = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(GetOfferHistoryQuery).Assembly);
        services.AddSingleton(store);
        services.AddSingleton(reader);
        services.AddSingleton<Tooba.Pricing.Contracts.Seller.IOfferPriceHistoryReader>(priceHistory ?? new FakePriceHistory());
        services.AddSingleton<Tooba.Inventory.Contracts.Seller.IOfferInventoryHistoryReader>(inventoryHistory ?? new FakeInventoryHistory());
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private sealed class FakePriceHistory(IReadOnlyList<SellerPriceChangeFact>? facts = null) : Tooba.Pricing.Contracts.Seller.IOfferPriceHistoryReader
    {
        public Task<IReadOnlyList<Tooba.Pricing.Contracts.Seller.SellerPriceChangeFact>> ListAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tooba.Pricing.Contracts.Seller.SellerPriceChangeFact>>(facts ?? []);
        public Task<Tooba.Pricing.Contracts.Seller.SellerPriceRevision?> GetRevisionAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<Tooba.Pricing.Contracts.Seller.SellerPriceRevision?>(null);
        public Task<IReadOnlyList<Tooba.Pricing.Contracts.Seller.SellerOfferPriceView>> ListBasePricesAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tooba.Pricing.Contracts.Seller.SellerOfferPriceView>>([]);
    }

    private sealed class FakeInventoryHistory(IReadOnlyList<OfferInventoryChangeFact>? facts = null) : Tooba.Inventory.Contracts.Seller.IOfferInventoryHistoryReader
    {
        public Task<IReadOnlyList<Tooba.Inventory.Contracts.Seller.OfferInventoryChangeFact>> ListAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tooba.Inventory.Contracts.Seller.OfferInventoryChangeFact>>(facts ?? []);
    }

    private sealed class FakeReader : IOfferHistoryReader
    {
        public Task<IReadOnlyList<OfferHistoryItem>> ListAsync(Guid offerId, CancellationToken cancellationToken)
        {
            Calls++;
            LastOfferId = offerId;
            return Task.FromResult<IReadOnlyList<OfferHistoryItem>>([]);
        }
        public int Calls { get; private set; }

        public Guid? LastOfferId { get; private set; }

        public Task<OfferHistoryPage> GetPageAsync(
            Guid offerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastOfferId = offerId;
            return Task.FromResult(new OfferHistoryPage([], 0, page, pageSize));
        }
    }

    private sealed class FakeStore(SellerOffer? offer) : IOfferStore
    {
        public Task<SellerOffer?> GetByIdAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult(offer is not null && offer.OfferId == offerId ? offer : null);

        public Task<IReadOnlyList<SellerOffer>> ListBySellerAsync(
            Guid sellerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SellerOffer>>(offer is null ? [] : [offer]);

        public Task AddAsync(SellerOffer value, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<SellerOffer?> PersistNewAsync(SellerOffer value, CancellationToken cancellationToken) =>
            Task.FromResult<SellerOffer?>(null);

        public int SaveCalls { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsActiveListingAsync(
            Guid sellerPartyId,
            Guid catalogVariantId,
            Tooba.Offer.Domain.ValueObjects.SalesChannel channel,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<Guid?> FindActiveListingOfferIdAsync(
            Guid sellerPartyId,
            Guid catalogVariantId,
            Tooba.Offer.Domain.ValueObjects.SalesChannel channel,
            CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);

        public Task<bool> ExistsSellerSkuAsync(
            Guid sellerPartyId,
            string sellerSku,
            Guid? excludingOfferId,
            CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
