using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// نگهبان موقتی موردکاربرد. ماتریس SpiceDB/نقش روی موجودیت Catalog نوشته نمی‌شود.
/// </summary>
public sealed class OpenCatalogUseCaseGuard : ICatalogUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// پیاده‌سازی نوشتن/خواندن Catalog روی schema همین ماژول. Host و Search را parse/ایندکس نمی‌کند.
/// </summary>
public sealed class CatalogDirectory : ICatalogDirectory, ICatalogLookupGateway
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;

    /// <summary>
    /// دایرکتوری را به DbContext Tenant-aware وصل می‌کند.
    /// </summary>
    public CatalogDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<ProductReference?> FindProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        return product is null ? null : new ProductReference(product.ProductId, product.Kind, product.Status);
    }

    /// <inheritdoc />
    public async Task<VariantReference?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken)
    {
        var variant = await _db.Variants.AsNoTracking().SingleOrDefaultAsync(x => x.VariantId == variantId, cancellationToken);
        return variant is null
            ? null
            : new VariantReference(variant.VariantId, variant.ProductId, variant.CombinationFingerprint, variant.Status);
    }

    /// <inheritdoc />
    public async Task<ReviewableProductReference?> FindReviewableProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalized = slug.Trim().ToLowerInvariant();
        var productId = await _db.Products.AsNoTracking()
            .Where(x => x.SlugSeam == normalized)
            .Select(x => (Guid?)x.ProductId)
            .SingleOrDefaultAsync(cancellationToken);
        return productId is null ? null : await FindReviewableProductByIdAsync(productId.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReviewableProductReference?> FindReviewableProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new { x.ProductId, x.SlugSeam, x.Status, VariantIds = x.Variants.Select(v => v.VariantId).ToArray() })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;
        var titles = await GetProductTitlesAsync([productId], cancellationToken);
        var slug = product.SlugSeam ?? product.ProductId.ToString("N");
        return new ReviewableProductReference(product.ProductId, slug, titles.GetValueOrDefault(productId) ?? slug, product.Status, product.VariantIds);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetProductTitlesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0) return new Dictionary<Guid, string>();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.FieldKey == "name" && productIds.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.OwnerId).ToDictionary(x => x.Key, x => x.First().Value);
    }

    /// <inheritdoc />
    public async Task<CategoryReference> CreateCategoryAsync(
        Guid? parentCategoryId,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (parentCategoryId is Guid parent
            && !await _db.Categories.AnyAsync(x => x.CategoryId == parent, cancellationToken))
        {
            throw new InvalidOperationException("ردهٔ والد در Catalog این Tenant وجود ندارد.");
        }

        var now = DateTimeOffset.UtcNow;
        var category = CatalogCategory.Create(parentCategoryId, now);
        _db.Categories.Add(category);
        AddLocalizedNames(CatalogLocalizedOwnerKind.Category, category.CategoryId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return new CategoryReference(category.CategoryId, category.ParentCategoryId, category.Status);
    }

    /// <inheritdoc />
    public async Task<BrandReference> CreateBrandAsync(
        string? slugSeam,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var brand = CatalogBrand.Create(slugSeam, DateTimeOffset.UtcNow);
        _db.Brands.Add(brand);
        AddLocalizedNames(CatalogLocalizedOwnerKind.Brand, brand.BrandId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return new BrandReference(brand.BrandId, brand.SlugSeam, brand.Status);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAttributeDefinitionAsync(
        string code,
        CatalogAttributeValueKind valueKind,
        bool isVariantAxis,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var definition = CatalogAttributeDefinition.Create(code, valueKind, isVariantAxis, DateTimeOffset.UtcNow);
        _db.AttributeDefinitions.Add(definition);
        AddLocalizedNames(CatalogLocalizedOwnerKind.AttributeDefinition, definition.DefinitionId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return definition.DefinitionId;
    }

    /// <inheritdoc />
    public async Task<Guid> AddAttributeOptionAsync(
        Guid definitionId,
        string code,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == definitionId, cancellationToken);
        if (definition.ValueKind != CatalogAttributeValueKind.Enumeration)
        {
            throw new InvalidOperationException("گزینه فقط برای ویژگی شمارشی معنا دارد.");
        }

        var option = CatalogAttributeOption.Create(definitionId, code);
        _db.AttributeOptions.Add(option);
        AddLocalizedNames(CatalogLocalizedOwnerKind.AttributeOption, option.OptionId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return option.OptionId;
    }

    /// <inheritdoc />
    public async Task<ProductReference> CreateProductAsync(
        CatalogProductKind kind,
        string? slugSeam,
        Guid? brandId,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (brandId is Guid brand && !await _db.Brands.AnyAsync(x => x.BrandId == brand, cancellationToken))
        {
            throw new InvalidOperationException("برند در Catalog این Tenant وجود ندارد.");
        }

        var product = CatalogProduct.Create(kind, slugSeam, DateTimeOffset.UtcNow);
        product.BrandId = brandId;
        _db.Products.Add(product);
        AddLocalizedNames(CatalogLocalizedOwnerKind.Product, product.ProductId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductReference(product.ProductId, product.Kind, product.Status);
    }

    /// <inheritdoc />
    public async Task UpsertProductLocalizedFieldAsync(
        Guid productId,
        string fieldKey,
        IReadOnlyDictionary<string, string> localizedValues,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var normalizedKey = fieldKey.Trim().ToLowerInvariant();
        if (normalizedKey is not ("short_description" or "full_description"))
        {
            throw new InvalidOperationException("فقط فیلدهای شرح کوتاه و شرح کامل محصول از این درز پذیرفته می‌شوند.");
        }

        if (localizedValues.Count == 0
            || !await _db.Products.AnyAsync(product => product.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول موجود و حداقل یک متن محلی غیرخالی لازم است.");
        }

        foreach (var pair in localizedValues)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Value);
            var locale = pair.Key.Trim();
            var row = await _db.LocalizedTexts.SingleOrDefaultAsync(
                text => text.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && text.OwnerId == productId
                    && text.FieldKey == normalizedKey
                    && text.Locale == locale,
                cancellationToken);
            if (row is null)
            {
                _db.LocalizedTexts.Add(CatalogLocalizedText.Create(
                    CatalogLocalizedOwnerKind.Product,
                    productId,
                    normalizedKey,
                    locale,
                    pair.Value));
            }
            else
            {
                row.Value = pair.Value.Trim();
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AssignCategoryAsync(Guid productId, Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken)
            || !await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("محصول یا رده در Catalog این Tenant نیست.");
        }

        _db.ProductCategories.Add(CatalogProductCategory.Assign(productId, categoryId));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AttachMediaReferenceAsync(Guid productId, Guid mediaAssetId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        _db.MediaReferences.Add(CatalogProductMediaReference.Link(productId, mediaAssetId));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetProductAttributeAsync(
        Guid productId,
        Guid definitionId,
        string rawValue,
        Guid? enumOptionId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == definitionId, cancellationToken);
        if (definition.IsVariantAxis)
        {
            throw new InvalidOperationException("محور Variant روی خود Product ذخیره نمی‌شود؛ به گونه تعلق دارد.");
        }

        var canonical = CatalogAttributeCanonicalizer.Canonicalize(definition.ValueKind, rawValue, enumOptionId);
        _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, definitionId, canonical));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        product.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleAsync(x => x.CategoryId == categoryId, cancellationToken);
        category.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishBrandAsync(Guid brandId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var brand = await _db.Brands.SingleAsync(x => x.BrandId == brandId, cancellationToken);
        brand.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VariantReference> CreateVariantAsync(
        Guid productId,
        string? catalogCodeSeam,
        IReadOnlyList<(Guid DefinitionId, string RawValue, Guid? EnumOptionId)> axes,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول والد گونه در Catalog این Tenant نیست.");
        }

        var normalized = new List<(Guid DefinitionId, string Canonical)>();
        foreach (var axis in axes)
        {
            var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == axis.DefinitionId, cancellationToken);
            if (!definition.IsVariantAxis)
            {
                throw new InvalidOperationException("فقط ویژگی محور Variant می‌تواند ترکیب گونه بسازد.");
            }

            var canonical = CatalogAttributeCanonicalizer.Canonicalize(definition.ValueKind, axis.RawValue, axis.EnumOptionId);
            normalized.Add((definition.DefinitionId, canonical));
        }

        var fingerprint = CatalogVariant.ComputeFingerprint(normalized);
        if (await _db.Variants.AnyAsync(
                x => x.ProductId == productId && x.CombinationFingerprint == fingerprint,
                cancellationToken))
        {
            throw new InvalidOperationException("ترکیب محور این گونه برای همین محصول تکراری است؛ هویت Offer فروشنده نیست.");
        }

        var variant = CatalogVariant.Create(productId, fingerprint, catalogCodeSeam, DateTimeOffset.UtcNow);
        foreach (var item in normalized)
        {
            variant.AttributeValues.Add(CatalogVariantAttributeValue.Create(variant.VariantId, item.DefinitionId, item.Canonical));
        }

        _db.Variants.Add(variant);
        await _db.SaveChangesAsync(cancellationToken);
        return new VariantReference(variant.VariantId, variant.ProductId, variant.CombinationFingerprint, variant.Status);
    }

    private void AddLocalizedNames(CatalogLocalizedOwnerKind ownerKind, Guid ownerId, IReadOnlyDictionary<string, string> localizedNames)
    {
        if (localizedNames.Count == 0)
        {
            throw new InvalidOperationException("حداقل یک نام محلی برای موجودیت توصیفی Catalog لازم است.");
        }

        foreach (var pair in localizedNames)
        {
            _db.LocalizedTexts.Add(CatalogLocalizedText.Create(ownerKind, ownerId, "name", pair.Key, pair.Value));
        }
    }
}
