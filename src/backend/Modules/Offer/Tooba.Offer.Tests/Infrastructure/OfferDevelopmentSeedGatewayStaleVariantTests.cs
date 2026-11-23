using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.ValueObjects;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Offer.Infrastructure.Persistence;
using Xunit;
using DomainStatus = Tooba.Offer.Domain.ValueObjects.OfferStatus;

namespace Tooba.Offer.Tests.Infrastructure;

/// <summary>
/// TB-288: a surviving Development seed Offer that only matches by SellerSku but points at a Catalog
/// variant deleted by a Catalog demo reset must NOT be reused; it must be rebuilt on a live variant.
/// </summary>
public sealed class OfferDevelopmentSeedGatewayStaleVariantTests
{
    private static readonly Guid LiveVariant = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid StaleVariant = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid SellerA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid SellerB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private const string OosSku = "DEV-SEED-OOS";
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-23T00:00:00Z");

    [Fact]
    public async Task Stale_variant_offer_is_rebuilt_on_a_live_variant_and_stays_idempotent()
    {
        await using var db = CreateDb();
        var template = SellerOffer.Create(Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2"), LiveVariant, SellerB, SalesChannel.Marketplace, "LIVE-TEMPLATE", Now);
        Assert.True(template.Activate(Now).IsSuccess);
        var stale = SellerOffer.Create(Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc1"), StaleVariant, SellerA, SalesChannel.Marketplace, OosSku, Now);
        Assert.True(stale.Activate(Now).IsSuccess);
        db.Offers.AddRange(template, stale);
        await db.SaveChangesAsync();
        var staleId = stale.OfferId;

        var gateway = new OfferDevelopmentSeedGateway(db, new SeqIds(), new FixedClock(Now), new FakeLookup(LiveVariant));

        var rebuiltId = await gateway.EnsureActiveCloneFromAnyActiveAsync(OosSku, SellerA, CancellationToken.None);

        Assert.NotNull(rebuiltId);
        Assert.NotEqual(staleId, rebuiltId);
        var oosRows = await db.Offers.Where(x => x.SellerSku == OosSku).ToListAsync();
        Assert.Single(oosRows);
        Assert.Equal(LiveVariant, oosRows[0].CatalogVariantId);
        Assert.Equal(DomainStatus.Active, oosRows[0].Status);
        Assert.Null(await db.Offers.SingleOrDefaultAsync(x => x.OfferId == staleId));

        // Idempotent: a second seed run reuses the rebuilt live-variant row (no new row / duplicate).
        var secondId = await gateway.EnsureActiveCloneFromAnyActiveAsync(OosSku, SellerA, CancellationToken.None);
        Assert.Equal(rebuiltId, secondId);
        Assert.Single(await db.Offers.Where(x => x.SellerSku == OosSku).ToListAsync());
    }

    [Fact]
    public async Task Valid_variant_offer_is_reused_without_rebuild()
    {
        await using var db = CreateDb();
        var valid = SellerOffer.Create(Guid.Parse("dddddddd-dddd-4ddd-8ddd-ddddddddddd1"), LiveVariant, SellerA, SalesChannel.Marketplace, OosSku, Now);
        Assert.True(valid.Activate(Now).IsSuccess);
        db.Offers.Add(valid);
        await db.SaveChangesAsync();
        var validId = valid.OfferId;

        var gateway = new OfferDevelopmentSeedGateway(db, new SeqIds(), new FixedClock(Now), new FakeLookup(LiveVariant));

        var resultId = await gateway.EnsureActiveCloneFromAnyActiveAsync(OosSku, SellerA, CancellationToken.None);

        Assert.Equal(validId, resultId);
        Assert.Single(await db.Offers.Where(x => x.SellerSku == OosSku).ToListAsync());
    }

    private static OfferDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>()
            .UseInMemoryDatabase("offer-seed-288-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new OfferDbContext(options);
    }

    private sealed class SeqIds : IIdGenerator
    {
        public Guid NewId() => Guid.NewGuid();
    }

    private sealed class FakeLookup(params Guid[] liveVariants) : ICatalogVariantLookup
    {
        private readonly HashSet<Guid> _live = [.. liveVariants];

        public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken) =>
            Task.FromResult(_live.Contains(variantId)
                ? new CatalogVariantLookupResult(variantId, Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1"))
                : null);

        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(new Dictionary<Guid, Guid?>());

        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>(_live.ToList());
    }
}
