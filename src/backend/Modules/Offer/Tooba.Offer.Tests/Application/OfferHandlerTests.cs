using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
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
    {
        public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid id, CancellationToken token) => Task.FromResult(result);
        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken token) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(new Dictionary<Guid, Guid?>());
        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken token) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
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
        public Task<Result> SetInventoryAsync(SetSellerOfferInventory request, CancellationToken token) => Task.FromResult(Result.Success());
    }
    private sealed class FixedIds(Guid id) : IIdGenerator
    { public Guid NewId() => id; }
}
