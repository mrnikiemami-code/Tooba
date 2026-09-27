using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.ProductValues.Ports;
using Tooba.Catalog.Application.ProductMedia.Ports;
using Tooba.Catalog.Application.ProductPublishing.Ports;
using Tooba.Catalog.Application.ProductSeo.Ports;
using Tooba.Catalog.Application.Variants.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// Focused Catalog composition for Admin product publish readiness.
/// Reuses ProductSeo / ProductMedia / ProductVariants / ProductAttributes readiness seams.
/// </summary>
public sealed class ProductPublishReadinessReader : IProductPublishReadinessReader
{
    private readonly CatalogDbContext _db;
    private readonly IProductSeoDirectory _seo;
    private readonly IProductAttributeDirectory _attributes;
    private readonly IProductVariantDirectory _variants;
    private readonly IProductMediaDirectory _media;

    /// <summary>Creates the reader.</summary>
    public ProductPublishReadinessReader(
        CatalogDbContext db,
        IProductSeoDirectory seo,
        IProductAttributeDirectory attributes,
        IProductVariantDirectory variants,
        IProductMediaDirectory media)
    {
        _db = db;
        _seo = seo;
        _attributes = attributes;
        _variants = variants;
        _media = media;
    }

    /// <inheritdoc />
    public async Task<Result<ProductPublishReadiness>> GetAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AsNoTracking().AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<ProductPublishReadiness>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        var normalizedLocale = ProductSeoRules.NormalizeLocale(locale);
        var categoryReady = await IsProductPrimaryCategoryAssignableAsync(productId, cancellationToken);

        var seoDetailResult = await _seo.GetAsync(productId, normalizedLocale, cancellationToken);
        if (seoDetailResult.IsFailure)
        {
            return Result.Failure<ProductPublishReadiness>(seoDetailResult.Errors);
        }

        var seoDetail = seoDetailResult.Value;
        var translationReady = !string.IsNullOrWhiteSpace(seoDetail.ProductName);

        var attributesResult = await _attributes.GetReadinessAsync(productId, cancellationToken);
        if (attributesResult.IsFailure)
        {
            return Result.Failure<ProductPublishReadiness>(attributesResult.Errors);
        }

        var attributes = attributesResult.Value;
        var attributeReady = attributes.IsComplete;

        var variantsResult = await _variants.GetReadinessAsync(productId, cancellationToken);
        if (variantsResult.IsFailure)
        {
            return Result.Failure<ProductPublishReadiness>(variantsResult.Errors);
        }

        var variants = variantsResult.Value;
        var variantReady = variants.IsValid;

        var mediaResult = await _media.GetReadinessAsync(productId, cancellationToken);
        if (mediaResult.IsFailure)
        {
            return Result.Failure<ProductPublishReadiness>(mediaResult.Errors);
        }

        var media = mediaResult.Value;
        var mediaReady = media.IsReady;

        var seo = seoDetail.Readiness;
        var seoReady = seo.IsReady;

        var missing = new List<ProductPublishMissingRequirement>();
        if (!categoryReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "category",
                ProductPublishRules.MessageCategoryIncompleteFa,
                "general"));
        }

        if (!translationReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "identity",
                ProductPublishRules.MessageIdentityIncompleteFa,
                "general"));
        }

        if (!attributeReady)
        {
            var attrMessage = attributes.MissingRequiredCodes.Count > 0
                ? $"{ProductPublishRules.MessageAttributesIncompleteFa} ({string.Join("، ", attributes.MissingRequiredCodes)})"
                : ProductPublishRules.MessageAttributesIncompleteFa;
            missing.Add(new ProductPublishMissingRequirement("attributes", attrMessage, "attributes"));
        }

        if (!variantReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "variants",
                ProductPublishRules.MessageVariantsIncompleteFa,
                "variants"));
        }

        if (!mediaReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "media",
                media.MessageFa ?? ProductPublishRules.MessageMediaIncompleteFa,
                "media"));
        }

        if (!seoReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "seo",
                seo.MessageFa ?? ProductPublishRules.MessageSeoIncompleteFa,
                "seo"));
        }

        var isReady = missing.Count == 0;
        var messageFa = isReady
            ? ProductPublishRules.MessageReadyFa
            : ProductPublishRules.SummarizeMissingFa(missing.Count);

        return Result.Success(new ProductPublishReadiness(
            isReady,
            categoryReady,
            translationReady,
            attributeReady,
            variantReady,
            mediaReady,
            seoReady,
            missing,
            messageFa));
    }

    private async Task<bool> IsProductPrimaryCategoryAssignableAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var primaryCategoryId = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId && x.Role == CatalogProductCategoryRole.Primary)
            .Select(x => x.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);
        if (primaryCategoryId == Guid.Empty)
        {
            return false;
        }

        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        return CatalogCategoryTreeRules.IsAssignableProductCategory(primaryCategoryId, parentById);
    }
}
