using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Party.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition.Models;
using Tooba.ProductWorkspace.Application.Composition.Queries;
using Tooba.Tax.Contracts;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Focused W19 aggregate GET composition parity tests (Contracts-only fakes).</summary>
public sealed class ProductWorkspaceAggregateGetW19Tests
{
    [Fact]
    public async Task Missing_product_returns_workspace_product_missing()
    {
        var handler = CreateHandler(catalog: new FakeCatalog(null));
        var result = await handler.Handle(
            new GetProductWorkspaceQuery(Guid.NewGuid(), FullPermissions()),
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrorCodes.WorkspaceProductMissing, result.Errors[0].Code);
    }

    [Fact]
    public async Task View_scope_permissions_are_view_only_and_full_scope_is_full()
    {
        var productId = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var handler = CreateHandler(catalog: new FakeCatalog(MinimalSnapshot(productId)));

        var view = await handler.Handle(
            new GetProductWorkspaceQuery(productId, new ProductWorkspacePermissions(true, false, false, false, false)),
            CancellationToken.None);
        Assert.True(view.IsSuccess);
        Assert.False(view.Value.Permissions.CanEditCatalog);
        Assert.False(view.Value.Permissions.CanPublish);

        var full = await handler.Handle(
            new GetProductWorkspaceQuery(productId, FullPermissions()),
            CancellationToken.None);
        Assert.True(full.IsSuccess);
        Assert.True(full.Value.Permissions.CanEditCatalog);
        Assert.True(full.Value.Permissions.CanPublish);
    }

    [Fact]
    public async Task Commercial_warnings_purchasable_hint_seller_fallback_and_unsupported_mutations_match()
    {
        var productId = Guid.Parse("22222222-2222-7222-8222-222222222222");
        var variantId = Guid.Parse("33333333-3333-7333-8333-333333333333");
        var offerId = Guid.Parse("44444444-4444-7444-8444-444444444444");
        var sellerId = Guid.Parse("55555555-5555-7555-8555-555555555555");
        var locationId = Guid.Parse("66666666-6666-7666-8666-666666666666");

        var snapshot = MinimalSnapshot(productId) with
        {
            Variants = [new CatalogAdminProductVariant(variantId, "fp", "Active", "SKU-1")],
            CatalogReadinessMessages = ["چک کاتالوگ"],
            Title = "عنوان"
        };

        var handler = CreateHandler(
            catalog: new FakeCatalog(snapshot),
            offers: new FakeOffers([
                new OfferReference(offerId, variantId, sellerId, SalesChannel.Marketplace, OfferStatus.Active, "sku")
            ]),
            prices: new FakePrices([
                new AuthoredPriceSnapshot(Guid.NewGuid(), offerId, "IR", SalesChannel.Marketplace, 1000m, "IRR", "Active",
                    DateTimeOffset.UnixEpoch, null, "Base", null)
            ]),
            inventory: new FakeInventory(
                [new StockPositionSnapshot(Guid.NewGuid(), offerId, locationId, 5, 1, 4)],
                [new InventoryLocationSnapshot(locationId, "WH1", "انبار ۱", "Active")]));

        var result = await handler.Handle(
            new GetProductWorkspaceQuery(productId, FullPermissions()),
            CancellationToken.None);
        Assert.True(result.IsSuccess);
        var view = result.Value;
        Assert.Equal("عنوان", view.Title);
        Assert.True(view.Publication.PurchasableHint);
        Assert.Equal(["چک کاتالوگ"], view.Publication.Checks);
        Assert.Equal("فروشنده", view.Offers[0].SellerDisplayName);
        Assert.Equal(1, view.Variants[0].OfferCount);
        Assert.Equal(1, view.Variants[0].LocationCount);
        Assert.Contains("media-binary-upload", view.UnsupportedMutations);
        Assert.Contains("product-video-upload", view.UnsupportedMutations);
        Assert.Contains("promotion-write", view.UnsupportedMutations);
        Assert.Contains("full-content-studio", view.UnsupportedMutations);
        Assert.Equal("چک کاتالوگ", view.ReadinessWarnings[0]);
    }

    [Fact]
    public async Task No_active_offer_price_or_stock_emits_exact_commercial_warnings()
    {
        var productId = Guid.Parse("77777777-7777-7777-8777-777777777777");
        var handler = CreateHandler(
            catalog: new FakeCatalog(MinimalSnapshot(productId) with { CatalogReadinessMessages = [] }));
        var result = await handler.Handle(new GetProductWorkspaceQuery(productId, FullPermissions()), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Publication.PurchasableHint);
        Assert.Equal(
            [
                "پیشنهاد فروشندهٔ فعالی ثبت نشده است",
                "قیمت فروشنده ثبت نشده است",
                "موجودی قابل‌فروش وجود ندارد"
            ],
            result.Value.ReadinessWarnings);
    }

    private static GetProductWorkspaceHandler CreateHandler(
        ICatalogAdminProductWorkspaceReadGateway? catalog = null,
        IOfferQueryGateway? offers = null,
        IPriceQueryGateway? prices = null,
        IInventoryQueryGateway? inventory = null,
        ITaxQueryGateway? tax = null,
        IPartyLookup? parties = null) =>
        new(
            catalog ?? new FakeCatalog(null),
            offers ?? new FakeOffers(),
            prices ?? new FakePrices(),
            inventory ?? new FakeInventory(),
            tax ?? new FakeTax(),
            parties ?? new FakeParties());

    private static ProductWorkspacePermissions FullPermissions() =>
        new(true, true, true, true, true);

    private static CatalogAdminProductWorkspaceSnapshot MinimalSnapshot(Guid productId) =>
        new(
            productId,
            "untitled",
            "Draft",
            "PhysicalGood",
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.UnixEpoch,
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            0,
            null,
            "pcs",
            "عدد",
            [],
            [],
            null,
            null,
            false,
            null,
            [],
            [],
            [],
            [],
            [],
            new CatalogAdminPublishReadiness(false, false, false, false, false, false, false, [], "not-ready"),
            [],
            [],
            []);

    private sealed class FakeCatalog(CatalogAdminProductWorkspaceSnapshot? snapshot) : ICatalogAdminProductWorkspaceReadGateway
    {
        public Task<CatalogAdminProductWorkspaceSnapshot?> GetAggregateSnapshotAsync(
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }

    private sealed class FakeOffers(IReadOnlyList<OfferReference>? rows = null) : IOfferQueryGateway
    {
        public Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<OfferReference?>(null);

        public Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, OfferReference>>(new Dictionary<Guid, OfferReference>());

        public Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(catalogVariantIds.ToDictionary(x => x, _ => 0));

        public Task<int> CountActiveOffersAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<IReadOnlyList<Guid>> ListDistinctSellerPartyIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<OfferSellerStatusRow>> ListSellerStatusRowsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OfferSellerStatusRow>>([]);

        public Task<IReadOnlyDictionary<Guid, int>> CountActiveOffersBySellerAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

        public Task<IReadOnlyDictionary<Guid, int>> CountAllOffersGroupedByCatalogVariantAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());

        public Task<IReadOnlyDictionary<Guid, Guid>> MapAllOfferIdsToCatalogVariantIdsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid>>(new Dictionary<Guid, Guid>());

        public Task<IReadOnlyList<OfferReference>> ListOffersByCatalogVariantIdsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(rows ?? []);

        public Task<IReadOnlyList<OfferReference>> ListActiveOffersByCatalogVariantIdsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(rows ?? []);

        public Task<IReadOnlyList<OfferReference>> ListActiveOffersAsync(CancellationToken cancellationToken) =>
            Task.FromResult(rows ?? []);

        public Task<bool> AnyOffersForCatalogVariantIdsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult((rows ?? []).Count > 0);

        public Task<IReadOnlyList<OfferListItem>> ListRecentActiveOffersAsync(
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OfferListItem>>([]);

        public Task<OfferReference?> FindLatestBySellerAndVariantAsync(
            Guid sellerPartyId,
            Guid catalogVariantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OfferReference?>(null);

        public Task<bool> ExistsBySellerSkuAsync(
            Guid sellerPartyId,
            string sellerSku,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<Guid>> ListOfferIdsBySellerSkuPrefixAsync(
            string sellerSkuPrefix,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<OfferReference?> FindBySellerSkuAsync(
            string sellerSku,
            CancellationToken cancellationToken) =>
            Task.FromResult<OfferReference?>(null);
    }

    private sealed class FakePrices(IReadOnlyList<AuthoredPriceSnapshot>? rows = null) : IPriceQueryGateway
    {
        public Task<IReadOnlyList<OfferAmountRow>> ListOfferAmountsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OfferAmountRow>>([]);

        public Task<IReadOnlyList<AuthoredPriceSnapshot>> ListByOfferIdsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(rows ?? []);

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

    private sealed class FakeInventory(
        IReadOnlyList<StockPositionSnapshot>? positions = null,
        IReadOnlyList<InventoryLocationSnapshot>? locations = null) : IInventoryQueryGateway
    {
        public Task<IReadOnlyList<StockPositionSnapshot>> ListPositionsByOfferIdsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(positions ?? []);

        public Task<IReadOnlyList<StockPositionSnapshot>> ListAllPositionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(positions ?? []);

        public Task<IReadOnlyList<InventoryLocationSnapshot>> ListLocationsByIdsAsync(
            IReadOnlyCollection<Guid> locationIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(locations ?? []);

        public Task<InventoryLocationSnapshot?> FindLocationByCodeAsync(string code, CancellationToken cancellationToken) =>
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

    private sealed class FakeTax : ITaxQueryGateway
    {
        public Task<TaxCategorySnapshot?> FindCategoryByCodesAsync(
            IReadOnlyCollection<string> codes,
            CancellationToken cancellationToken) =>
            Task.FromResult<TaxCategorySnapshot?>(null);

        public Task<bool> HasActiveRuleAsync(
            Guid categoryId,
            string jurisdiction,
            string market,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<TaxClassificationSnapshot>> ListClassificationsByOfferIdsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TaxClassificationSnapshot>>([]);

        public Task<IReadOnlyList<TaxCategorySnapshot>> ListCategoriesByIdsAsync(
            IReadOnlyCollection<Guid> categoryIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TaxCategorySnapshot>>([]);
    }

    private sealed class FakeParties : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken) =>
            Task.FromResult<PartyLookupResult?>(null);

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyList<Guid> partyIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
            string term,
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
            string? op,
            string? value,
            IReadOnlyList<string>? values,
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }
}

