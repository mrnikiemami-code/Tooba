using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductDeletion.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for Admin product hard-delete / soft-archive-on-Offer.</summary>
public sealed class ProductDeletionDirectory : IProductDeletionDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly IOfferQueryGateway _offers;

    /// <summary>Creates the directory.</summary>
    public ProductDeletionDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        IOfferQueryGateway offers)
    {
        _db = db;
        _guard = guard;
        _offers = offers;
    }

    /// <inheritdoc />
    public async Task<Result> DeleteOrSoftArchiveAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        var variantIds = await _db.Variants.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.VariantId)
            .ToListAsync(cancellationToken);
        var hasOffers = variantIds.Count > 0
            && await _offers.AnyOffersForCatalogVariantIdsAsync(variantIds, cancellationToken);

        if (hasOffers)
        {
            product.Archive(DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductDeleteReferenced));
        }

        var media = await _db.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var productAttrs = await _db.ProductAttributeValues.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var axes = await _db.ProductVariantAxes.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var categories = await _db.ProductCategories.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var names = await _db.LocalizedTexts
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product && x.OwnerId == productId)
            .ToListAsync(cancellationToken);
        var variants = await _db.Variants.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var variantAttr = variantIds.Count == 0
            ? []
            : await _db.VariantAttributeValues.Where(x => variantIds.Contains(x.VariantId)).ToListAsync(cancellationToken);

        _db.MediaReferences.RemoveRange(media);
        _db.ProductAttributeValues.RemoveRange(productAttrs);
        _db.ProductVariantAxes.RemoveRange(axes);
        _db.ProductCategories.RemoveRange(categories);
        _db.LocalizedTexts.RemoveRange(names);
        _db.VariantAttributeValues.RemoveRange(variantAttr);
        _db.Variants.RemoveRange(variants);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
