using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductSeo.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Catalog persistence for Admin product SEO read, update, and readiness.</summary>
public sealed class ProductSeoDirectory : IProductSeoDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly ICatalogActorContext? _actor;

    /// <summary>Creates the directory.</summary>
    public ProductSeoDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        ICatalogActorContext? actor = null)
    {
        _db = db;
        _guard = guard;
        _actor = actor;
    }

    /// <inheritdoc />
    public async Task<Result<ProductSeoDetail>> GetAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductSeoDetail>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        return Result.Success(await BuildProductSeoDetailAsync(product, locale, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Result<ProductSeoDetail>> UpdateAsync(
        Guid productId,
        ProductSeoUpdateInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var locale = ProductSeoRules.NormalizeLocale(input.Locale);
        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductSeoDetail>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        await _db.Entry(product).ReloadAsync(cancellationToken);
        if (product.UpdatedAt != input.ExpectedUpdatedAt)
        {
            return Result.Failure<ProductSeoDetail>(
                new SemanticError(CatalogErrorCodes.WorkspaceCatalogStale));
        }

        string slug;
        try
        {
            if (string.IsNullOrWhiteSpace(input.Slug))
            {
                var name = await ResolveProductNameForSeoAsync(productId, locale, cancellationToken);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Result.Failure<ProductSeoDetail>(
                        new SemanticError(CatalogErrorCodes.WorkspaceProductSlugInvalid));
                }

                slug = CatalogCategorySlugNormalizer.SlugifyFromName(name);
            }
            else
            {
                slug = CatalogCategorySlugNormalizer.NormalizeSlug(input.Slug);
            }
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<ProductSeoDetail>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductSlugInvalid));
        }

        if (await _db.Products.AsNoTracking()
                .AnyAsync(x => x.ProductId != productId && x.SlugSeam == slug, cancellationToken))
        {
            return Result.Failure<ProductSeoDetail>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductSlugDuplicate));
        }

        await UpsertProductLocalizedFieldForSeoAsync(productId, "seo_title", locale, input.SeoTitle, cancellationToken);
        await UpsertProductLocalizedFieldForSeoAsync(
            productId,
            "seo_description",
            locale,
            input.SeoDescription,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var seoTitleTrimmed = string.IsNullOrWhiteSpace(input.SeoTitle) ? null : input.SeoTitle.Trim();
        product.TouchDescriptiveSeams(slug, seoTitleTrimmed ?? product.SeoTitleSeam, product.BrandId, now);
        if (locale.Equals("fa-IR", StringComparison.OrdinalIgnoreCase))
        {
            product.SeoTitleSeam = seoTitleTrimmed;
        }

        product.SlugSeam = slug;
        product.UpdatedAt = now;
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventSeoChanged,
            ProductHistoryRules.SectionSeo,
            ProductHistoryRules.SummarySeoFa,
            null,
            slug);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(productId, locale, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ProductSeoReadiness>> GetReadinessAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var detail = await GetAsync(productId, locale, cancellationToken);
        if (detail.IsFailure)
        {
            return Result.Failure<ProductSeoReadiness>(detail.Errors);
        }

        return Result.Success(detail.Value.Readiness);
    }

    private async Task<ProductSeoDetail> BuildProductSeoDetailAsync(
        CatalogProduct product,
        string? locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = ProductSeoRules.NormalizeLocale(locale);
        var name = await ResolveProductNameForSeoAsync(product.ProductId, normalizedLocale, cancellationToken);
        var seoTitle = await ResolveLocalizedFieldForSeoAsync(
            product.ProductId,
            "seo_title",
            normalizedLocale,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(seoTitle)
            && normalizedLocale.Equals("fa-IR", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(product.SeoTitleSeam))
        {
            seoTitle = product.SeoTitleSeam;
        }

        var seoDescription = await ResolveLocalizedFieldForSeoAsync(
            product.ProductId,
            "seo_description",
            normalizedLocale,
            cancellationToken);
        var titleFallback = string.IsNullOrWhiteSpace(seoTitle) ? name : seoTitle;
        var snapshot = ProductSeoRules.Evaluate(product.SlugSeam, seoTitle, seoDescription, name);
        var readiness = new ProductSeoReadiness(
            snapshot.HasValidSlug,
            snapshot.HasSeoTitleOrFallback,
            snapshot.HasSeoDescription,
            snapshot.HasLocalizedIdentity,
            snapshot.IsReady,
            snapshot.MessageFa);

        return new ProductSeoDetail(
            product.ProductId,
            normalizedLocale,
            product.SlugSeam,
            seoTitle,
            seoDescription,
            name,
            titleFallback,
            ProductSeoRules.BuildPublicPath(normalizedLocale, product.SlugSeam),
            readiness,
            product.UpdatedAt);
    }

    private async Task<string?> ResolveProductNameForSeoAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        var exact = await ResolveLocalizedFieldForSeoAsync(productId, "name", locale, cancellationToken);
        if (!string.IsNullOrWhiteSpace(exact))
        {
            return exact;
        }

        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.OwnerId == productId
                && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        return rows
            .OrderBy(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Select(x => x.Value)
            .FirstOrDefault();
    }

    private async Task<string?> ResolveLocalizedFieldForSeoAsync(
        Guid productId,
        string fieldKey,
        string locale,
        CancellationToken cancellationToken)
    {
        var row = await _db.LocalizedTexts.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && x.OwnerId == productId
                    && x.FieldKey == fieldKey
                    && x.Locale == locale,
                cancellationToken);
        return row?.Value;
    }

    private async Task UpsertProductLocalizedFieldForSeoAsync(
        Guid productId,
        string fieldKey,
        string locale,
        string? value,
        CancellationToken cancellationToken)
    {
        var normalized = value?.Trim();
        var row = await _db.LocalizedTexts.SingleOrDefaultAsync(
            x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.OwnerId == productId
                && x.FieldKey == fieldKey
                && x.Locale == locale,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (row is not null)
            {
                _db.LocalizedTexts.Remove(row);
            }

            return;
        }

        if (row is null)
        {
            _db.LocalizedTexts.Add(CatalogLocalizedText.Create(
                CatalogLocalizedOwnerKind.Product,
                productId,
                fieldKey,
                locale,
                normalized));
        }
        else
        {
            row.Value = normalized;
        }
    }

    private void QueueProductHistory(
        Guid productId,
        string eventType,
        string section,
        string summaryFa,
        string? beforeSummary,
        string? afterSummary)
    {
        _db.ProductHistoryEntries.Add(CatalogProductHistoryEntry.Create(
            productId,
            eventType,
            section,
            summaryFa,
            DateTimeOffset.UtcNow,
            _actor?.ActorUserId,
            _actor?.ActorDisplayName,
            beforeSummary,
            afterSummary));
    }
}
