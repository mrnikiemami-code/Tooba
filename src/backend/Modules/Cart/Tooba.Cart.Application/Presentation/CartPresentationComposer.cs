using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts;
using Tooba.Party.Contracts;

namespace Tooba.Cart.Application.Presentation;

/// <summary>
/// Owns Cart storefront presentation enrichment via Catalog/Party Contracts only.
/// </summary>
public sealed class CartPresentationComposer : Tooba.Cart.Contracts.ICartPresentationGateway
{
    private readonly ICartQueryGateway _cartQueries;
    private readonly ICatalogCartPresentationLookup _catalog;
    private readonly IPartyLookup _parties;
    private readonly ICurrentAuthenticatedUser _user;

    /// <summary>Creates the Cart presentation composer.</summary>
    public CartPresentationComposer(
        ICartQueryGateway cartQueries,
        ICatalogCartPresentationLookup catalog,
        IPartyLookup parties,
        ICurrentAuthenticatedUser user)
    {
        _cartQueries = cartQueries;
        _catalog = catalog;
        _parties = parties;
        _user = user;
    }

    /// <inheritdoc />
    public async Task<CartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var snapshot = await _cartQueries.GetCartAsync(cartId, Access(guestSecret), cancellationToken);
        return snapshot is null ? null : await PresentAsync(snapshot, guestSecret: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartPage?> TryGetForOwnershipAsync(
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        try
        {
            return await GetAsync(cartId, guestSecret, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Presents a Cart snapshot with Catalog/Party enrichment.</summary>
    public async Task<CartPage> PresentAsync(
        CartSnapshot snapshot,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var variantIds = snapshot.Lines.Select(line => line.CatalogVariantId).Distinct().ToList();
        var presentations = variantIds.Count == 0
            ? new Dictionary<Guid, CatalogCartVariantPresentation>()
            : await _catalog.GetVariantPresentationsAsync(variantIds, cancellationToken);
        var policies = await _catalog.GetEffectiveQuantityPoliciesForVariantIdsAsync(
            snapshot.Lines.Select(item => item.CatalogVariantId).Distinct().ToArray(),
            cancellationToken);

        var lines = new List<CartLineView>();
        foreach (var line in snapshot.Lines)
        {
            presentations.TryGetValue(line.CatalogVariantId, out var presentation);
            var seller = await _parties.FindByIdAsync(line.SellerPartyId, cancellationToken);
            var unit = line.QuotedAmount;
            var lineAmount = unit is decimal amount ? amount * line.Quantity : (decimal?)null;
            var title = presentation?.LocalizedTitle ?? string.Empty;
            var slug = presentation?.Slug;
            Guid? mediaId = presentation?.MediaAssetId;
            if (mediaId == Guid.Empty)
            {
                mediaId = null;
            }

            policies.TryGetValue(line.CatalogVariantId, out var policy);
            // Line currency truth only. There is no cart.DefaultCurrency fallback for a quoted line.
            if (string.IsNullOrWhiteSpace(line.QuotedCurrency))
            {
                throw new InvalidOperationException(CartErrorCodes.LineCurrencyMissing);
            }

            lines.Add(new CartLineView(
                line.LineId,
                line.OfferId,
                line.CatalogVariantId,
                line.SellerPartyId,
                presentation?.ProductId,
                slug,
                title,
                seller?.DisplayName ?? string.Empty,
                mediaId,
                line.Quantity,
                unit,
                lineAmount,
                line.QuotedCurrency,
                line.QuotedTaxExclusive,
                policy?.UnitCode,
                policy?.UnitDisplayName ?? policy?.UnitShortName,
                policy?.DecimalPlaces ?? 0,
                policy?.Step,
                line.Availability.ToString(),
                line.MerchandisingCampaignId));
        }

        var totalsByCurrency = CartCurrencyTotals.Group(
            lines.Select(item => (item.Currency, item.LineAmountExclusiveOfTax)));
        if (snapshot.Status == CartStatus.Converted)
        {
            return new CartPage(
                snapshot.CartId,
                snapshot.Version,
                snapshot.Market,
                snapshot.DefaultCurrency,
                snapshot.Channel.ToString(),
                0,
                Array.Empty<CartCurrencyTotal>(),
                Array.Empty<CartLineView>(),
                guestSecret,
                snapshot.Status.ToString());
        }

        return new CartPage(
            snapshot.CartId,
            snapshot.Version,
            snapshot.Market,
            snapshot.DefaultCurrency,
            snapshot.Channel.ToString(),
            snapshot.Lines.Sum(item => item.Quantity),
            totalsByCurrency,
            lines,
            guestSecret,
            snapshot.Status.ToString());
    }

    private CartAccess Access(string? guestSecret)
    {
        var userId = _user.IsAuthenticated ? _user.UserId : null;
        return new CartAccess(userId, guestSecret);
    }
}
