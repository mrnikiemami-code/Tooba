using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Offer.Infrastructure.Adapters.Tracing;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Commands.CreateOffer;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Party.Contracts;
using Tooba.Pricing.Contracts;
using Xunit;
using DomainChannel = Tooba.Offer.Domain.ValueObjects.SalesChannel;

namespace Tooba.Offer.Tests.Observability;

[CollectionDefinition(nameof(OfferTraceTopologyCollection), DisableParallelization = true)]
public sealed class OfferTraceTopologyCollection;

[Collection(nameof(OfferTraceTopologyCollection))]
public sealed class OfferTraceTopologyTests
{
    [Fact]
    public async Task List_read_path_emits_catalog_pricing_inventory_module_spans()
    {
        var activities = new List<Activity>();
        using var listener = CreateListener(activities);
        using var correlationScope = CorrelationIdContext.BeginScope(Guid.NewGuid().ToString("N"));

        var services = new ServiceCollection();
        services.AddSingleton<IModuleCallTracer, ModuleCallTracer>();
        services.AddSingleton<ICatalogOfferReadGateway, FakeCatalogReads>();
        services.AddSingleton<IPriceLookupGateway, FakePrices>();
        services.AddSingleton<ISellerOfferInventoryGateway, FakeInventory>();
        services.AddSingleton<IPartyLookup, FakeParty>();
        services.AddSingleton<IReturnPolicyResolver>(_ => new ReturnPolicyResolver(new ReturnPolicyOptions()));
        services.AddSingleton<IClock>(_ => new FixedClock(DateTimeOffset.Parse("2026-01-01T00:00:00Z")));
        services.AddOfferModuleCallTracing();
        services.AddSingleton<OfferReadModelComposer>();
        var sp = services.BuildServiceProvider();
        var composer = sp.GetRequiredService<OfferReadModelComposer>();

        var offer = SellerOffer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DomainChannel.Marketplace, null, DateTimeOffset.UtcNow);
        await composer.ListAsync([offer], CancellationToken.None);

        Assert.Contains(activities, a => HasModuleCall(a, "Offer", "Catalog"));
        Assert.Contains(activities, a => HasModuleCall(a, "Offer", "Pricing"));
        Assert.Contains(activities, a => HasModuleCall(a, "Offer", "Inventory"));
        Assert.Equal(1, activities.Count(a => HasModuleCall(a, "Offer", "Catalog")));
        Assert.Equal(1, activities.Count(a => HasModuleCall(a, "Offer", "Pricing")));
        Assert.Equal(1, activities.Count(a => HasModuleCall(a, "Offer", "Inventory")));
    }

    [Fact]
    public async Task Create_path_emits_catalog_and_party_module_spans()
    {
        var activities = new List<Activity>();
        using var listener = CreateListener(activities);
        using var correlationScope = CorrelationIdContext.BeginScope(Guid.NewGuid().ToString("N"));

        var variantId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IModuleCallTracer, ModuleCallTracer>();
        services.AddSingleton<ICatalogVariantLookup>(_ => new FakeCatalogVariant(variantId));
        services.AddSingleton<IPartyLookup>(_ => new FakePartyOrg(sellerId));
        services.AddSingleton<ICatalogOfferReadGateway, FakeCatalogReads>();
        services.AddSingleton<IPriceLookupGateway, FakePrices>();
        services.AddSingleton<ISellerOfferInventoryGateway, FakeInventory>();
        services.AddSingleton<IReturnPolicyResolver>(_ => new ReturnPolicyResolver(new ReturnPolicyOptions()));
        services.AddSingleton<IClock>(_ => new FixedClock(DateTimeOffset.Parse("2026-01-01T00:00:00Z")));
        services.AddSingleton<IIdGenerator>(_ => new FixedIds(Guid.Parse("11111111-1111-4111-8111-111111111111")));
        services.AddSingleton<IOfferStore, MemoryOfferStore>();
        services.AddOfferModuleCallTracing();
        services.AddSingleton<OfferReadModelComposer>();
        services.AddToobaCqrsFoundation(typeof(CreateOfferCommand).Assembly);
        var sp = services.BuildServiceProvider();
        var sender = sp.GetRequiredService<MediatR.ISender>();

        await sender.Send(new CreateOfferCommand(variantId, sellerId, SalesChannel.Marketplace, "SKU-1"));

        Assert.Contains(activities, a => a.OperationName.Contains("CreateOfferCommand", StringComparison.Ordinal));
        Assert.Contains(activities, a => HasModuleCall(a, "Offer", "Catalog"));
        Assert.Contains(activities, a => HasModuleCall(a, "Offer", "Party"));
    }

    private static bool HasModuleCall(Activity activity, string source, string target) =>
        activity.GetTagItem(TracingTagNames.ModuleSource)?.ToString() == source
        && activity.GetTagItem(TracingTagNames.ModuleTarget)?.ToString() == target
        && activity.GetTagItem(TracingTagNames.RequestKind)?.ToString() == TracingRequestKinds.ModuleCall;

    private static ActivityListener CreateListener(List<Activity> sink)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ToobaTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => sink.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private sealed class FakeCatalogReads : ICatalogOfferReadGateway
    {
        public Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, CatalogOfferPresentation>>(
                catalogVariantIds.ToDictionary(
                    id => id,
                    id => new CatalogOfferPresentation(id, Guid.NewGuid(), "p", null, null, null, null)));
    }

    private sealed class FakePrices : IPriceLookupGateway
    {
        public Task<PriceQuote?> ResolvePriceAsync(PriceResolutionQuery query, CancellationToken cancellationToken)
            => Task.FromResult<PriceQuote?>(null);

        public Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolvePricesBatchAsync(
            IReadOnlyCollection<Guid> offerIds, string market, SalesChannel channel, string currency, DateTimeOffset at,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, PriceQuote>>(new Dictionary<Guid, PriceQuote>());

        public Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolveCampaignPricesBatchAsync(
            IReadOnlyCollection<Guid> offerIds, Guid campaignId, string market, SalesChannel channel, string currency,
            DateTimeOffset at, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, PriceQuote>>(new Dictionary<Guid, PriceQuote>());
    }

    private sealed class FakeInventory : ISellerOfferInventoryGateway
    {
        public Task<IReadOnlyDictionary<Guid, OfferInventorySummary>> GetAvailabilityAsync(
            IReadOnlyCollection<Guid> offerIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, OfferInventorySummary>>(
                offerIds.ToDictionary(id => id, id => new OfferInventorySummary(id, 0, 0, 0)));

        public Task<Result> SetInventoryAsync(SetSellerOfferInventory request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());
    }

    private sealed class FakeParty : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken)
            => Task.FromResult<PartyLookupResult?>(new PartyLookupResult(partyId, "Organization", "Seller"));
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                partyIds.ToDictionary(id => id, id => "Seller"));
        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
            string term, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class FakePartyOrg(Guid id) : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken)
            => Task.FromResult<PartyLookupResult?>(
                partyId == id ? new PartyLookupResult(id, "Organization", "Seller") : null);
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                partyIds.Where(x => x == id).ToDictionary(x => x, _ => "Seller"));
        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
            string term, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class FakeCatalogVariant(Guid id) : ICatalogVariantLookup
    {
        public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken)
            => Task.FromResult<CatalogVariantLookupResult?>(
                variantId == id ? new CatalogVariantLookupResult(id, Guid.NewGuid()) : null);
        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(
                variantIds.ToDictionary(x => x, _ => (Guid?)null));
        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                variantIds.ToDictionary(x => x, _ => "Product"));
    }

    private sealed class FixedIds(Guid id) : IIdGenerator
    {
        public Guid NewId() => id;
    }

    private sealed class MemoryOfferStore : IOfferStore
    {
        private readonly List<SellerOffer> _offers = [];

        public Task AddAsync(SellerOffer offer, CancellationToken cancellationToken)
        {
            _offers.Add(offer);
            return Task.CompletedTask;
        }

        public Task<SellerOffer?> GetByIdAsync(Guid offerId, CancellationToken cancellationToken)
            => Task.FromResult(_offers.FirstOrDefault(x => x.OfferId == offerId));

        public Task<bool> ExistsActiveListingAsync(Guid sellerPartyId, Guid catalogVariantId, DomainChannel channel, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<bool> ExistsSellerSkuAsync(Guid sellerPartyId, string sellerSku, Guid? excludingOfferId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<SellerOffer>> ListBySellerAsync(Guid sellerPartyId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SellerOffer>>(_offers.Where(x => x.SellerPartyId == sellerPartyId).ToList());
    }
}
