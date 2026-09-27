using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductTaxonomy.Models;
using Tooba.Catalog.Application.ProductTaxonomy.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Catalog persistence for Admin product category/brand taxonomy mutations.</summary>
public sealed class ProductTaxonomyDirectory : IProductTaxonomyDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly ICatalogDirectory _catalogDirectory;

    /// <summary>Creates the directory.</summary>
    public ProductTaxonomyDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        ICatalogDirectory catalogDirectory)
    {
        _db = db;
        _guard = guard;
        _catalogDirectory = catalogDirectory;
    }

    /// <inheritdoc />
    public async Task<Result> AssignPrimaryCategoryAsync(
        Guid productId,
        WorkspaceProductCategoryAssignWriteModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        if (product.UpdatedAt != model.ExpectedUpdatedAt)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceCatalogStale));
        }

        if (!await _db.Categories.AsNoTracking().AnyAsync(x => x.CategoryId == model.CategoryId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductCategoryInvalid));
        }

        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        if (!CatalogCategoryTreeRules.IsAssignableProductCategory(model.CategoryId, parentById))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryAssignmentLevelInvalid));
        }

        var hasAttrValues = await _db.ProductAttributeValues.AsNoTracking()
            .AnyAsync(x => x.ProductId == productId, cancellationToken);
        var hasVariants = await _db.Variants.AsNoTracking()
            .AnyAsync(x => x.ProductId == productId, cancellationToken);
        if ((hasAttrValues || hasVariants) && !model.ConfirmSchemaImpact)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductCategorySchemaImpact));
        }

        try
        {
            var hasExistingCategory = await _db.ProductCategories.AsNoTracking()
                .AnyAsync(
                    x => x.ProductId == productId && x.Role == CatalogProductCategoryRole.Primary,
                    cancellationToken);
            if (hasExistingCategory)
            {
                await _catalogDirectory.ReplaceProductPrimaryCategoryAsync(
                    productId, model.CategoryId, cancellationToken);
            }
            else
            {
                await _catalogDirectory.AssignCategoryAsync(productId, model.CategoryId, cancellationToken);
            }

            product.TouchDescriptiveSeams(product.SlugSeam, product.SeoTitleSeam, product.BrandId, DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            var code = IsAssignmentLevelInvalid(ex)
                ? CatalogErrorCodes.CategoryAssignmentLevelInvalid
                : CatalogErrorCodes.WorkspaceProductCategoryAssignRejected;
            return Result.Failure(new SemanticError(code));
        }
    }

    /// <inheritdoc />
    public async Task<Result> AddAdditionalCategoryAsync(
        Guid productId,
        WorkspaceProductAdditionalCategoryWriteModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        if (product.UpdatedAt != model.ExpectedUpdatedAt)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceCatalogStale));
        }

        try
        {
            await _catalogDirectory.AddProductAdditionalCategoryAsync(
                productId, model.CategoryId, cancellationToken);
            product.TouchDescriptiveSeams(product.SlugSeam, product.SeoTitleSeam, product.BrandId, DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            var code = IsAssignmentLevelInvalid(ex)
                ? CatalogErrorCodes.CategoryAssignmentLevelInvalid
                : ex.Message.Contains("دسته اصلی", StringComparison.Ordinal)
                    ? CatalogErrorCodes.CategoryAssignmentDuplicatePrimary
                    : ex.Message.Contains("قبلاً", StringComparison.Ordinal)
                        ? CatalogErrorCodes.CategoryAssignmentDuplicate
                        : CatalogErrorCodes.CategoryAssignmentInvalid;
            return Result.Failure(new SemanticError(code));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        if (product.UpdatedAt != expectedUpdatedAt)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceCatalogStale));
        }

        try
        {
            await _catalogDirectory.RemoveProductAdditionalCategoryAsync(
                productId, categoryId, cancellationToken);
            product.TouchDescriptiveSeams(product.SlugSeam, product.SeoTitleSeam, product.BrandId, DateTimeOffset.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message.Contains("دسته اصلی", StringComparison.Ordinal)
                ? CatalogErrorCodes.CategoryAssignmentCannotRemovePrimary
                : CatalogErrorCodes.CategoryAssignmentMissing;
            return Result.Failure(new SemanticError(code));
        }
    }

    /// <inheritdoc />
    public async Task<Result> AssignBrandAsync(
        Guid productId,
        WorkspaceProductBrandAssignWriteModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        if (product.UpdatedAt != model.ExpectedUpdatedAt)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceCatalogStale));
        }

        if (model.BrandId is { } brandId)
        {
            var exists = await _db.Brands.AsNoTracking().AnyAsync(x => x.BrandId == brandId, cancellationToken);
            if (!exists)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductBrandInvalid));
            }
        }

        var previous = product.BrandId;
        product.AssignBrand(model.BrandId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        await _catalogDirectory.AppendProductHistoryAsync(
            productId,
            ProductHistoryRules.EventGeneralChanged,
            ProductHistoryRules.SectionGeneral,
            "برند محصول به‌روزرسانی شد",
            previous is null ? "بدون برند" : "برند قبلی",
            model.BrandId is null ? "بدون برند" : "برند جدید",
            cancellationToken);

        return Result.Success();
    }

    private static bool IsAssignmentLevelInvalid(InvalidOperationException ex) =>
        string.Equals(
            ex.Message,
            CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa,
            StringComparison.Ordinal);
}
