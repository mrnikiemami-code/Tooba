using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Contracts;
using Tooba.Cart.Application.Presentation;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Party.Contracts;
using Xunit;

namespace Tooba.Cart.Tests.Behavior;

public sealed class CartPresentationAndErrorTests
{
    [Fact]
    public void Exception_mapper_maps_exact_stable_codes_only()
    {
        Assert.Equal(CartErrorCodes.Missing, CartExceptionMapper.ToSemanticError(new InvalidOperationException(CartErrorCodes.Missing)).Code);
        Assert.Equal(CartErrorCodes.GuestInvalid, CartExceptionMapper.ToSemanticError(new InvalidOperationException("cart.guest_secret.invalid")).Code);
        Assert.Equal(CartErrorCodes.AccessDenied, CartExceptionMapper.ToSemanticError(new InvalidOperationException(CartErrorCodes.AccessDenied)).Code);
        Assert.Equal(CartErrorCodes.VersionConflict, CartExceptionMapper.ToSemanticError(new InvalidOperationException("cart.version.stale")).Code);
        Assert.Equal(CartErrorCodes.Expired, CartExceptionMapper.ToSemanticError(new InvalidOperationException(CartErrorCodes.Expired)).Code);
        Assert.Equal(CartErrorCodes.OfferUnavailable, CartExceptionMapper.ToSemanticError(new InvalidOperationException("cart.offer.inactive")).Code);
        Assert.Equal(CartErrorCodes.InventoryInsufficient, CartExceptionMapper.ToSemanticError(new InvalidOperationException(CartErrorCodes.InventoryInsufficient)).Code);
        Assert.Equal(CartErrorCodes.InventoryStale, CartExceptionMapper.ToSemanticError(new InvalidOperationException(CartErrorCodes.InventoryStale)).Code);
        Assert.Equal(CartErrorCodes.QuantityInvalid, CartExceptionMapper.ToSemanticError(new InvalidOperationException("cart.line.quantity_positive")).Code);
        Assert.Equal(CartErrorCodes.LineMissing, CartExceptionMapper.ToSemanticError(new InvalidOperationException(CartErrorCodes.LineMissing)).Code);
        Assert.Equal(CartErrorCodes.Rejected, CartExceptionMapper.ToSemanticError(new InvalidOperationException("cart.line.requires_active")).Code);
        Assert.Equal(CartErrorCodes.AuthenticationRequired, CartExceptionMapper.ToSemanticError(new InvalidOperationException(CartErrorCodes.AuthenticationRequired)).Code);
    }

    [Fact]
    public void Exception_mapper_does_not_parse_localized_or_prose_messages()
    {
        Assert.False(CartExceptionMapper.TryMapExact("پیدا نشد", out _));
        Assert.False(CartExceptionMapper.TryMapExact("Offer inactive", out _));
        Assert.False(CartExceptionMapper.TryMapExact("Held reservation", out _));
        Assert.False(CartExceptionMapper.TryMapExact("cart missing somehow", out _));
        Assert.Throws<InvalidOperationException>(() =>
            CartExceptionMapper.ToSemanticError(new InvalidOperationException("پیدا نشد")));
    }

    [Fact]
    public async Task Exception_mapper_propagates_unknown_InvalidOperationException()
    {
        var unknown = new InvalidOperationException("cart.pricing.quote_missing");
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CartExceptionMapper.TryAsync<int>(() => throw unknown));
        Assert.Same(unknown, thrown);
        Assert.Equal("cart.pricing.quote_missing", thrown.Message);
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
        Assert.Equal(0m, page.SubtotalExclusiveOfTax);
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
        Assert.Equal(3000m, page.SubtotalExclusiveOfTax);
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
