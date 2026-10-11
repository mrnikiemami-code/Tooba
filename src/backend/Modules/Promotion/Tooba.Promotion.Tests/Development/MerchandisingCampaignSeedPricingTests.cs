using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Application.Offers.Policies;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Promotion.Infrastructure.Development;
using Xunit;

namespace Tooba.Promotion.Tests.Development;

/// <summary>
/// رگرسیون قطعی برای دو دفکت دانهٔ کمپین AMAZING:
/// (۱) انتخاب Primary باید همان حکم فروشگاه باشد، نه کمترین OfferId؛
/// (۲) پوشش پنجرهٔ قیمت کمپین باید کامل باشد و از معناشناسی نیم‌باز Pricing پیروی کند.
/// </summary>
public sealed class MerchandisingCampaignSeedPricingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Campaign_price_targets_the_storefront_primary_offer_not_the_lowest_offer_id()
    {
        var product = Guid.NewGuid();
        var variant = Guid.NewGuid();
        var lowestId = Guid.Parse("00000001-0000-0000-0000-000000000001");
        var primaryId = Guid.Parse("00000002-0000-0000-0000-000000000002");

        // کمترین OfferId موجودی ندارد و گران‌تر است؛ فروشگاه ارزان‌ترین Offer دارای موجودی را می‌گیرد.
        var offers = new[]
        {
            Offer(lowestId, variant),
            Offer(primaryId, variant),
        };
        var prices = new[]
        {
            Base(lowestId, 200m),
            Base(primaryId, 100m),
        };
        var inventory = new Dictionary<Guid, decimal> { [lowestId] = 0m, [primaryId] = 5m };

        var selected = await MerchandisingCampaignDevelopmentSeed.ResolveStorefrontPrimaryOffersAsync(
            new StubCatalogRead(new Dictionary<Guid, Guid> { [variant] = product }),
            new StubPriceQuery(prices),
            new StubInventory(inventory),
            new PrimaryOfferSelectionPolicy(),
            offers,
            maxProducts: 8,
            Now,
            CancellationToken.None);

        var primary = Assert.Single(selected);
        Assert.Equal(primaryId, primary.OfferId);
        Assert.Equal(100m, primary.BaseAmount);
    }

    [Fact]
    public async Task Stock_and_price_tie_breaks_follow_the_canonical_policy()
    {
        var product = Guid.NewGuid();
        var variant = Guid.NewGuid();
        var cheaperInStock = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var cheaperNoStock = Guid.Parse("00000001-0000-0000-0000-000000000001");

        // ارزان‌تر بدون موجودی است؛ برنده باید Offer دارای موجودی باشد حتی اگر گران‌تر باشد.
        var stockBeatsPrice = await Resolve(
            product,
            variant,
            offers: [Offer(cheaperNoStock, variant), Offer(cheaperInStock, variant)],
            prices: [Base(cheaperNoStock, 10m), Base(cheaperInStock, 900m)],
            available: new Dictionary<Guid, decimal> { [cheaperNoStock] = 0m, [cheaperInStock] = 1m });
        Assert.Equal(cheaperInStock, stockBeatsPrice.OfferId);

        // هر دو دارای موجودی و هم‌قیمت: کمترین OfferId برنده است.
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var idTieBreak = await Resolve(
            product,
            variant,
            offers: [Offer(higherId, variant), Offer(lowerId, variant)],
            prices: [Base(higherId, 500m), Base(lowerId, 500m)],
            available: new Dictionary<Guid, decimal> { [higherId] = 3m, [lowerId] = 3m });
        Assert.Equal(lowerId, idTieBreak.OfferId);
    }

    [Fact]
    public async Task Row_covering_the_start_but_ending_early_is_expired_and_replaced()
    {
        var offer = Guid.NewGuid();
        var campaign = Guid.NewGuid();
        var validTo = Now.AddDays(7);
        var stale = Row(offer, campaign, 70m, Now.AddDays(-1), Now.AddHours(1));
        var dir = new RecordingPriceDirectory();

        await MerchandisingCampaignDevelopmentSeed.EnsureCampaignPromoPricesAsync(
            dir,
            new StubPriceQuery([stale]),
            campaign,
            [Target(offer, 70m)],
            Now,
            validTo,
            CancellationToken.None);

        Assert.Equal([stale.PriceId], dir.Expired);
        Assert.Equal([offer], dir.Created.Select(x => x.OfferId));
    }

    [Fact]
    public async Task Fully_covering_row_is_left_untouched()
    {
        var offer = Guid.NewGuid();
        var campaign = Guid.NewGuid();
        var validTo = Now.AddDays(7);
        var covering = Row(offer, campaign, 70m, Now.AddDays(-1), validTo);
        var dir = new RecordingPriceDirectory();

        await MerchandisingCampaignDevelopmentSeed.EnsureCampaignPromoPricesAsync(
            dir,
            new StubPriceQuery([covering]),
            campaign,
            [Target(offer, 70m)],
            Now,
            validTo,
            CancellationToken.None);

        Assert.Empty(dir.Expired);
        Assert.Empty(dir.Created);
    }

    [Fact]
    public async Task Rerun_after_creation_writes_nothing_new()
    {
        var offer = Guid.NewGuid();
        var campaign = Guid.NewGuid();
        var validTo = Now.AddDays(7);
        var written = Row(offer, campaign, 70m, Now, validTo);
        var dir = new RecordingPriceDirectory();

        await MerchandisingCampaignDevelopmentSeed.EnsureCampaignPromoPricesAsync(
            dir,
            new StubPriceQuery([written]),
            campaign,
            [Target(offer, 70m)],
            Now,
            validTo,
            CancellationToken.None);

        Assert.Empty(dir.Expired);
        Assert.Empty(dir.Created);
        Assert.Empty(dir.Activated);
    }

    [Fact]
    public void Window_coverage_uses_the_half_open_pricing_endpoint_semantics()
    {
        var validTo = Now.AddDays(7);

        // شروع شامل است: ردیفی که دقیقاً از شروع درخواست آغاز می‌شود پوشش می‌دهد.
        Assert.True(MerchandisingCampaignDevelopmentSeed.CoversRequiredWindow(Now, validTo, Now, validTo));
        // پایان انحصاری است: ردیفی که دقیقاً در پایان درخواست تمام می‌شود هنوز پوشش می‌دهد.
        Assert.True(MerchandisingCampaignDevelopmentSeed.CoversRequiredWindow(Now, validTo, Now, validTo));
        // یک تیک کمتر از پایان درخواست، پوشش کامل نیست.
        Assert.False(MerchandisingCampaignDevelopmentSeed.CoversRequiredWindow(Now, validTo.AddTicks(-1), Now, validTo));
        // پنجرهٔ بی‌پایان درخواست فقط با ردیف بی‌پایان پوشش می‌یابد.
        Assert.False(MerchandisingCampaignDevelopmentSeed.CoversRequiredWindow(Now, validTo, Now, null));
        Assert.True(MerchandisingCampaignDevelopmentSeed.CoversRequiredWindow(Now, null, Now, null));
        // ردیفی که بعد از شروع درخواست آغاز می‌شود پوشش نمی‌دهد.
        Assert.False(MerchandisingCampaignDevelopmentSeed.CoversRequiredWindow(Now.AddTicks(1), validTo, Now, validTo));
    }

    [Fact]
    public async Task Seed_does_not_run_outside_development()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new StubEnvironment(development: false));
        using var provider = services.BuildServiceProvider();

        // هیچ وابستگی دیگری رجیستر نشده است؛ اگر دانه Production را لمس کند اینجا می‌شکند.
        await MerchandisingCampaignDevelopmentSeed.EnsureAsync(provider, CancellationToken.None);
    }

    private static async Task<MerchandisingCampaignDevelopmentSeed.StorefrontPrimaryOffer> Resolve(
        Guid product,
        Guid variant,
        IReadOnlyList<OfferReference> offers,
        IReadOnlyList<AuthoredPriceSnapshot> prices,
        IReadOnlyDictionary<Guid, decimal> available)
    {
        var selected = await MerchandisingCampaignDevelopmentSeed.ResolveStorefrontPrimaryOffersAsync(
            new StubCatalogRead(new Dictionary<Guid, Guid> { [variant] = product }),
            new StubPriceQuery(prices),
            new StubInventory(available),
            new PrimaryOfferSelectionPolicy(),
            offers,
            maxProducts: 8,
            Now,
            CancellationToken.None);

        return Assert.Single(selected);
    }

    private static OfferReference Offer(Guid offerId, Guid variant) =>
        new(offerId, variant, Guid.NewGuid(), SalesChannel.Marketplace, OfferStatus.Active, $"SKU-{offerId:N}");

    private static AuthoredPriceSnapshot Base(Guid offerId, decimal amount) =>
        new(Guid.NewGuid(), offerId, "IR", SalesChannel.Marketplace, amount, "IRR", "Active",
            Now.AddDays(-1), null, "Base", null);

    private static AuthoredPriceSnapshot Row(
        Guid offerId,
        Guid campaignId,
        decimal amount,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo) =>
        new(Guid.NewGuid(), offerId, "IR", SalesChannel.Marketplace, amount, "IRR", "Active",
            validFrom, validTo, "MerchandisingCampaign", campaignId.ToString("D"));

    private static MerchandisingCampaignDevelopmentSeed.CampaignPromoTarget Target(Guid offerId, decimal amount) =>
        new(offerId, amount, "IR", SalesChannel.Marketplace, "IRR");

    private sealed class StubEnvironment(bool development) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = development ? "Development" : "Production";

        public string ApplicationName { get; set; } = "Tooba.Promotion.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class StubCatalogRead(IReadOnlyDictionary<Guid, Guid> variantToProduct) : ICatalogOfferReadGateway
    {
        public Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, CatalogOfferPresentation>>(
                catalogVariantIds
                    .Distinct()
                    .Where(variantToProduct.ContainsKey)
                    .ToDictionary(
                        id => id,
                        id => new CatalogOfferPresentation(
                            id,
                            variantToProduct[id],
                            $"Product {id:N}",
                            BrandName: null,
                            UnitCode: null,
                            UnitName: null,
                            UnitShortName: null)));
    }

    private sealed class StubPriceQuery(IReadOnlyList<AuthoredPriceSnapshot> rows) : IPriceQueryGateway
    {
        public Task<IReadOnlyList<OfferAmountRow>> ListOfferAmountsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OfferAmountRow>>([]);

        public Task<IReadOnlyList<AuthoredPriceSnapshot>> ListByOfferIdsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AuthoredPriceSnapshot>>(
                rows.Where(row => offerIds.Contains(row.OfferId)).ToList());

        public Task<IReadOnlyList<Guid>> ListActiveCampaignOfferIdsAsync(
            IReadOnlyCollection<Guid> offerIds,
            string campaignKey,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<AuthoredPriceSnapshot?> FindLatestActiveBaseAsync(
            Guid offerId,
            string market,
            string currency,
            CancellationToken cancellationToken) =>
            Task.FromResult<AuthoredPriceSnapshot?>(null);
    }

    private sealed class StubInventory(IReadOnlyDictionary<Guid, decimal> availableByOffer) : IInventoryQueryGateway
    {
        public Task<IReadOnlyList<StockPositionSnapshot>> ListPositionsByOfferIdsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StockPositionSnapshot>>(
                offerIds
                    .Where(availableByOffer.ContainsKey)
                    .Select(id => new StockPositionSnapshot(
                        Guid.NewGuid(), id, Guid.Empty, availableByOffer[id], 0m, availableByOffer[id]))
                    .ToList());

        public Task<IReadOnlyList<StockPositionSnapshot>> ListAllPositionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StockPositionSnapshot>>([]);

        public Task<IReadOnlyList<InventoryLocationSnapshot>> ListLocationsByIdsAsync(
            IReadOnlyCollection<Guid> locationIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InventoryLocationSnapshot>>([]);

        public Task<InventoryLocationSnapshot?> FindLocationByCodeAsync(
            string code,
            CancellationToken cancellationToken) =>
            Task.FromResult<InventoryLocationSnapshot?>(null);

        public Task<Guid?> FindFirstActiveLocationIdAsync(CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);

        public Task<StockPositionSnapshot?> FindPositionAsync(
            Guid offerId,
            Guid locationId,
            CancellationToken cancellationToken) =>
            Task.FromResult<StockPositionSnapshot?>(null);

        public Task<StockPositionSnapshot?> FindPositionByStockItemIdAsync(
            Guid stockItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult<StockPositionSnapshot?>(null);
    }

    private sealed class RecordingPriceDirectory : IPriceDirectory
    {
        public List<Guid> Expired { get; } = [];

        public List<MerchandisingCampaignDevelopmentSeed.CampaignPromoTarget> Created { get; } = [];

        public List<Guid> Activated { get; } = [];

        public Task<PriceQuote> CreatePriceAsync(
            Guid offerId,
            string market,
            SalesChannel channel,
            decimal amount,
            string currency,
            DateTimeOffset validFrom,
            DateTimeOffset? validTo,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PriceQuote> CreateCampaignPriceAsync(
            Guid offerId,
            Guid campaignId,
            string market,
            SalesChannel channel,
            decimal amount,
            string currency,
            DateTimeOffset validFrom,
            DateTimeOffset? validTo,
            CancellationToken cancellationToken)
        {
            Created.Add(new MerchandisingCampaignDevelopmentSeed.CampaignPromoTarget(
                offerId, amount, market, channel, currency));
            return Task.FromResult(new PriceQuote(
                Guid.NewGuid(), offerId, market, channel, amount, currency, TaxExclusive: true, IsAuthored: true));
        }

        public Task ActivateAsync(Guid priceId, CancellationToken cancellationToken)
        {
            Activated.Add(priceId);
            return Task.CompletedTask;
        }

        public Task ChangeAmountAsync(
            Guid priceId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ExpireAsync(Guid priceId, CancellationToken cancellationToken)
        {
            Expired.Add(priceId);
            return Task.CompletedTask;
        }
    }
}
