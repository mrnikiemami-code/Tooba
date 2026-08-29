using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Party.Domain;

namespace Tooba.Offer.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد. ماتریس Seller Portal اینجا نیست.
/// </summary>
public sealed class OpenOfferUseCaseGuard : IOfferUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// نوشتن Offer با قرارداد Catalog و Party. DbContext آن ماژول‌ها لمس نمی‌شود تا مرز Persistence حفظ شود.
/// </summary>
public sealed class OfferDirectory : IOfferDirectory, IOfferLookupGateway
{
    private readonly OfferDbContext _db;
    private readonly IOfferUseCaseGuard _guard;
    private readonly ICatalogLookupGateway _catalog;
    private readonly IPartyLookupGateway _party;

    /// <summary>
    /// دایرکتوری را به schema Offer و درزهای قرارداد وصل می‌کند نه به join بین‌schema.
    /// </summary>
    public OfferDirectory(
        OfferDbContext db,
        IOfferUseCaseGuard guard,
        ICatalogLookupGateway catalog,
        IPartyLookupGateway party)
    {
        _db = db;
        _guard = guard;
        _catalog = catalog;
        _party = party;
    }

    /// <inheritdoc />
    public async Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await _db.Offers.AsNoTracking().SingleOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);
        return offer is null ? null : ToReference(offer);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalogVariantIds);
        if (catalogVariantIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var ids = catalogVariantIds.Distinct().ToArray();
        var rows = await _db.Offers.AsNoTracking()
            .Where(x => ids.Contains(x.CatalogVariantId) && x.Status != OfferStatus.Archived)
            .GroupBy(x => x.CatalogVariantId)
            .Select(g => new { VariantId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = ids.ToDictionary(id => id, _ => 0);
        foreach (var row in rows)
        {
            result[row.VariantId] = row.Count;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<OfferReference> CreateOfferAsync(
        Guid catalogVariantId,
        Guid sellerPartyId,
        SalesChannel channel,
        string? sellerSku,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (await _catalog.FindVariantAsync(catalogVariantId, cancellationToken) is null)
        {
            throw new InvalidOperationException("Variant Catalog از قرارداد Lookup پیدا نشد؛ DbContext کاتالوگ خوانده نشد.");
        }

        var seller = await _party.FindByIdAsync(sellerPartyId, cancellationToken)
            ?? throw new InvalidOperationException("فروشنده از قرارداد Party پیدا نشد؛ جدول party مستقیم خوانده نشد.");
        if (seller.Kind != PartyKind.Organization)
        {
            throw new InvalidOperationException("فروشنده باید Organization باشد؛ User ورود فروشنده نیست.");
        }

        var exists = await _db.Offers.AnyAsync(
            x => x.SellerPartyId == sellerPartyId
                 && x.CatalogVariantId == catalogVariantId
                 && x.Channel == channel
                 && x.Status != OfferStatus.Archived,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("برای این فروشنده و Variant و کانال یک Offer غیرآرشیو وجود دارد.");
        }

        if (!string.IsNullOrWhiteSpace(sellerSku)
            && await _db.Offers.AnyAsync(x => x.SellerPartyId == sellerPartyId && x.SellerSku == sellerSku.Trim(), cancellationToken))
        {
            throw new InvalidOperationException("SKU فروشنده داخل همان فروشنده تکراری است؛ شناسهٔ جهانی Catalog نیست.");
        }

        var offer = SellerOffer.Create(catalogVariantId, sellerPartyId, channel, sellerSku, DateTimeOffset.UtcNow);
        _db.Offers.Add(offer);
        await _db.SaveChangesAsync(cancellationToken);
        return ToReference(offer);
    }

    /// <inheritdoc />
    public async Task ActivateAsync(Guid offerId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == offerId, cancellationToken);
        offer.Activate(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SuspendAsync(Guid offerId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == offerId, cancellationToken);
        offer.Suspend(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArchiveAsync(Guid offerId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await _db.Offers.SingleAsync(x => x.OfferId == offerId, cancellationToken);
        offer.Archive(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static OfferReference ToReference(SellerOffer offer) =>
        new(offer.OfferId, offer.CatalogVariantId, offer.SellerPartyId, offer.Channel, offer.Status, offer.SellerSku);
}
