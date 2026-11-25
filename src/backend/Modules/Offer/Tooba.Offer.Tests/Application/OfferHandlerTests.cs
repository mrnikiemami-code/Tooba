using Tooba.Offer.Contracts.ReturnPolicy;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Offers.Commands.ActivateOffer;
using Tooba.Offer.Application.Offers.Commands.CreateOffer;
using Tooba.Offer.Application.Offers.Commands.UpdateOffer;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.Queries.GetOffer;
using Tooba.Offer.Application.Offers.Queries.ListSellerOffers;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.ValueObjects;
using Tooba.Party.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Xunit;
using ContractChannel = Tooba.Offer.Contracts.Dtos.SalesChannel;
using ContractStatus = Tooba.Offer.Contracts.Dtos.OfferStatus;

namespace Tooba.Offer.Tests.Application;

public sealed class OfferHandlerTests
{
    private static readonly Guid OfferId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid VariantId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid SellerId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-21T00:00:00Z");

    [Fact]
    public async Task Create_rejects_missing_variant()
    {
        var sender = BuildSender(new FakeCatalog(null), new FakeParty(new PartyLookupResult(SellerId, "Organization")), out _);
        var result = await sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, "SKU"));
        Assert.True(result.IsFailure);
        Assert.Equal(OfferErrorCodes.CatalogVariantMissing, result.FirstError.Code);
    }

    [Fact]
    public async Task Create_rejects_missing_seller()
    {
        var sender = BuildSender(new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())), new FakeParty(null), out _);
        var result = await sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, "SKU"));
        Assert.True(result.IsFailure);
        Assert.Equal(OfferErrorCodes.SellerMissing, result.FirstError.Code);
    }

    [Fact]
    public async Task Create_update_lifecycle_get_and_list_are_deterministic()
    {
        var sender = BuildSender(
            new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())),
            new FakeParty(new PartyLookupResult(SellerId, "Organization")),
            out var store);
        var created = await sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, " SKU "));
        Assert.True(created.IsSuccess);
        Assert.Equal(OfferId, created.Value.OfferId);
        Assert.Equal(Now, store.Items.Single().CreatedAt);

        var active = await sender.Send(new ActivateOfferCommand(OfferId, SellerId));
        Assert.True(active.IsSuccess);
        Assert.Equal(ContractStatus.Active, active.Value.Status);
        var updated = await sender.Send(new UpdateOfferCommand(OfferId, SellerId, "SKU-2", nameof(ContractStatus.Suspended)));
        Assert.True(updated.IsSuccess);
        Assert.Equal(nameof(ContractStatus.Suspended), updated.Value.Status);
        Assert.Equal("SKU-2", updated.Value.SellerSku);
        var detail = await sender.Send(new GetOfferQuery(OfferId, SellerId));
        Assert.True(detail.IsSuccess);
        Assert.Equal(OfferId, detail.Value.OfferId);
        var list = await sender.Send(new ListSellerOffersQuery(SellerId));
        Assert.True(list.IsSuccess);
        Assert.Single(list.Value);
    }

    [Fact]
    public async Task Get_returns_not_found_as_result_failure()
    {
        var sender = BuildSender(
            new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())),
            new FakeParty(new PartyLookupResult(SellerId, "Organization")),
            out _);
        var result = await sender.Send(new GetOfferQuery(OfferId, SellerId));
        Assert.True(result.IsFailure);
        Assert.Equal(OfferErrorCodes.NotFound, result.FirstError.Code);
    }

    private static ISender BuildSender(
        ICatalogVariantLookup catalog,
        IPartyLookup party,
        out FakeStore store,
        ToobaEdition edition = ToobaEdition.Marketplace,
        IOfferHistoryWriter? history = null)
    {
        store = new FakeStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateOfferCommand).Assembly);
        services.AddSingleton<IOfferStore>(store);
        services.AddSingleton(catalog);
        services.AddSingleton(party);
        services.AddSingleton<ICurrentEdition>(new FixedEdition(edition));
        services.AddSingleton<ICatalogOfferReadGateway, FakeCatalogReads>();
        services.AddSingleton<IPriceLookupGateway, FakePrices>();
        services.AddSingleton<ISellerOfferInventoryGateway, FakeInventory>();
        services.AddSingleton<OfferReadModelComposer>();
        services.AddSingleton<Tooba.Offer.Contracts.Ports.IOfferFulfillmentAssignmentLookup, Tooba.Offer.Tests.TestFulfillmentAssignments>();
        services.AddSingleton<IClock>(new FixedClock(Now));
        services.AddSingleton<IIdGenerator>(new FixedIds(OfferId));
        services.AddSingleton(history ?? new NoopHistoryWriter());
        services.AddSingleton<IReturnPolicyResolver>(new ReturnPolicyResolver(new ReturnPolicyOptions()));
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private sealed class FakeStore : IOfferStore
    {
        public List<SellerOffer> Items { get; } = [];
        public Task<SellerOffer?> GetByIdAsync(Guid id, CancellationToken token) => Task.FromResult(Items.SingleOrDefault(x => x.OfferId == id));
        public Task<IReadOnlyList<SellerOffer>> ListBySellerAsync(Guid id, CancellationToken token) => Task.FromResult<IReadOnlyList<SellerOffer>>(Items.Where(x => x.SellerPartyId == id).ToList());
        public Task AddAsync(SellerOffer offer, CancellationToken token) { Items.Add(offer); return Task.CompletedTask; }
        public Task<SellerOffer?> PersistNewAsync(SellerOffer offer, CancellationToken token) { Items.Add(offer); return Task.FromResult<SellerOffer?>(null); }
        public Task SaveChangesAsync(CancellationToken token) => Task.CompletedTask;
        public Task<bool> ExistsActiveListingAsync(Guid sellerId, Guid variantId, Tooba.Offer.Domain.ValueObjects.SalesChannel channel, CancellationToken token) => Task.FromResult(false);
        public Task<Guid?> FindActiveListingOfferIdAsync(Guid sellerId, Guid variantId, Tooba.Offer.Domain.ValueObjects.SalesChannel channel, CancellationToken token) => Task.FromResult<Guid?>(null);
        public Task<bool> ExistsSellerSkuAsync(Guid sellerId, string sku, Guid? excludingId, CancellationToken token) => Task.FromResult(false);
    }

    private sealed class FakeCatalog(CatalogVariantLookupResult? result) : ICatalogVariantLookup
    {
        public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid id, CancellationToken token) => Task.FromResult(result);
        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken token) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(new Dictionary<Guid, Guid?>());
        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken token) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }
    private sealed class FakeParty(PartyLookupResult? result) : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid id, CancellationToken token) => Task.FromResult(result);
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken token) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(string term, int take, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }
    private sealed class FakeCatalogReads : ICatalogOfferReadGateway
    {
        public Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken token, string? locale = null) =>
            Task.FromResult<IReadOnlyDictionary<Guid, CatalogOfferPresentation>>(
                ids.ToDictionary(x => x, x => new CatalogOfferPresentation(x, Guid.NewGuid(), "Product", null, null, null, null)));
    }
    private sealed class FakePrices : IPriceLookupGateway
    {
        public Task<PriceQuote?> ResolvePriceAsync(PriceResolutionQuery query, CancellationToken token) => Task.FromResult<PriceQuote?>(null);
        public Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolvePricesBatchAsync(IReadOnlyCollection<Guid> ids, string market, ContractChannel channel, string currency, DateTimeOffset at, CancellationToken token) => Task.FromResult<IReadOnlyDictionary<Guid, PriceQuote>>(new Dictionary<Guid, PriceQuote>());
        public Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolveCampaignPricesBatchAsync(IReadOnlyCollection<Guid> ids, Guid campaignId, string market, ContractChannel channel, string currency, DateTimeOffset at, CancellationToken token) => Task.FromResult<IReadOnlyDictionary<Guid, PriceQuote>>(new Dictionary<Guid, PriceQuote>());
    }
    private sealed class FakeInventory : ISellerOfferInventoryGateway
    {
        public Task<IReadOnlyDictionary<Guid, OfferInventorySummary>> GetAvailabilityAsync(IReadOnlyCollection<Guid> ids, CancellationToken token) => Task.FromResult<IReadOnlyDictionary<Guid, OfferInventorySummary>>(new Dictionary<Guid, OfferInventorySummary>());
        public Task<Result> SetInventoryAsync(SetSellerOfferInventory request, CancellationToken token) => Task.FromResult(Result.Success());
    }
    private sealed class FixedIds(Guid id) : IIdGenerator
    { public Guid NewId() => id; }

    private sealed class NoopHistoryWriter : IOfferHistoryWriter
    {
        public Task AppendAsync(OfferHistoryEntry entry, CancellationToken token) => Task.CompletedTask;
    }

    private sealed class RecordingHistoryWriter : IOfferHistoryWriter
    {
        public List<OfferHistoryEntry> Entries { get; } = [];

        public Task AppendAsync(OfferHistoryEntry entry, CancellationToken token)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedEdition(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current { get; } = new EditionContext(edition, "test");
    }

    [Fact]
    public async Task Draft_sku_patch_stays_draft_and_clears_both_limits()
    {
        var sender = BuildSender(
            new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())),
            new FakeParty(new PartyLookupResult(SellerId, "Organization")),
            out var store);
        var created = await sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, "SKU-1"));
        Assert.True(created.IsSuccess);

        var limits = await sender.Send(new UpdateOfferCommand(
            OfferId, SellerId, null, null, MinimumOrderQuantity: 2, MaximumOrderQuantity: 5));
        Assert.True(limits.IsSuccess);
        Assert.Equal(2, limits.Value.MinimumOrderQuantity);
        Assert.Equal(5, limits.Value.MaximumOrderQuantity);

        var cleared = await sender.Send(new UpdateOfferCommand(
            OfferId, SellerId, "SKU-2", null, ClearOrderQuantityLimits: true));
        Assert.True(cleared.IsSuccess);
        Assert.Equal("SKU-2", cleared.Value.SellerSku);
        Assert.Equal(nameof(ContractStatus.Draft), cleared.Value.Status);
        Assert.Null(cleared.Value.MinimumOrderQuantity);
        Assert.Null(cleared.Value.MaximumOrderQuantity);
        Assert.Equal(0m, cleared.Value.OnHand);
    }

    [Fact]
    public async Task Create_rejects_a_channel_not_allowed_by_the_edition_policy()
    {
        var sender = BuildSender(
            new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())),
            new FakeParty(new PartyLookupResult(SellerId, "Organization")),
            out var store,
            ToobaEdition.SingleStore);

        var result = await sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, "SKU"));

        Assert.True(result.IsFailure);
        Assert.Equal(OfferErrorCodes.ChannelNotAllowed, result.FirstError.Code);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task Create_and_update_stage_history_entries_with_unknown_actor()
    {
        var history = new RecordingHistoryWriter();
        var sender = BuildSender(
            new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())),
            new FakeParty(new PartyLookupResult(SellerId, "Organization")),
            out _,
            history: history);

        var created = await sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, "SKU"));
        Assert.True(created.IsSuccess);
        var updated = await sender.Send(new UpdateOfferCommand(OfferId, SellerId, "SKU-2", null));
        Assert.True(updated.IsSuccess);

        Assert.Equal(2, history.Entries.Count);
        Assert.Equal("Created", history.Entries[0].ChangeKind);
        Assert.Null(history.Entries[0].ActorUserId);
        Assert.Contains("status=Draft", history.Entries[0].NewValue, StringComparison.Ordinal);
        Assert.Contains("channel=Marketplace", history.Entries[0].NewValue, StringComparison.Ordinal);
        Assert.Equal("CommercialChanged", history.Entries[1].ChangeKind);
        Assert.Null(history.Entries[1].ActorUserId);
        Assert.Contains("sku=SKU", history.Entries[1].OldValue, StringComparison.Ordinal);
        Assert.Contains("sku=SKU-2", history.Entries[1].NewValue, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_carries_the_allowed_channel_into_the_persisted_offer_and_reread()
    {
        var sender = BuildSender(
            new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())),
            new FakeParty(new PartyLookupResult(SellerId, "Organization")),
            out var store,
            ToobaEdition.SingleStore);

        var result = await sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Direct, "SKU"));

        Assert.True(result.IsSuccess);
        Assert.Equal(Tooba.Offer.Domain.ValueObjects.SalesChannel.Direct, store.Items.Single().Channel);
        Assert.Equal(nameof(ContractChannel.Direct), result.Value.Channel);

        var updated = await sender.Send(new UpdateOfferCommand(OfferId, SellerId, "SKU-2", null));

        Assert.True(updated.IsSuccess);
        Assert.Equal(Tooba.Offer.Domain.ValueObjects.SalesChannel.Direct, store.Items.Single().Channel);
        Assert.Equal(nameof(ContractChannel.Direct), updated.Value.Channel);
    }
}


