using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Application.Composition;
using Tooba.Cart.Application.Presentation;
using Tooba.Cart.Contracts;
using Tooba.Cart.Contracts.Errors;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Party.Contracts.Ports;
using Xunit;

namespace Tooba.Cart.Tests.Behavior;

public sealed class CartPresentationAndErrorTests
{
    [Fact]
    public async Task Semantic_faults_map_to_result_failures_by_stable_code()
    {
        var codes = new[]
        {
            CartErrorCodes.Missing,
            CartErrorCodes.GuestInvalid,
            CartErrorCodes.AccessDenied,
            CartErrorCodes.VersionConflict,
            CartErrorCodes.Expired,
            CartErrorCodes.OfferUnavailable,
            CartErrorCodes.InventoryInsufficient,
            CartErrorCodes.InventoryStale,
            CartErrorCodes.QuantityInvalid,
            CartErrorCodes.LineMissing,
            CartErrorCodes.Rejected,
            CartErrorCodes.AuthenticationRequired,
            CartErrorCodes.PricingQuoteMissing,
        };

        foreach (var code in codes)
        {
            var result = await CartOperation.ExecuteAsync<int>(() =>
                throw new SemanticException(new SemanticError(code)));

            Assert.True(result.IsFailure, code);
            Assert.Equal(code, result.FirstError.Code);
            Assert.Equal(code, Assert.Single(result.Errors).Code);
        }
    }

    [Fact]
    public async Task Semantic_faults_map_to_valueless_result_failures()
    {
        var result = await CartOperation.ExecuteAsync(() =>
            throw new SemanticException(new SemanticError(CartErrorCodes.LineMergeViaQuantity)));

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrorCodes.LineMergeViaQuantity, result.FirstError.Code);
    }

    [Fact]
    public async Task CartOperation_propagates_unknown_exceptions_untouched()
    {
        var unknown = new InvalidOperationException("cart.pricing.quote_missing");
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CartOperation.ExecuteAsync<int>(() => throw unknown));
        Assert.Same(unknown, thrown);
        Assert.Equal("cart.pricing.quote_missing", thrown.Message);
    }

    [Fact]
    public async Task CartOperation_returns_success_value_without_errors()
    {
        var result = await CartOperation.ExecuteAsync(() => Task.FromResult(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Converted_cart_presents_empty_lines_and_zero_totals()
    {
        var snapshot = new CartSnapshot(
            Guid.NewGuid(),
            CartStatus.Converted,
            CartAccessKind.Guest,
            null,
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            null,
            CartConversionIntent.OnlinePurchase,
            3,
            [
                new CartLineSnapshot(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    2m,
                    null,
                    1000m,
                    "IRR",
                    true,
                    null,
                    DateTimeOffset.UtcNow)
            ]);

        var composer = new CartPresentationComposer(
            new StubCartQueries(),
            new StubCatalog(),
            new StubParties(),
            new StubUser());

        var page = await composer.PresentAsync(snapshot, guestSecret: "raw-secret", CancellationToken.None);
        Assert.Equal(0m, page.ItemCount);
        Assert.Empty(page.TotalsByCurrency);
        Assert.Empty(page.Lines);
        Assert.Equal("Converted", page.Status);
        Assert.Equal("raw-secret", page.GuestSecret);
    }

    [Fact]
    public async Task Active_cart_enriches_product_seller_media_and_quantity_policy()
    {
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var snapshot = new CartSnapshot(
            Guid.NewGuid(),
            CartStatus.Active,
            CartAccessKind.Guest,
            null,
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            null,
            CartConversionIntent.None,
            1,
            [
                new CartLineSnapshot(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    variantId,
                    sellerId,
                    2m,
                    null,
                    1500m,
                    "IRR",
                    true,
                    null,
                    DateTimeOffset.UtcNow,
                    MerchandisingCampaignId: Guid.NewGuid())
            ]);

        var composer = new CartPresentationComposer(
            new StubCartQueries(),
            new StubCatalog(variantId, productId, "tea", "چای", mediaId),
            new StubParties(sellerId, "فروشنده تست"),
            new StubUser());

        var page = await composer.PresentAsync(snapshot, guestSecret: null, CancellationToken.None);
        Assert.Equal(2m, page.ItemCount);
        var total = Assert.Single(page.TotalsByCurrency);
        Assert.Equal("IRR", total.Currency);
        Assert.Equal(3000m, total.SubtotalExclusiveOfTax);
        var line = Assert.Single(page.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal("tea", line.ProductSlug);
        Assert.Equal("چای", line.Title);
        Assert.Equal("فروشنده تست", line.SellerDisplayName);
        Assert.Equal(mediaId, line.MediaAssetId);
        Assert.Equal("kg", line.UnitCode);
        Assert.Equal(2, line.QuantityDecimalPlaces);
        Assert.Equal(0.5m, line.QuantityStep);
        Assert.NotNull(line.MerchandisingCampaignId);
    }

    private sealed class StubUser : BuildingBlocks.Security.ICurrentAuthenticatedUser
    {
        public bool IsAuthenticated => false;
        public Guid? UserId => null;
    }

    private sealed class StubCartQueries : ICartQueryGateway
    {
        public Task<CartSnapshot?> GetCartAsync(Guid cartId, CartAccess access, CancellationToken cancellationToken)
            => Task.FromResult<CartSnapshot?>(null);
    }

    private sealed class StubParties(Guid? id = null, string? name = null) : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken)
            => Task.FromResult(id == partyId ? new PartyLookupResult(partyId, "Organization", name) : null);

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(string term, int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class StubCatalog : ICatalogCartPresentationLookup
    {
        private readonly Guid _variantId;
        private readonly CatalogCartVariantPresentation? _presentation;

        public StubCatalog()
        {
            _variantId = Guid.Empty;
        }

        public StubCatalog(Guid variantId, Guid productId, string slug, string title, Guid mediaId)
        {
            _variantId = variantId;
            _presentation = new CatalogCartVariantPresentation(variantId, productId, slug, title, mediaId);
        }

        public Task<IReadOnlyDictionary<Guid, CatalogCartVariantPresentation>> GetVariantPresentationsAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken)
        {
            var map = new Dictionary<Guid, CatalogCartVariantPresentation>();
            if (_presentation is not null && variantIds.Contains(_variantId))
            {
                map[_variantId] = _presentation;
            }

            return Task.FromResult<IReadOnlyDictionary<Guid, CatalogCartVariantPresentation>>(map);
        }

        public Task<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>> GetEffectiveQuantityPoliciesForVariantIdsAsync(
            IReadOnlyCollection<Guid> variantIds,
            CancellationToken cancellationToken)
        {
            var map = new Dictionary<Guid, EffectiveQuantityPolicy>();
            if (_presentation is not null && variantIds.Contains(_variantId))
            {
                map[_variantId] = new EffectiveQuantityPolicy(
                    _presentation.ProductId,
                    Guid.NewGuid(),
                    "kg",
                    "کیلوگرم",
                    "kg",
                    2,
                    0.5m,
                    QuantityRoundingMode.Nearest);
            }

            return Task.FromResult<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>>(map);
        }
    }
}
