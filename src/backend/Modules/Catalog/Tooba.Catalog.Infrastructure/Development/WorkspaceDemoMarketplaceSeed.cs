#pragma warning disable CS1591
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Party.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Tax.Contracts;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>
/// Development seed data for the two-seller marketplace demo of the live workspace product.
/// Catalog-owned; every foreign module interaction goes through that module's narrow
/// Development Contracts gateway — no foreign Application/Infrastructure/Domain/Persistence.
/// </summary>
public sealed class WorkspaceDemoMarketplaceSeed(
    IPartyDevelopmentSeedGateway parties,
    IOfferDevelopmentSeedGateway offers,
    IPricingDevelopmentSeedGateway prices,
    IInventoryDevelopmentSeedGateway inventory,
    ITaxDevelopmentSeedGateway tax)
{
    /// <summary>Display name of the first demo seller organization.</summary>
    public const string SellerADisplayName = "فروشگاه آرمان";

    /// <summary>Legal name of the first demo seller organization.</summary>
    public const string SellerALegalName = "Arman Store Legal";

    /// <summary>Display name of the second demo seller organization.</summary>
    public const string SellerBDisplayName = "دیجی‌استایل نمونه";

    /// <summary>Legal name of the second demo seller organization.</summary>
    public const string SellerBLegalName = "Digistyle Sample Legal";

    /// <summary>Seller SKU of the first demo offer.</summary>
    public const string SellerASku = "ARM-LN-01";

    /// <summary>Seller SKU of the second demo offer.</summary>
    public const string SellerBSku = "DGS-LN-01";

    /// <summary>Legacy display name of the first seller that the copy refresh rewrites.</summary>
    public const string LegacySellerADisplayName = "فروشنده الف";

    /// <summary>Legacy English display name of the first seller that the copy refresh rewrites.</summary>
    public const string LegacySellerADisplayNameEn = "Seller A";

    /// <summary>Legacy display name of the second seller that the copy refresh rewrites.</summary>
    public const string LegacySellerBDisplayName = "فروشنده ب";

    /// <summary>Legacy English display name of the second seller that the copy refresh rewrites.</summary>
    public const string LegacySellerBDisplayNameEn = "Seller B";

    /// <summary>Base price of the first demo offer, in IRR minor units.</summary>
    public const decimal SellerAPrice = 1_850_000m;

    /// <summary>Base price of the second demo offer, in IRR minor units.</summary>
    public const decimal SellerBPrice = 1_790_000m;

    /// <summary>Demo price market.</summary>
    public const string PriceMarket = "IR";

    /// <summary>Demo price currency.</summary>
    public const string PriceCurrency = "IRR";

    /// <summary>Tax category code assigned to both demo offers.</summary>
    public const string TaxCategoryCode = "standard";

    /// <summary>Tax category display name.</summary>
    public const string TaxCategoryDisplayName = "استاندارد";

    /// <summary>Tax rule jurisdiction of the demo rule.</summary>
    public const string TaxJurisdiction = "IR-NAT";

    /// <summary>Tax rule market of the demo rule.</summary>
    public const string TaxMarket = "IR";

    /// <summary>Tax rule rate of the demo rule.</summary>
    public const decimal TaxRate = 0.09m;

    /// <summary>Tax rule specificity of the demo rule.</summary>
    public const int TaxSpecificity = 10;

    /// <summary>Code of the central Tehran demo warehouse.</summary>
    public const string LocationTehranCode = "WH-THR";

    /// <summary>Name of the central Tehran demo warehouse.</summary>
    public const string LocationTehranName = "انبار مرکزی تهران";

    /// <summary>Code of the Isfahan demo warehouse.</summary>
    public const string LocationIsfahanCode = "WH-ISF";

    /// <summary>Name of the Isfahan demo warehouse.</summary>
    public const string LocationIsfahanName = "انبار اصفهان";

    /// <summary>Code of the Kashan demo warehouse.</summary>
    public const string LocationKashanCode = "WH-KSH";

    /// <summary>Name of the Kashan demo warehouse.</summary>
    public const string LocationKashanName = "انبار کاشان";

    /// <summary>Stock increase reason recorded by the demo seed.</summary>
    public const string StockReason = "seed-receipt";

    /// <summary>External reference and idempotency key of the demo stock hold.</summary>
    public const string HoldReference = "workspace-live-hold";

    /// <summary>Number of units held by the demo stock hold.</summary>
    public const decimal HoldQuantity = 3m;

    /// <summary>Creates the two-seller marketplace demo around the given live demo variant. Idempotent.</summary>
    public async Task EnsureMarketplaceAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        var sellerAPartyId = await parties.EnsureDevelopmentOrganizationAsync(
            SellerADisplayName,
            SellerALegalName,
            cancellationToken);
        var sellerBPartyId = await parties.EnsureDevelopmentOrganizationAsync(
            SellerBDisplayName,
            SellerBLegalName,
            cancellationToken);

        var offerA = await offers.EnsureActiveSellerOfferAsync(
            variantId,
            sellerAPartyId,
            SellerASku,
            cancellationToken);
        var offerB = await offers.EnsureActiveSellerOfferAsync(
            variantId,
            sellerBPartyId,
            SellerBSku,
            cancellationToken);

        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var priceA = await prices.EnsureDevelopmentBasePriceAsync(
            new SetDevelopmentBasePrice(offerA, PriceMarket, SalesChannel.Marketplace, SellerAPrice, PriceCurrency, start),
            cancellationToken);
        if (priceA.IsFailure)
        {
            throw new InvalidOperationException(priceA.FirstError.Code);
        }

        var priceB = await prices.EnsureDevelopmentBasePriceAsync(
            new SetDevelopmentBasePrice(offerB, PriceMarket, SalesChannel.Marketplace, SellerBPrice, PriceCurrency, start),
            cancellationToken);
        if (priceB.IsFailure)
        {
            throw new InvalidOperationException(priceB.FirstError.Code);
        }

        var classifiedA = await tax.EnsureDevelopmentOfferCategoryAsync(
            new EnsureDevelopmentOfferCategory(offerA, TaxCategoryCode, TaxCategoryDisplayName),
            cancellationToken);
        if (classifiedA.IsFailure)
        {
            throw new InvalidOperationException(classifiedA.FirstError.Code);
        }

        var classifiedB = await tax.EnsureDevelopmentOfferCategoryAsync(
            new EnsureDevelopmentOfferCategory(offerB, TaxCategoryCode, TaxCategoryDisplayName),
            cancellationToken);
        if (classifiedB.IsFailure)
        {
            throw new InvalidOperationException(classifiedB.FirstError.Code);
        }

        var rule = await tax.EnsureDevelopmentRuleAsync(
            new EnsureDevelopmentTaxRule(
                TaxJurisdiction,
                TaxMarket,
                TaxCategoryCode,
                DevelopmentTaxRuleKind.Percentage,
                TaxRate,
                start,
                TaxSpecificity,
                DevelopmentTaxOverridePolicy.Disabled),
            cancellationToken);
        if (rule.IsFailure)
        {
            throw new InvalidOperationException(rule.FirstError.Code);
        }

        await EnsureLocationAsync(LocationTehranCode, LocationTehranName, cancellationToken);
        await EnsureLocationAsync(LocationIsfahanCode, LocationIsfahanName, cancellationToken);
        await EnsureLocationAsync(LocationKashanCode, LocationKashanName, cancellationToken);

        await IncreaseStockAsync(offerA, LocationTehranCode, 12m, cancellationToken);
        await IncreaseStockAsync(offerA, LocationIsfahanCode, 7m, cancellationToken);
        await IncreaseStockAsync(offerB, LocationKashanCode, 4m, cancellationToken);

        var hold = await inventory.ReserveDevelopmentHoldAsync(
            new SeedDevelopmentStockHold(
                offerA,
                LocationTehranCode,
                HoldQuantity,
                HoldReference,
                HoldReference),
            cancellationToken);
        if (hold.IsFailure)
        {
            throw new InvalidOperationException(hold.FirstError.Code);
        }
    }

    /// <summary>
    /// Rewrites legacy operator-facing seller display names. Accessible when the live demo
    /// product already exists; idempotent and no-ops when the legacy names are absent.
    /// </summary>
    public Task RefreshOperatorFacingCopyAsync(CancellationToken cancellationToken = default) =>
        parties.EnsureDevelopmentOrganizationDisplayNamesAsync(
            [
                new DevelopmentOrganizationRename(LegacySellerADisplayName, SellerADisplayName),
                new DevelopmentOrganizationRename(LegacySellerADisplayNameEn, SellerADisplayName),
                new DevelopmentOrganizationRename(LegacySellerBDisplayName, SellerBDisplayName),
                new DevelopmentOrganizationRename(LegacySellerBDisplayNameEn, SellerBDisplayName),
            ],
            cancellationToken);

    private async Task EnsureLocationAsync(string code, string name, CancellationToken cancellationToken)
    {
        var location = await inventory.EnsureDevelopmentLocationAsync(code, name, cancellationToken);
        if (location.IsFailure)
        {
            throw new InvalidOperationException(location.FirstError.Code);
        }
    }

    private async Task IncreaseStockAsync(
        Guid offerId,
        string locationCode,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var location = await inventory.EnsureDevelopmentLocationAsync(locationCode, locationCode, cancellationToken);
        if (location.IsFailure)
        {
            throw new InvalidOperationException(location.FirstError.Code);
        }

        var stock = await inventory.IncreaseDevelopmentStockAsync(
            new SeedDevelopmentStock(offerId, location.Value, quantity, StockReason),
            cancellationToken);
        if (stock.IsFailure)
        {
            throw new InvalidOperationException(stock.FirstError.Code);
        }
    }
}
