using Microsoft.EntityFrameworkCore;
using Tooba.Cart.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Offer.Domain;
using Tooba.Party.Application;

namespace Tooba.Host.Storefront;

/// <summary>
/// ترکیب نمایشی سبد در Host. Catalog و Party جدا از Cart خوانده می‌شوند و SQL بین‌schema نیست.
/// </summary>
public sealed class StorefrontCartComposer
{
    private readonly ICartDirectory _carts;
    private readonly ICartQueryGateway _cartQueries;
    private readonly CatalogDbContext _catalog;
    private readonly IPartyLookupGateway _parties;

    /// <summary>
    /// سازندهٔ ترکیب سبد فروشگاه.
    /// </summary>
    public StorefrontCartComposer(
        ICartDirectory carts,
        ICartQueryGateway cartQueries,
        CatalogDbContext catalog,
        IPartyLookupGateway parties)
    {
        _carts = carts;
        _cartQueries = cartQueries;
        _catalog = catalog;
        _parties = parties;
    }

    /// <summary>
    /// سبد مهمان می‌سازد. راز خام فقط در همین پاسخ برمی‌گردد.
    /// </summary>
    public async Task<StorefrontCartPage> CreateGuestAsync(CancellationToken cancellationToken)
    {
        var created = await _carts.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, cancellationToken);
        return await PresentAsync(created.Cart, created.GuestSecret, cancellationToken);
    }

    /// <summary>
    /// سبد را پس از احراز راز مهمان می‌خواند.
    /// </summary>
    public async Task<StorefrontCartPage?> GetAsync(Guid cartId, string? guestSecret, CancellationToken cancellationToken)
    {
        var snapshot = await _cartQueries.GetCartAsync(cartId, Access(guestSecret), cancellationToken);
        return snapshot is null ? null : await PresentAsync(snapshot, guestSecret: null, cancellationToken);
    }

    /// <summary>
    /// خط Offer را اضافه یا افزایش می‌دهد.
    /// </summary>
    public async Task<StorefrontCartPage> AddLineAsync(
        Guid cartId,
        string? guestSecret,
        int expectedVersion,
        Guid offerId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var snapshot = await _carts.AddOrIncreaseLineAsync(
            cartId,
            Access(guestSecret),
            expectedVersion,
            offerId,
            quantity,
            cancellationToken);
        return await PresentAsync(snapshot, guestSecret: null, cancellationToken);
    }

    /// <summary>
    /// تعداد خط را عوض می‌کند. صفر یعنی حذف.
    /// </summary>
    public async Task<StorefrontCartPage> ChangeLineAsync(
        Guid cartId,
        string? guestSecret,
        int expectedVersion,
        Guid lineId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var snapshot = await _carts.ChangeLineQuantityAsync(
            cartId,
            Access(guestSecret),
            expectedVersion,
            lineId,
            quantity,
            cancellationToken);
        return await PresentAsync(snapshot, guestSecret: null, cancellationToken);
    }

    /// <summary>
    /// خط را حذف می‌کند.
    /// </summary>
    public async Task<StorefrontCartPage> RemoveLineAsync(
        Guid cartId,
        string? guestSecret,
        int expectedVersion,
        Guid lineId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _carts.RemoveLineAsync(
            cartId,
            Access(guestSecret),
            expectedVersion,
            lineId,
            cancellationToken);
        return await PresentAsync(snapshot, guestSecret: null, cancellationToken);
    }

    private async Task<StorefrontCartPage> PresentAsync(
        CartSnapshot snapshot,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var variantIds = snapshot.Lines.Select(line => line.CatalogVariantId).Distinct().ToList();
        var variants = variantIds.Count == 0
            ? []
            : await _catalog.Variants.AsNoTracking()
                .Where(item => variantIds.Contains(item.VariantId))
                .ToListAsync(cancellationToken);
        var variantMap = variants.ToDictionary(item => item.VariantId);
        var productIds = variants.Select(item => item.ProductId).Distinct().ToList();
        var products = productIds.Count == 0
            ? []
            : await _catalog.Products.AsNoTracking()
                .Where(item => productIds.Contains(item.ProductId))
                .ToListAsync(cancellationToken);
        var productMap = products.ToDictionary(item => item.ProductId);
        var names = await LoadProductNamesAsync(productIds, cancellationToken);
        var media = productIds.Count == 0
            ? []
            : await _catalog.MediaReferences.AsNoTracking()
                .Where(item => productIds.Contains(item.ProductId))
                .ToListAsync(cancellationToken);
        var mediaMap = media
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.MediaAssetId).FirstOrDefault());

        var lines = new List<StorefrontCartLineView>();
        foreach (var line in snapshot.Lines)
        {
            variantMap.TryGetValue(line.CatalogVariantId, out var variant);
            CatalogProduct? product = null;
            if (variant is not null)
            {
                productMap.TryGetValue(variant.ProductId, out product);
            }

            var seller = await _parties.FindByIdAsync(line.SellerPartyId, cancellationToken);
            var unit = line.QuotedAmount;
            var lineAmount = unit is decimal amount ? amount * line.Quantity : (decimal?)null;
            var title = product is null
                ? "کالا"
                : names.GetValueOrDefault(product.ProductId) ?? product.SlugSeam ?? "کالا";
            var slug = product is null || string.IsNullOrWhiteSpace(product.SlugSeam)
                ? product?.ProductId.ToString("N")
                : product.SlugSeam;
            Guid? mediaId = product is null ? null : mediaMap.GetValueOrDefault(product.ProductId);
            if (mediaId == Guid.Empty)
            {
                mediaId = null;
            }

            lines.Add(new StorefrontCartLineView(
                line.LineId,
                line.OfferId,
                line.CatalogVariantId,
                line.SellerPartyId,
                product?.ProductId,
                slug,
                title,
                seller?.DisplayName ?? "فروشنده",
                mediaId,
                line.Quantity,
                unit,
                lineAmount,
                line.QuotedCurrency ?? snapshot.Currency,
                line.QuotedTaxExclusive));
        }

        var subtotal = lines.Sum(item => item.LineAmountExclusiveOfTax ?? 0);
        return new StorefrontCartPage(
            snapshot.CartId,
            snapshot.Version,
            snapshot.Market,
            snapshot.Currency,
            snapshot.Channel.ToString(),
            snapshot.Lines.Sum(item => item.Quantity),
            subtotal,
            lines,
            guestSecret);
    }

    private async Task<Dictionary<Guid, string>> LoadProductNamesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var rows = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(item => item.OwnerKind == CatalogLocalizedOwnerKind.Product
                && productIds.Contains(item.OwnerId)
                && item.FieldKey == "name")
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(item => item.OwnerId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => item.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
    }

    private static CartAccess Access(string? guestSecret) => new(null, guestSecret);
}
