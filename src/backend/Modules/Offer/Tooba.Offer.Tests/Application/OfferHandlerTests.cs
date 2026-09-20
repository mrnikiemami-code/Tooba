using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts;
using Tooba.Offer.Application.Commands.ActivateOffer;
using Tooba.Offer.Application.Commands.CreateOffer;
using Tooba.Offer.Application.Commands.UpdateOffer;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.Queries.GetOffer;
using Tooba.Offer.Application.Queries.ListSellerOffers;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.ValueObjects;
using Tooba.Party.Contracts;
using Tooba.Pricing.Contracts;
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
        var error = await Assert.ThrowsAsync<SemanticException>(() =>
            sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, "SKU")));
        Assert.Equal(OfferErrorCodes.CatalogVariantMissing, error.Error.Code);
    }

    [Fact]
    public async Task Create_rejects_missing_seller()
    {
        var sender = BuildSender(new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())), new FakeParty(null), out _);
        var error = await Assert.ThrowsAsync<SemanticException>(() =>
            sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, "SKU")));
        Assert.Equal(OfferErrorCodes.SellerMissing, error.Error.Code);
    }

    [Fact]
    public async Task Create_update_lifecycle_get_and_list_are_deterministic()
    {
        var sender = BuildSender(
            new FakeCatalog(new CatalogVariantLookupResult(VariantId, Guid.NewGuid())),
            new FakeParty(new PartyLookupResult(SellerId, "Organization")),
            out var store);
        var created = await sender.Send(new CreateOfferCommand(VariantId, SellerId, ContractChannel.Marketplace, " SKU "));
        Assert.Equal(OfferId, created.OfferId);
        Assert.Equal(Now, store.Items.Single().CreatedAt);

        var active = await sender.Send(new ActivateOfferCommand(OfferId, SellerId));
        Assert.Equal(ContractStatus.Active, active.Status);
        var updated = await sender.Send(new UpdateOfferCommand(OfferId, SellerId, "SKU-2", nameof(ContractStatus.Suspended)));
        Assert.Equal(nameof(ContractStatus.Suspended), updated.Status);
        Assert.Equal("SKU-2", updated.SellerSku);
        Assert.Equal(OfferId, (await sender.Send(new GetOfferQuery(OfferId, SellerId))).OfferId);
        Assert.Single(await sender.Send(new ListSellerOffersQuery(SellerId)));
    }

    private static ISender BuildSender(ICatalogVariantLookup catalog, IPartyLookup party, out FakeStore store)
    {
        store = new FakeStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateOfferCommand).Assembly);
        services.AddSingleton<IOfferStore>(store);
        services.AddSingleton(catalog);
        services.AddSingleton(party);
        services.AddSingleton<ICatalogOfferReadGateway, FakeCatalogReads>();
        services.AddSingleton<IPriceLookupGateway, FakePrices>();
        services.AddSingleton<ISellerOfferInventoryGateway, FakeInventory>();
        services.AddSingleton<OfferReadModelComposer>();
        services.AddSingleton<IClock>(new FixedClock(Now));
        services.AddSingleton<IIdGenerator>(new FixedIds(OfferId));
        services.AddSingleton<IReturnPolicyResolver>(new ReturnPolicyResolver(new ReturnPolicyOptions()));
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private sealed class FakeStore : IOfferStore
    {
        public List<SellerOffer> Items { get; } = [];
        public Task<SellerOffer?> GetByIdAsync(Guid id, CancellationToken token) => Task.FromResult(Items.SingleOrDefault(x => x.OfferId == id));
        public Task<IReadOnlyList<SellerOffer>> ListBySellerAsync(Guid id, CancellationToken token) => Task.FromResult<IReadOnlyList<SellerOffer>>(Items.Where(x => x.SellerPartyId == id).ToList());
        public Task AddAsync(SellerOffer offer, CancellationToken token) { Items.Add(offer); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken token) => Task.CompletedTask;
        public Task<bool> ExistsActiveListingAsync(Guid sellerId, Guid variantId, Tooba.Offer.Domain.ValueObjects.SalesChannel channel, CancellationToken token) => Task.FromResult(false);
        public Task<bool> ExistsSellerSkuAsync(Guid sellerId, string sku, Guid? excludingId, CancellationToken token) => Task.FromResult(false);
    }

    private sealed class FakeCatalog(CatalogVariantLookupResult? result) : ICatalogVariantLookup
    { public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid id, CancellationToken token) => Task.FromResult(result); }
    private sealed class FakeParty(PartyLookupResult? result) : IPartyLookup
    { public Task<PartyLookupResult?> FindByIdAsync(Guid id, CancellationToken token) => Task.FromResult(result); }
    private sealed class FakeCatalogReads : ICatalogOfferReadGateway
    {
        public Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken token) =>
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
        public Task SetInventoryAsync(SetSellerOfferInventory request, CancellationToken token) => Task.CompletedTask;
    }
    private sealed class FixedIds(Guid id) : IIdGenerator
    { public Guid NewId() => id; }
}
