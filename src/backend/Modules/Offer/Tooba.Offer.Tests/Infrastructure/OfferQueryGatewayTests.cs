using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.ValueObjects;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Offer.Infrastructure.Persistence;
using Xunit;
using ContractStatus = Tooba.Offer.Contracts.Dtos.OfferStatus;
using DomainChannel = Tooba.Offer.Domain.ValueObjects.SalesChannel;
using DomainStatus = Tooba.Offer.Domain.ValueObjects.OfferStatus;

namespace Tooba.Offer.Tests.Infrastructure;

public sealed class OfferQueryGatewayTests
{
    [Fact]
    public async Task Query_gateway_covers_active_counts_sellers_variants_and_batch_refs()
    {
        await using var db = CreateDb();
        var sellerA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
        var sellerB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
        var variant1 = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var variant2 = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var now = DateTimeOffset.Parse("2026-09-21T00:00:00Z");

        var activeA = SellerOffer.Create(Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"), variant1, sellerA, DomainChannel.Marketplace, "A-1", now);
        activeA.Activate(now);
        var draftA = SellerOffer.Create(Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3"), variant1, sellerA, DomainChannel.Marketplace, "A-2", now);
        var activeB = SellerOffer.Create(Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2"), variant2, sellerB, DomainChannel.Marketplace, "B-1", now);
        activeB.Activate(now);
        var archived = SellerOffer.Create(Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc1"), variant2, sellerB, DomainChannel.Marketplace, "B-2", now);
        archived.Activate(now);
        archived.Archive(now.AddMinutes(1));

        db.Offers.AddRange(activeA, draftA, activeB, archived);
        await db.SaveChangesAsync();

        IOfferQueryGateway gateway = new OfferStore(db);

        Assert.Equal(2, await gateway.CountActiveOffersAsync(CancellationToken.None));
        var sellers = await gateway.ListDistinctSellerPartyIdsAsync(CancellationToken.None);
        Assert.Equal(2, sellers.Count);
        Assert.Contains(sellerA, sellers);
        Assert.Contains(sellerB, sellers);

        var bySeller = await gateway.CountActiveOffersBySellerAsync(CancellationToken.None);
        Assert.Equal(1, bySeller[sellerA]);
        Assert.Equal(1, bySeller[sellerB]);

        var statusRows = await gateway.ListSellerStatusRowsAsync(CancellationToken.None);
        Assert.Equal(4, statusRows.Count);
        Assert.Equal(1, statusRows.Count(x => x.SellerPartyId == sellerA && x.Status == ContractStatus.Active));

        var nonArchivedCounts = await gateway.CountOffersByCatalogVariantIdsAsync([variant1, variant2], CancellationToken.None);
        Assert.Equal(2, nonArchivedCounts[variant1]);
        Assert.Equal(1, nonArchivedCounts[variant2]);

        var allCounts = await gateway.CountAllOffersGroupedByCatalogVariantAsync(CancellationToken.None);
        Assert.Equal(2, allCounts[variant1]);
        Assert.Equal(2, allCounts[variant2]);

        var map = await gateway.MapAllOfferIdsToCatalogVariantIdsAsync(CancellationToken.None);
        Assert.Equal(4, map.Count);
        Assert.Equal(variant1, map[activeA.OfferId]);

        var byVariant = await gateway.ListOffersByCatalogVariantIdsAsync([variant1], CancellationToken.None);
        Assert.Equal(2, byVariant.Count);
        var activeByVariant = await gateway.ListActiveOffersByCatalogVariantIdsAsync([variant1, variant2], CancellationToken.None);
        Assert.Equal(2, activeByVariant.Count);
        Assert.All(activeByVariant, x => Assert.Equal(ContractStatus.Active, x.Status));

        Assert.True(await gateway.AnyOffersForCatalogVariantIdsAsync([variant1], CancellationToken.None));
        Assert.False(await gateway.AnyOffersForCatalogVariantIdsAsync([Guid.NewGuid()], CancellationToken.None));

        var batch = await gateway.FindOffersBatchAsync([activeA.OfferId, activeB.OfferId], CancellationToken.None);
        Assert.Equal(2, batch.Count);
        Assert.Equal(sellerA, batch[activeA.OfferId].SellerPartyId);

        var recent = await gateway.ListRecentActiveOffersAsync(10, CancellationToken.None);
        Assert.Equal(2, recent.Count);
        Assert.All(recent, x => Assert.Equal(ContractStatus.Active, x.Status));

        var latest = await gateway.FindLatestBySellerAndVariantAsync(sellerA, variant1, CancellationToken.None);
        Assert.NotNull(latest);
        Assert.True(await gateway.ExistsBySellerSkuAsync(sellerA, "A-1", CancellationToken.None));
        Assert.False(await gateway.ExistsBySellerSkuAsync(sellerA, "missing", CancellationToken.None));
        Assert.Equal(activeA.OfferId, (await gateway.FindBySellerSkuAsync("A-1", CancellationToken.None))!.OfferId);
        var prefixed = await gateway.ListOfferIdsBySellerSkuPrefixAsync("A-", CancellationToken.None);
        Assert.Equal(2, prefixed.Count);
    }

    [Fact]
    public async Task Development_seed_gateway_clones_active_template()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.Parse("2026-09-21T00:00:00Z");
        var template = SellerOffer.Create(
            Guid.Parse("dddddddd-dddd-4ddd-8ddd-ddddddddddd1"),
            Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1"),
            Guid.Parse("ffffffff-ffff-4fff-8fff-fffffffffff1"),
            DomainChannel.Marketplace,
            "TEMPLATE",
            now);
        template.Activate(now);
        db.Offers.Add(template);
        await db.SaveChangesAsync();

        var seed = new OfferDevelopmentSeedGateway(db, new FixedIds(Guid.Parse("99999999-9999-4999-8999-999999999999")));
        var seller = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa9");
        var created = await seed.EnsureActiveCloneFromAnyActiveAsync("OOS-SKU", seller, CancellationToken.None);
        Assert.NotNull(created);
        var row = await db.Offers.SingleAsync(x => x.OfferId == created);
        Assert.Equal(DomainStatus.Active, row.Status);
        Assert.Equal(template.CatalogVariantId, row.CatalogVariantId);
        Assert.Equal(seller, row.SellerPartyId);
    }

    private static OfferDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>()
            .UseInMemoryDatabase("offer-query-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new OfferDbContext(options);
    }

    private sealed class FixedIds(Guid id) : IIdGenerator
    {
        public Guid NewId() => id;
    }
}
