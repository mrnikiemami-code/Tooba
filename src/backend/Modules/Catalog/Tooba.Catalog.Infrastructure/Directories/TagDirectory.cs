using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Models;
using Tooba.Catalog.Application.Tags.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for Admin tag read/write and assignments.</summary>
public sealed class TagDirectory : ITagDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;

    /// <summary>Creates the directory.</summary>
    public TagDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagView>> ListAsync(
        string locale,
        string? search,
        CancellationToken cancellationToken)
    {
        var tags = await _db.Tags.AsNoTracking().OrderBy(x => x.Code).Take(200).ToListAsync(cancellationToken);
        var views = new List<TagView>(tags.Count);
        foreach (var tag in tags)
        {
            views.Add(await MapTagViewAsync(tag, locale, cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            views = views
                .Where(v =>
                    v.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || v.Code.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return views;
    }

    /// <inheritdoc />
    public async Task<Result<TagView>> GetAsync(Guid tagId, string? locale, CancellationToken cancellationToken)
    {
        var tag = await _db.Tags.AsNoTracking().SingleOrDefaultAsync(x => x.TagId == tagId, cancellationToken);
        if (tag is null)
        {
            return Result.Failure<TagView>(new SemanticError(CatalogErrorCodes.TagMissing));
        }

        return Result.Success(await MapTagViewAsync(tag, locale ?? "fa-IR", cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Result<TagView>> CreateAsync(CreateTagWriteModel model, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var localizedNames = model.LocalizedNames ?? new Dictionary<string, string>();
        var faName = localizedNames
            .FirstOrDefault(x => x.Key.StartsWith("fa", StringComparison.OrdinalIgnoreCase))
            .Value?.Trim();
        if (string.IsNullOrWhiteSpace(faName) && localizedNames.Count > 0)
        {
            faName = localizedNames.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        }

        if (string.IsNullOrWhiteSpace(faName))
        {
            return Result.Failure<TagView>(new SemanticError(CatalogErrorCodes.TagInvalid));
        }

        if (localizedNames.Count == 0)
        {
            return Result.Failure<TagView>(new SemanticError(CatalogErrorCodes.TagInvalid));
        }

        var resolvedCode = string.IsNullOrWhiteSpace(model.Code)
            ? await ResolveUniqueTagCodeAsync(SlugifyTagCode(faName), cancellationToken)
            : model.Code.Trim().ToLowerInvariant();
        var codeTaken = await _db.Tags.AsNoTracking().AnyAsync(x => x.Code == resolvedCode, cancellationToken);
        if (codeTaken)
        {
            return Result.Failure<TagView>(new SemanticError(CatalogErrorCodes.TagCodeDuplicate));
        }

        var tag = CatalogTag.Create(resolvedCode, model.Slug, DateTimeOffset.UtcNow);
        _db.Tags.Add(tag);
        foreach (var pair in localizedNames)
        {
            _db.LocalizedTexts.Add(
                CatalogLocalizedText.Create(CatalogLocalizedOwnerKind.Tag, tag.TagId, "name", pair.Key, pair.Value));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(await MapTagViewAsync(tag, model.Locale ?? "fa-IR", cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagView>> ListProductTagsAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var tagIds = await _db.ProductTagAssignments.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.TagId)
            .ToListAsync(cancellationToken);
        if (tagIds.Count == 0)
        {
            return [];
        }

        var tags = await _db.Tags.AsNoTracking().Where(x => tagIds.Contains(x.TagId)).ToListAsync(cancellationToken);
        var views = new List<TagView>(tags.Count);
        foreach (var tag in tags.OrderBy(x => x.Code))
        {
            views.Add(await MapTagViewAsync(tag, locale ?? "fa-IR", cancellationToken));
        }

        return views;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagView>>> AssignProductTagAsync(
        Guid productId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AsNoTracking().AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<TagView>>(new SemanticError(CatalogErrorCodes.TagProductMissing));
        }

        if (!await _db.Tags.AsNoTracking().AnyAsync(x => x.TagId == tagId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<TagView>>(new SemanticError(CatalogErrorCodes.TagMissing));
        }

        var exists = await _db.ProductTagAssignments.AsNoTracking()
            .AnyAsync(x => x.ProductId == productId && x.TagId == tagId, cancellationToken);
        if (exists)
        {
            return Result.Failure<IReadOnlyList<TagView>>(new SemanticError(CatalogErrorCodes.TagAssignDuplicate));
        }

        _db.ProductTagAssignments.Add(CatalogProductTagAssignment.Assign(productId, tagId));
        await _db.SaveChangesAsync(cancellationToken);
        var list = await ListProductTagsAsync(productId, "fa-IR", cancellationToken);
        return Result.Success(list);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagView>>> RemoveProductTagAsync(
        Guid productId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var row = await _db.ProductTagAssignments
            .SingleOrDefaultAsync(x => x.ProductId == productId && x.TagId == tagId, cancellationToken);
        if (row is not null)
        {
            _db.ProductTagAssignments.Remove(row);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var list = await ListProductTagsAsync(productId, "fa-IR", cancellationToken);
        return Result.Success(list);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagView>> ListCategoryTagsAsync(
        Guid categoryId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var tagIds = await _db.CategoryTagAssignments.AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .Select(x => x.TagId)
            .ToListAsync(cancellationToken);
        if (tagIds.Count == 0)
        {
            return [];
        }

        var tags = await _db.Tags.AsNoTracking().Where(x => tagIds.Contains(x.TagId)).ToListAsync(cancellationToken);
        var views = new List<TagView>(tags.Count);
        foreach (var tag in tags.OrderBy(x => x.Code))
        {
            views.Add(await MapTagViewAsync(tag, locale ?? "fa-IR", cancellationToken));
        }

        return views;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagView>>> AssignCategoryTagAsync(
        Guid categoryId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Categories.AsNoTracking().AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<TagView>>(new SemanticError(CatalogErrorCodes.TagCategoryMissing));
        }

        if (!await _db.Tags.AsNoTracking().AnyAsync(x => x.TagId == tagId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<TagView>>(new SemanticError(CatalogErrorCodes.TagMissing));
        }

        var exists = await _db.CategoryTagAssignments.AsNoTracking()
            .AnyAsync(x => x.CategoryId == categoryId && x.TagId == tagId, cancellationToken);
        if (exists)
        {
            return Result.Failure<IReadOnlyList<TagView>>(new SemanticError(CatalogErrorCodes.TagAssignDuplicate));
        }

        _db.CategoryTagAssignments.Add(CatalogCategoryTagAssignment.Assign(categoryId, tagId));
        await _db.SaveChangesAsync(cancellationToken);
        var list = await ListCategoryTagsAsync(categoryId, "fa-IR", cancellationToken);
        return Result.Success(list);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagView>>> RemoveCategoryTagAsync(
        Guid categoryId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var row = await _db.CategoryTagAssignments
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId && x.TagId == tagId, cancellationToken);
        if (row is not null)
        {
            _db.CategoryTagAssignments.Remove(row);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var list = await ListCategoryTagsAsync(categoryId, "fa-IR", cancellationToken);
        return Result.Success(list);
    }

    private async Task<TagView> MapTagViewAsync(CatalogTag tag, string locale, CancellationToken cancellationToken)
    {
        var name = await PreferLocaleNameAsync(tag.TagId, locale, cancellationToken) ?? tag.Code;
        return new TagView(tag.TagId, tag.Code, tag.SlugSeam, tag.Status, name, tag.CreatedAt, tag.UpdatedAt);
    }

    private async Task<string?> PreferLocaleNameAsync(Guid tagId, string locale, CancellationToken cancellationToken)
    {
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Tag && x.OwnerId == tagId && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        return rows.FirstOrDefault(x => string.Equals(x.Locale, locale, StringComparison.OrdinalIgnoreCase))?.Value
            ?? rows.FirstOrDefault(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase))?.Value
            ?? rows[0].Value;
    }

    private static string SlugifyTagCode(string name)
    {
        var raw = name.Trim().ToLowerInvariant();
        var chars = raw.Select(ch => char.IsLetterOrDigit(ch) || ch > 127 ? ch : '-').ToArray();
        var code = new string(chars);
        while (code.Contains("--", StringComparison.Ordinal))
        {
            code = code.Replace("--", "-", StringComparison.Ordinal);
        }

        code = code.Trim('-');
        if (string.IsNullOrWhiteSpace(code))
        {
            return "tag";
        }

        return code.Length <= 64 ? code : code[..64].TrimEnd('-');
    }

    private async Task<string> ResolveUniqueTagCodeAsync(string baseCode, CancellationToken cancellationToken)
    {
        var candidate = baseCode;
        var n = 0;
        while (await _db.Tags.AsNoTracking().AnyAsync(x => x.Code == candidate, cancellationToken))
        {
            n++;
            var suffix = $"-{n}";
            var trimmed = baseCode.Length + suffix.Length <= 64
                ? baseCode
                : baseCode[..Math.Max(1, 64 - suffix.Length)].TrimEnd('-');
            candidate = trimmed + suffix;
        }

        return candidate;
    }
}
