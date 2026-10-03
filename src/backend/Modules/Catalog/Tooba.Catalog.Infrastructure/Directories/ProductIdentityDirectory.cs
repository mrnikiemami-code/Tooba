using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductIdentity.Models;
using Tooba.Catalog.Application.ProductIdentity.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for Admin product identity mutations (create / title / core / quantity).</summary>
public sealed class ProductIdentityDirectory : IProductIdentityDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly ICatalogDirectory _catalogDirectory;

    /// <summary>Creates the directory.</summary>
    public ProductIdentityDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        ICatalogDirectory catalogDirectory)
    {
        _db = db;
        _guard = guard;
        _catalogDirectory = catalogDirectory;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateWorkspaceProductAsync(
        WorkspaceProductCreateWriteModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var title = model.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<Guid>.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductTitleMissing));
        }

        if (model.CategoryId is not Guid categoryId || categoryId == Guid.Empty)
        {
            return Result<Guid>.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductCategoryMissing));
        }

        if (!await _db.Categories.AsNoTracking().AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result<Guid>.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductCategoryInvalid));
        }

        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        if (!CatalogCategoryTreeRules.IsAssignableProductCategory(categoryId, parentById))
        {
            return Result<Guid>.Failure(new SemanticError(CatalogErrorCodes.CategoryAssignmentLevelInvalid));
        }

        var locale = string.IsNullOrWhiteSpace(model.Locale) ? "fa-IR" : model.Locale.Trim();
        var slugSeed = string.IsNullOrWhiteSpace(model.Slug)
            ? CatalogCategorySlugNormalizer.SlugifyFromName(title)
            : CatalogCategorySlugNormalizer.NormalizeSlug(model.Slug);
        if (string.IsNullOrWhiteSpace(slugSeed))
        {
            return Result<Guid>.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductSlugInvalid));
        }

        if (await _db.Products.AsNoTracking().AnyAsync(x => x.SlugSeam == slugSeed, cancellationToken))
        {
            return Result<Guid>.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductSlugDuplicate));
        }

        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [locale] = title };

        try
        {
            var product = await _catalogDirectory.CreateProductAsync(
                CatalogProductKind.PhysicalGood,
                slugSeed,
                null,
                names,
                cancellationToken);

            await _catalogDirectory.AssignCategoryAsync(product.ProductId, categoryId, cancellationToken);
            return Result<Guid>.Success(product.ProductId);
        }
        catch (InvalidOperationException)
        {
            return Result<Guid>.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductCreateRejected));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateCatalogTitleAsync(
        Guid productId,
        WorkspaceProductCatalogTitleWriteModel model,
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

        var locale = model.Locale;
        var title = model.Title;
        var row = await _db.LocalizedTexts.SingleOrDefaultAsync(
            x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.OwnerId == productId
                && x.FieldKey == "name"
                && x.Locale == locale,
            cancellationToken);
        if (row is null)
        {
            _db.LocalizedTexts.Add(CatalogLocalizedText.Create(
                CatalogLocalizedOwnerKind.Product,
                productId,
                "name",
                locale,
                title));
        }
        else
        {
            row.Value = title.Trim();
        }

        product.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _catalogDirectory.AppendProductHistoryAsync(
            productId,
            ProductHistoryRules.EventLocalizedChanged,
            ProductHistoryRules.SectionGeneral,
            ProductHistoryRules.SummaryLocalizedFa,
            null,
            title.Trim(),
            cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateProductCoreAsync(
        Guid productId,
        WorkspaceProductCoreUpdateWriteModel model,
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

        var title = model.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductTitleMissing));
        }

        var locale = string.IsNullOrWhiteSpace(model.Locale) ? "fa-IR" : model.Locale.Trim();
        var isPrimaryLocale = locale.Equals("fa-IR", StringComparison.OrdinalIgnoreCase);
        var previousSlug = product.SlugSeam;
        var previousTitle = (await _db.LocalizedTexts.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && x.OwnerId == productId
                    && x.FieldKey == "name"
                    && x.Locale == locale,
                cancellationToken))?.Value;

        string slug;
        if (isPrimaryLocale)
        {
            slug = string.IsNullOrWhiteSpace(model.Slug)
                ? CatalogCategorySlugNormalizer.SlugifyFromName(title)
                : CatalogCategorySlugNormalizer.NormalizeSlug(model.Slug);
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductSlugInvalid));
            }

            if (await _db.Products.AsNoTracking()
                    .AnyAsync(x => x.ProductId != productId && x.SlugSeam == slug, cancellationToken))
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductSlugDuplicate));
            }
        }
        else
        {
            slug = product.SlugSeam ?? string.Empty;
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductSlugMissing));
            }
        }

        await UpsertLocalizedTextAsync(productId, "name", locale, title, cancellationToken);
        await UpsertLocalizedTextAsync(productId, "short_description", locale, model.ShortDescription, cancellationToken);
        await UpsertLocalizedTextAsync(productId, "full_description", locale, model.Description, cancellationToken);
        await UpsertLocalizedTextAsync(productId, "seo_title", locale, model.SeoTitle, cancellationToken);
        await UpsertLocalizedTextAsync(productId, "seo_description", locale, model.SeoDescription, cancellationToken);

        if (isPrimaryLocale)
        {
            product.TouchDescriptiveSeams(slug, model.SeoTitle?.Trim(), product.BrandId, DateTimeOffset.UtcNow);
        }
        else
        {
            product.TouchDescriptiveSeams(product.SlugSeam, product.SeoTitleSeam, product.BrandId, DateTimeOffset.UtcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var beforeBits = new List<string>();
        var afterBits = new List<string>();
        if (!string.Equals(previousTitle, title, StringComparison.Ordinal))
        {
            beforeBits.Add($"عنوان: {previousTitle ?? "—"}");
            afterBits.Add($"عنوان: {title}");
        }

        if (!string.Equals(previousSlug, slug, StringComparison.Ordinal))
        {
            beforeBits.Add($"نشانی: {previousSlug ?? "—"}");
            afterBits.Add($"نشانی: {slug}");
        }

        var isLocalizedOnly = !isPrimaryLocale
            || (string.Equals(previousTitle, title, StringComparison.Ordinal)
                && string.Equals(previousSlug, slug, StringComparison.Ordinal));
        await _catalogDirectory.AppendProductHistoryAsync(
            productId,
            isLocalizedOnly
                ? ProductHistoryRules.EventLocalizedChanged
                : ProductHistoryRules.EventGeneralChanged,
            isLocalizedOnly ? ProductHistoryRules.SectionTranslations : ProductHistoryRules.SectionGeneral,
            isLocalizedOnly
                ? ProductHistoryRules.SummaryLocalizedFa
                : ProductHistoryRules.SummaryGeneralFa,
            beforeBits.Count == 0 ? null : string.Join(" · ", beforeBits),
            afterBits.Count == 0 ? null : string.Join(" · ", afterBits),
            cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateQuantityPolicyAsync(
        Guid productId,
        WorkspaceProductQuantityPolicyWriteModel model,
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
            product.SetQuantityPolicy(model.UnitOfMeasureId, model.DecimalPlaces, model.Step, DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceQuantityRejected));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task UpsertLocalizedTextAsync(
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
}
