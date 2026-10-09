#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Development;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Party.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Tax.Contracts.Ports;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>
/// Catalog-owned Development enricher: publish + Offer/Pricing/Inventory/Tax/Party for the schema demo product.
/// Cross-module calls use only narrow module Contracts; no foreign Application/Infrastructure/Domain/Persistence.
/// </summary>
public sealed class CatalogAttributeSchemaSellableEnricher(
    CatalogDbContext catalogDb,
    ICatalogDirectory catalog,
    IOfferDevelopmentSeedGateway offerSeeds,
    IOfferQueryGateway offerQueries,
    IPartyDevelopmentSeedGateway parties,
    IPricingDevelopmentSeedGateway prices,
    IInventoryDevelopmentSeedGateway inventory,
    ITaxDevelopmentSeedGateway tax) : ICatalogAttributeSchemaSellableEnricher
{
    private const string DemoSellerSkuPrefix = "SCHEMA-PHONE";

    /// <inheritdoc />
    public async Task EnsurePublishedAndSellableAsync(CancellationToken cancellationToken = default)
    {
        var product = await catalogDb.Products.SingleAsync(
            p => p.SlugSeam == CatalogAttributeSchemaDevelopmentSeed.DemoProductSlug,
            cancellationToken);
        var categoryIds = await catalogDb.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
        foreach (var categoryId in categoryIds)
        {
            await catalog.PublishCategoryAsync(categoryId, cancellationToken);
        }

        if (product.Status != CatalogPublicationStatus.Published)
        {
            var parentById = await catalogDb.Categories.AsNoTracking()
                .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
            var assignable = categoryIds.Count > 0
                && categoryIds.All(id => CatalogCategoryTreeRules.IsAssignableProductCategory(id, parentById));
            if (assignable)
            {
                if (!await catalogDb.MediaReferences.AsNoTracking()
                        .AnyAsync(m => m.ProductId == product.ProductId, cancellationToken))
                {
                    await catalog.AttachMediaReferenceAsync(
                        product.ProductId,
                        Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                        CancellationToken.None);
                }

                await ProductPublishPrep.EnsureMinimalSeoForPublishAsync(
                    catalog, product.ProductId, "توضیح سئو گوشی نمونه schema", CancellationToken.None);
                await catalog.PublishProductAsync(product.ProductId, CancellationToken.None);
            }
        }

        var variants = await catalogDb.Variants.AsNoTracking()
            .Where(v => v.ProductId == product.ProductId)
            .OrderBy(v => v.CatalogCodeSeam)
            .ToListAsync(cancellationToken);
        if (variants.Count == 0)
        {
            return;
        }

        var sellerPartyId = await parties.ResolveDevelopmentSellerPartyAsync(
            "فروشنده schema موبایل",
            "Schema Mobile Seller Legal",
            cancellationToken);

        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var amount = 12_500_000m;
        var location = await inventory.EnsureDevelopmentLocationAsync(
            "WH-SCHEMA-MOBILE",
            "انبار schema موبایل",
            cancellationToken);
        if (location.IsFailure)
        {
            throw new InvalidOperationException(location.FirstError.Code);
        }

        var locationId = location.Value;
        foreach (var variant in variants)
        {
            var sku = $"{DemoSellerSkuPrefix}-{variant.CatalogCodeSeam ?? variant.VariantId.ToString("N")[..8]}";
            if (await offerQueries.ExistsBySellerSkuAsync(sellerPartyId, sku, cancellationToken))
            {
                continue;
            }

            var offerId = await offerSeeds.EnsureActiveSellerOfferAsync(
                variant.VariantId,
                sellerPartyId,
                sku,
                cancellationToken);

            var price = await prices.EnsureDevelopmentBasePriceAsync(
                new SetDevelopmentBasePrice(
                    offerId,
                    "IR",
                    SalesChannel.Marketplace,
                    amount,
                    "IRR",
                    start),
                cancellationToken);
            if (price.IsFailure)
            {
                throw new InvalidOperationException(price.FirstError.Code);
            }

            var taxCode = $"sch-{offerId:N}"[..20];
            var classified = await tax.EnsureDevelopmentOfferCategoryAsync(
                new EnsureDevelopmentOfferCategory(offerId, taxCode, "schema phone"),
                cancellationToken);
            if (classified.IsFailure)
            {
                throw new InvalidOperationException(classified.FirstError.Code);
            }

            var stock = await inventory.IncreaseDevelopmentStockAsync(
                new SeedDevelopmentStock(offerId, locationId, 5, "schema-seed"),
                cancellationToken);
            if (stock.IsFailure)
            {
                throw new InvalidOperationException(stock.FirstError.Code);
            }

            amount += 500_000m;
        }
    }
}

