#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Development;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Pricing.Application;
using Tooba.Tax.Application;

namespace Tooba.Host.Development;

/// <summary>
/// Host-owned cross-module enricher: publish + Offer/Pricing/Inventory/Tax/Party for schema demo product.
/// </summary>
public sealed class CatalogAttributeSchemaSellableEnricher : ICatalogAttributeSchemaSellableEnricher
{
    private const string DemoSellerSkuPrefix = "SCHEMA-PHONE";

    private readonly IServiceProvider _provider;

    public CatalogAttributeSchemaSellableEnricher(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public async Task EnsurePublishedAndSellableAsync(CancellationToken cancellationToken = default)
    {
        var catalogDb = _provider.GetRequiredService<CatalogDbContext>();
        var catalog = _provider.GetRequiredService<ICatalogDirectory>();
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

        var offers = _provider.GetRequiredService<MediatR.ISender>();
        var offerQueries = _provider.GetRequiredService<IOfferQueryGateway>();
        var parties = _provider.GetRequiredService<IPartyDirectory>();
        var partyDb = _provider.GetRequiredService<PartyDbContext>();
        var prices = _provider.GetRequiredService<IPriceDirectory>();
        var tax = _provider.GetRequiredService<ITaxDirectory>();
        var inventory = _provider.GetRequiredService<IInventoryDirectory>();
        var inventoryQuery = _provider.GetRequiredService<IInventoryQueryGateway>();

        var seller = await partyDb.Parties.AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        Guid sellerPartyId;
        if (seller is null)
        {
            var created = await parties.CreateOrganizationAsync(
                "فروشنده schema موبایل",
                "Schema Mobile Seller Legal",
                cancellationToken);
            sellerPartyId = created.PartyId;
        }
        else
        {
            sellerPartyId = seller.PartyId;
        }

        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var amount = 12_500_000m;
        var locationCode = "WH-SCHEMA-MOBILE";
        var location = await inventoryQuery.FindLocationByCodeAsync(locationCode, cancellationToken);
        var locationId = location?.LocationId
            ?? await inventory.CreateLocationAsync(locationCode, "انبار schema موبایل", cancellationToken);

        foreach (var variant in variants)
        {
            var sku = $"{DemoSellerSkuPrefix}-{variant.CatalogCodeSeam ?? variant.VariantId.ToString("N")[..8]}";
            if (await offerQueries.ExistsBySellerSkuAsync(sellerPartyId, sku, cancellationToken))
            {
                continue;
            }

            var created = await offers.Send(new Tooba.Offer.Application.Commands.CreateOffer.CreateOfferCommand(
                variant.VariantId, sellerPartyId, SalesChannel.Marketplace, sku), cancellationToken);
            if (created.IsFailure)
                throw new InvalidOperationException(created.FirstError.Code);
            var offer = created.Value;
            var activated = await offers.Send(new Tooba.Offer.Application.Commands.ActivateOffer.ActivateOfferCommand(
                offer.OfferId, sellerPartyId), cancellationToken);
            if (activated.IsFailure)
                throw new InvalidOperationException(activated.FirstError.Code);
            var price = await prices.CreatePriceAsync(
                offer.OfferId,
                "IR",
                SalesChannel.Marketplace,
                amount,
                "IRR",
                start,
                null,
                cancellationToken);
            await prices.ActivateAsync(price.PriceId, cancellationToken);

            var taxCode = $"sch-{offer.OfferId:N}"[..20];
            var taxCategory = await tax.CreateCategoryAsync(taxCode, "schema phone", cancellationToken);
            await tax.AssignOfferCategoryAsync(offer.OfferId, taxCategory.CategoryId, cancellationToken);

            var stock = await inventory.OpenPositionAsync(offer.OfferId, locationId, cancellationToken);
            await inventory.AdjustAsync(stock, StockAdjustmentKind.Increase, 5, "schema-seed", null, cancellationToken);
            amount += 500_000m;
        }
    }
}