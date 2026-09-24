using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Commands.AddCartLine;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Application.Presentation;
using Tooba.Cart.Application.Validation;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Party.Contracts;
using Xunit;

namespace Tooba.Cart.Tests.Behavior;

/// <summary>
/// TB-TMAR-CART-MULTICURRENCY-LINES-001 — line-level currency authority, default-selection
/// semantics, and per-currency totals. Unlike currencies are never summed into one scalar.
/// </summary>
public sealed class CartMulticurrencyTests
{
    [Fact]
    public async Task Mixed_currency_lines_yield_one_total_per_currency_in_ordinal_order()
    {
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
            2,
            [
                Line(1500m, "IRR", 2m),
                Line(10m, "USD", 3m),
            ]);

        var page = await Composer().PresentAsync(snapshot, guestSecret: null, CancellationToken.None);

        Assert.Equal("IRR", page.DefaultCurrency);
        Assert.Equal(2, page.TotalsByCurrency.Count);
        Assert.Equal("IRR", page.TotalsByCurrency[0].Currency);
        Assert.Equal(3000m, page.TotalsByCurrency[0].SubtotalExclusiveOfTax);
        Assert.Equal("USD", page.TotalsByCurrency[1].Currency);
        Assert.Equal(30m, page.TotalsByCurrency[1].SubtotalExclusiveOfTax);
        Assert.Equal(5m, page.ItemCount);
        Assert.All(page.Lines, line => Assert.False(string.IsNullOrWhiteSpace(line.Currency)));
    }

    [Fact]
    public async Task CartPage_exposes_no_cross_currency_scalar_subtotal()
    {
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
            [Line(10m, "USD", 1m)]);

        var page = await Composer().PresentAsync(snapshot, guestSecret: null, CancellationToken.None);

        Assert.Null(typeof(CartPage).GetProperty("SubtotalExclusiveOfTax"));
        Assert.DoesNotContain(
            typeof(CartPage).GetProperties(),
            property => property.Name == "Currency" && property.PropertyType == typeof(string));
        var total = Assert.Single(page.TotalsByCurrency);
        Assert.Equal("USD", total.Currency);
        Assert.Equal(10m, total.SubtotalExclusiveOfTax);
    }

    [Fact]
    public async Task Missing_line_currency_fails_closed_without_default_fallback()
    {
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
            [Line(10m, null, 1m)]);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Composer().PresentAsync(snapshot, guestSecret: null, CancellationToken.None));
        Assert.Equal(CartErrorCodes.LineCurrencyMissing, error.Message);
    }

    [Fact]
    public async Task Converted_cart_keeps_default_currency_metadata_and_empty_totals()
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
            [Line(10m, "USD", 1m)]);

        var page = await Composer().PresentAsync(snapshot, guestSecret: null, CancellationToken.None);

        Assert.Equal("IRR", page.DefaultCurrency);
        Assert.Empty(page.TotalsByCurrency);
        Assert.Empty(page.Lines);
    }

    [Fact]
    public void Add_line_validator_allows_absent_currency_and_requires_three_characters_when_present()
    {
        var validator = new AddCartLineCommandValidator();

        var absent = validator.Validate(new AddCartLineCommand(Guid.NewGuid(), null, 1, Guid.NewGuid(), 2m));
        Assert.True(absent.IsValid);

        var blank = validator.Validate(new AddCartLineCommand(Guid.NewGuid(), null, 1, Guid.NewGuid(), 2m, null, "  "));
        Assert.True(blank.IsValid);

        var shaped = validator.Validate(new AddCartLineCommand(Guid.NewGuid(), null, 1, Guid.NewGuid(), 2m, null, "usd"));
        Assert.True(shaped.IsValid);

        var tooShort = validator.Validate(new AddCartLineCommand(Guid.NewGuid(), null, 1, Guid.NewGuid(), 2m, null, "IR"));
        Assert.False(tooShort.IsValid);
        Assert.Contains(
            tooShort.Errors,
            failure => failure.ErrorCode == CartValidationCodes.CurrencyShape);
    }

    private static CartLineSnapshot Line(decimal unitAmount, string? currency, decimal quantity) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantity,
            null,
            unitAmount,
            currency,
            true,
            null,
            DateTimeOffset.UtcNow);

    private static CartPresentationComposer Composer() =>
        new(new StubCartQueries(), new StubCatalog(), new StubParties(), new StubUser());

    private sealed class StubUser : ICurrentAuthenticatedUser
    {
        public bool IsAuthenticated => false;
        public Guid? UserId => null;
    }

    private sealed class StubCartQueries : ICartQueryGateway
    {
        public Task<CartSnapshot?> GetCartAsync(Guid cartId, CartAccess access, CancellationToken cancellationToken)
            => Task.FromResult<CartSnapshot?>(null);
    }

    private sealed class StubParties : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken)
            => Task.FromResult<PartyLookupResult?>(null);

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(string term, int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class StubCatalog : ICatalogCartPresentationLookup
    {
        public Task<IReadOnlyDictionary<Guid, CatalogCartVariantPresentation>> GetVariantPresentationsAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, CatalogCartVariantPresentation>>(
                new Dictionary<Guid, CatalogCartVariantPresentation>());

        public Task<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>> GetEffectiveQuantityPoliciesForVariantIdsAsync(
            IReadOnlyCollection<Guid> variantIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>>(
                new Dictionary<Guid, EffectiveQuantityPolicy>());
    }
}
