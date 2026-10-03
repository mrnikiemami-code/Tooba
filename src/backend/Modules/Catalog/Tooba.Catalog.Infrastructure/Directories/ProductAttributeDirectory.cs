using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.ProductValues.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for Product Attribute Editor/Readiness Admin operations.</summary>
public sealed class ProductAttributeDirectory : IProductAttributeDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly ICatalogActorContext? _actor;

    /// <summary>Creates the directory.</summary>
    public ProductAttributeDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        ICatalogActorContext? actor = null)
    {
        _db = db;
        _guard = guard;
        _actor = actor;
    }

    /// <inheritdoc />
    public async Task<Result<ProductAttributeEditorState>> GetEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<ProductAttributeEditorState>(
                new SemanticError(CatalogErrorCodes.ProductMissing));
        }

        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        string? categoryPath = null;
        IReadOnlyList<CatalogEffectiveSchemaBinding> schema = Array.Empty<CatalogEffectiveSchemaBinding>();
        if (categoryId is Guid cid)
        {
            categoryPath = await BuildCategoryPathAsync(cid, normalizedLocale, cancellationToken);
            schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        }

        var values = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var valueByDef = values.ToDictionary(x => x.DefinitionId);

        var definitionIds = schema.Select(x => x.DefinitionId).ToArray();
        var names = await GetAttributeDefinitionNamesAsync(definitionIds, normalizedLocale, cancellationToken);
        var enumDefIds = schema
            .Where(x => x.Definition.ValueKind == CatalogAttributeValueKind.Enumeration)
            .Select(x => x.DefinitionId)
            .ToArray();
        var options = enumDefIds.Length == 0
            ? new List<CatalogAttributeOption>()
            : await _db.AttributeOptions.AsNoTracking()
                .Where(x => enumDefIds.Contains(x.DefinitionId))
                .OrderBy(x => x.Code)
                .ToListAsync(cancellationToken);
        var optionNames = await GetAttributeOptionNamesAsync(
            options.Select(x => x.OptionId).ToArray(),
            normalizedLocale,
            cancellationToken);
        var optionsByDef = options.GroupBy(x => x.DefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var fields = new List<ProductAttributeEditorField>();
        foreach (var entry in schema.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Definition.Code, StringComparer.Ordinal))
        {
            valueByDef.TryGetValue(entry.DefinitionId, out var stored);
            var optionViews = Array.Empty<ProductAttributeEditorOption>();
            if (optionsByDef.TryGetValue(entry.DefinitionId, out var defOptions))
            {
                optionViews = defOptions.Select(o => new ProductAttributeEditorOption(
                    o.OptionId,
                    optionNames.GetValueOrDefault(o.OptionId) ?? o.Code,
                    o.IsActive)).ToArray();
            }

            Guid? currentEnumOptionId = null;
            string? displayValue = null;
            if (stored is not null)
            {
                (currentEnumOptionId, displayValue) = FormatAttributeDisplay(
                    entry.Definition.ValueKind,
                    entry.Definition.IsMultivalue,
                    stored.CanonicalValue,
                    entry.Definition.Unit,
                    optionViews);
            }

            var isMissingRequired = entry.IsRequired
                && !entry.IsVariantAxis
                && entry.Definition.IsActive
                && stored is null;

            fields.Add(new ProductAttributeEditorField(
                entry.DefinitionId,
                entry.Definition.Code,
                names.GetValueOrDefault(entry.DefinitionId) ?? entry.Definition.Code,
                entry.Definition.ValueKind,
                entry.Definition.Unit,
                entry.IsRequired,
                entry.IsVariantAxis,
                entry.IsFilterable,
                entry.IsComparable,
                entry.Definition.IsMultivalue,
                entry.DisplayOrder,
                optionViews,
                stored?.CanonicalValue,
                currentEnumOptionId,
                displayValue,
                isMissingRequired));
        }

        var readiness = BuildReadiness(schema, values);
        return Result.Success(new ProductAttributeEditorState(productId, categoryId, categoryPath, fields, readiness));
    }

    /// <inheritdoc />
    public async Task<Result<ProductAttributeReadiness>> GetReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<ProductAttributeReadiness>(
                new SemanticError(CatalogErrorCodes.ProductMissing));
        }

        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        if (categoryId is not Guid cid)
        {
            return Result.Success(new ProductAttributeReadiness(true, Array.Empty<string>(), Array.Empty<string>()));
        }

        var schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        var values = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        return Result.Success(BuildReadiness(schema, values));
    }

    /// <inheritdoc />
    public async Task<Result> SetSingleAsync(
        Guid productId,
        Guid definitionId,
        string rawValue,
        Guid? enumOptionId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.ProductMissing));
        }

        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == definitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeMissing));
        }

        if (!definition.IsActive)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeDefinitionInactive));
        }

        var axisCheck = await EnsureNotEffectiveVariantAxisOnProductAsync(productId, definitionId, cancellationToken);
        if (axisCheck.IsFailure)
        {
            return axisCheck;
        }

        var schemaCheck = await EnsureDefinitionAllowedForProductSchemaAsync(productId, definitionId, cancellationToken);
        if (schemaCheck.IsFailure)
        {
            return schemaCheck;
        }

        if (definition.ValueKind == CatalogAttributeValueKind.Enumeration)
        {
            if (enumOptionId is not Guid optionId)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeEnumOptionRequired));
            }

            var option = await _db.AttributeOptions.SingleOrDefaultAsync(
                x => x.OptionId == optionId && x.DefinitionId == definitionId,
                cancellationToken);
            if (option is null)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeEnumOptionMismatch));
            }

            if (!option.IsActive)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeEnumOptionInactive));
            }
        }

        if (!TryCanonicalize(definition, rawValue, enumOptionId, out var canonical, out var canonError)
            || canonical is null)
        {
            return Result.Failure(canonError ?? new SemanticError(CatalogErrorCodes.AttributeValueInvalid));
        }

        var existing = await _db.ProductAttributeValues.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.DefinitionId == definitionId,
            cancellationToken);
        if (existing is null)
        {
            _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, definitionId, canonical));
        }
        else
        {
            _db.ProductAttributeValues.Remove(existing);
            _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, definitionId, canonical));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ProductAttributeEditorState>> SetBulkAsync(
        Guid productId,
        IReadOnlyList<ProductAttributeValueInput> values,
        string locale,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(values);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<ProductAttributeEditorState>(
                new SemanticError(CatalogErrorCodes.ProductMissing));
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var input in values)
            {
                var apply = await ApplyProductAttributeValueAsync(productId, input, cancellationToken);
                if (apply.IsFailure)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return Result.Failure<ProductAttributeEditorState>(apply.FirstError);
                }
            }

            QueueProductHistory(
                productId,
                ProductHistoryRules.EventAttributesChanged,
                ProductHistoryRules.SectionAttributes,
                ProductHistoryRules.SummaryAttributesFa,
                null,
                null);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetEditorStateAsync(productId, locale, cancellationToken);
    }

    private async Task<Result> ApplyProductAttributeValueAsync(
        Guid productId,
        ProductAttributeValueInput input,
        CancellationToken cancellationToken)
    {
        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == input.DefinitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeMissing));
        }

        if (!definition.IsActive)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeDefinitionInactive));
        }

        var axisCheck = await EnsureNotEffectiveVariantAxisOnProductAsync(productId, input.DefinitionId, cancellationToken);
        if (axisCheck.IsFailure)
        {
            return axisCheck;
        }

        var schemaCheck = await EnsureDefinitionAllowedForProductSchemaAsync(productId, input.DefinitionId, cancellationToken);
        if (schemaCheck.IsFailure)
        {
            return schemaCheck;
        }

        var existing = await _db.ProductAttributeValues.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.DefinitionId == input.DefinitionId,
            cancellationToken);

        if (input.Clear)
        {
            var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
            var isRequired = false;
            if (categoryId is Guid cid)
            {
                var schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
                isRequired = schema.Any(x => x.DefinitionId == input.DefinitionId && x.IsRequired && !x.IsVariantAxis);
            }

            if (isRequired)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeClearRequiredForbidden));
            }

            if (existing is not null)
            {
                _db.ProductAttributeValues.Remove(existing);
            }

            return Result.Success();
        }

        string canonical;
        if (definition.ValueKind == CatalogAttributeValueKind.Enumeration && definition.IsMultivalue)
        {
            var multi = await CanonicalizeMultivalueEnumerationAsync(
                definition.DefinitionId,
                input.RawValue,
                input.EnumOptionId,
                cancellationToken);
            if (multi.IsFailure)
            {
                return Result.Failure(multi.FirstError);
            }

            canonical = multi.Value;
        }
        else
        {
            if (definition.ValueKind == CatalogAttributeValueKind.Enumeration)
            {
                if (input.EnumOptionId is not Guid optionId)
                {
                    return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeEnumOptionRequired));
                }

                var option = await _db.AttributeOptions.SingleOrDefaultAsync(
                    x => x.OptionId == optionId && x.DefinitionId == definition.DefinitionId,
                    cancellationToken);
                if (option is null)
                {
                    return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeEnumOptionMismatch));
                }

                if (!option.IsActive)
                {
                    return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeEnumOptionInactive));
                }
            }

            var raw = input.RawValue;
            if (definition.ValueKind == CatalogAttributeValueKind.Enumeration)
            {
                raw = string.IsNullOrWhiteSpace(raw) ? "ignored" : raw;
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeValueEmpty));
            }

            if (!TryCanonicalize(definition, raw, input.EnumOptionId, out canonical!, out var canonError))
            {
                return Result.Failure(canonError!);
            }
        }

        if (existing is null)
        {
            _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, input.DefinitionId, canonical));
        }
        else
        {
            _db.ProductAttributeValues.Remove(existing);
            _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, input.DefinitionId, canonical));
        }

        return Result.Success();
    }

    private async Task<Result<string>> CanonicalizeMultivalueEnumerationAsync(
        Guid definitionId,
        string? rawValue,
        Guid? enumOptionId,
        CancellationToken cancellationToken)
    {
        var optionIds = new List<Guid>();
        if (enumOptionId is Guid single)
        {
            optionIds.Add(single);
        }

        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            foreach (var part in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Guid.TryParse(part, out var oid))
                {
                    return Result.Failure<string>(new SemanticError(CatalogErrorCodes.AttributeEnumOptionInvalid));
                }

                optionIds.Add(oid);
            }
        }

        optionIds = optionIds.Distinct().ToList();
        if (optionIds.Count == 0)
        {
            return Result.Failure<string>(new SemanticError(CatalogErrorCodes.AttributeEnumOptionRequired));
        }

        foreach (var optionId in optionIds)
        {
            var option = await _db.AttributeOptions.SingleOrDefaultAsync(
                x => x.OptionId == optionId && x.DefinitionId == definitionId,
                cancellationToken);
            if (option is null)
            {
                return Result.Failure<string>(new SemanticError(CatalogErrorCodes.AttributeEnumOptionMismatch));
            }

            if (!option.IsActive)
            {
                return Result.Failure<string>(new SemanticError(CatalogErrorCodes.AttributeEnumOptionInactive));
            }
        }

        return Result.Success(string.Join(",", optionIds.Select(id => id.ToString("N"))));
    }

    private static bool TryCanonicalize(
        CatalogAttributeDefinition definition,
        string rawValue,
        Guid? enumOptionId,
        out string? canonical,
        out SemanticError? error)
    {
        canonical = null;
        error = null;
        try
        {
            canonical = CatalogAttributeCanonicalizer.Canonicalize(definition.ValueKind, rawValue, enumOptionId);
        }
        catch (Exception)
        {
            error = new SemanticError(CatalogErrorCodes.AttributeValueInvalid);
            return false;
        }

        try
        {
            CatalogAttributeCanonicalizer.EnforceValidationBounds(definition, canonical);
        }
        catch (Exception)
        {
            error = new SemanticError(CatalogErrorCodes.AttributeValidationBounds);
            canonical = null;
            return false;
        }

        return true;
    }

    private static ProductAttributeReadiness BuildReadiness(
        IReadOnlyList<CatalogEffectiveSchemaBinding> schema,
        IReadOnlyList<CatalogProductAttributeValue> values)
    {
        var valueByDef = values.ToDictionary(x => x.DefinitionId);
        var missing = new List<string>();
        var invalid = new List<string>();

        foreach (var entry in schema)
        {
            if (entry.IsVariantAxis || !entry.Definition.IsActive)
            {
                continue;
            }

            valueByDef.TryGetValue(entry.DefinitionId, out var stored);
            if (entry.IsRequired && stored is null)
            {
                missing.Add(entry.Definition.Code);
                continue;
            }

            if (stored is null)
            {
                continue;
            }

            try
            {
                if (entry.Definition.ValueKind == CatalogAttributeValueKind.Enumeration)
                {
                    var parts = entry.Definition.IsMultivalue
                        ? stored.CanonicalValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        : [stored.CanonicalValue];
                    foreach (var part in parts)
                    {
                        if (!Guid.TryParse(part, out _))
                        {
                            invalid.Add(entry.Definition.Code);
                            break;
                        }
                    }
                }
                else
                {
                    CatalogAttributeCanonicalizer.EnforceValidationBounds(entry.Definition, stored.CanonicalValue);
                }
            }
            catch (Exception)
            {
                invalid.Add(entry.Definition.Code);
            }
        }

        return new ProductAttributeReadiness(missing.Count == 0 && invalid.Count == 0, missing, invalid);
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

    private async Task<Guid?> ResolvePrimaryCategoryIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var categoryId = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId && x.Role == CatalogProductCategoryRole.Primary)
            .Select(x => x.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);
        return categoryId == Guid.Empty ? null : categoryId;
    }

    private async Task<string> BuildCategoryPathAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var byId = categories.ToDictionary(x => x.CategoryId);
        var chain = new List<Guid>();
        var current = categoryId;
        var seen = new HashSet<Guid>();
        while (byId.TryGetValue(current, out var node) && seen.Add(current))
        {
            chain.Add(current);
            if (node.ParentCategoryId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        chain.Reverse();
        var names = await GetCategoryNamesAsync(chain, locale, cancellationToken);
        return string.Join(" > ", chain.Select(id => names.GetValueOrDefault(id) ?? "رده"));
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = categoryIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Category
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetAttributeOptionNamesAsync(
        IReadOnlyCollection<Guid> optionIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (optionIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = optionIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.AttributeOption
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
        var options = await _db.AttributeOptions.AsNoTracking()
            .Where(x => ids.Contains(x.OptionId))
            .Select(x => new { x.OptionId, x.Code })
            .ToListAsync(cancellationToken);
        foreach (var opt in options)
        {
            names.TryAdd(opt.OptionId, opt.Code);
        }

        return names;
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetAttributeDefinitionNamesAsync(
        IReadOnlyCollection<Guid> definitionIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (definitionIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = definitionIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.AttributeDefinition
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
        var definitions = await _db.AttributeDefinitions.AsNoTracking()
            .Where(x => ids.Contains(x.DefinitionId))
            .Select(x => new { x.DefinitionId, x.Code })
            .ToListAsync(cancellationToken);
        foreach (var def in definitions)
        {
            names.TryAdd(def.DefinitionId, def.Code);
        }

        return names;
    }

    private static (Guid? EnumOptionId, string? DisplayValue) FormatAttributeDisplay(
        CatalogAttributeValueKind kind,
        bool isMultivalue,
        string canonical,
        string? unit,
        IReadOnlyList<ProductAttributeEditorOption> options)
    {
        switch (kind)
        {
            case CatalogAttributeValueKind.Boolean:
                return (null, bool.TryParse(canonical, out var b) && b ? "بله" : "خیر");
            case CatalogAttributeValueKind.Number:
                return (null, string.IsNullOrWhiteSpace(unit) ? canonical : $"{canonical} {unit}");
            case CatalogAttributeValueKind.Enumeration:
            {
                var parts = isMultivalue
                    ? canonical.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : [canonical];
                var labels = new List<string>();
                Guid? singleId = null;
                foreach (var part in parts)
                {
                    if (!Guid.TryParse(part, out var oid))
                    {
                        labels.Add(part);
                        continue;
                    }

                    singleId ??= oid;
                    var label = options.FirstOrDefault(o => o.OptionId == oid)?.LocalizedLabel ?? oid.ToString("N");
                    labels.Add(label);
                }

                return (isMultivalue ? null : singleId, string.Join("، ", labels));
            }
            case CatalogAttributeValueKind.Instant:
                return (null, canonical);
            default:
                return (null, canonical);
        }
    }

    private async Task<Result> EnsureNotEffectiveVariantAxisOnProductAsync(
        Guid productId,
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        var primaryCategoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        if (primaryCategoryId is not Guid categoryId)
        {
            return Result.Success();
        }

        var schema = await ResolveEffectiveBindingsAsync(categoryId, cancellationToken);
        if (schema.Any(x => x.DefinitionId == definitionId && x.IsVariantAxis))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeVariantAxisOnProductForbidden));
        }

        return Result.Success();
    }

    private async Task<Result> EnsureDefinitionAllowedForProductSchemaAsync(
        Guid productId,
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        var primaryCategoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        if (primaryCategoryId is not Guid categoryId)
        {
            return Result.Success();
        }

        var allowed = new HashSet<Guid>();
        foreach (var entry in await ResolveEffectiveBindingsAsync(categoryId, cancellationToken))
        {
            allowed.Add(entry.DefinitionId);
        }

        if (allowed.Count > 0 && !allowed.Contains(definitionId))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeSchemaNotAllowed));
        }

        return Result.Success();
    }

    private async Task<IReadOnlyList<CatalogEffectiveSchemaBinding>> ResolveEffectiveBindingsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(x => x.CategoryId);
        var bindings = await _db.CategoryAttributeBindings.AsNoTracking().ToListAsync(cancellationToken);
        var definitions = await _db.AttributeDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        var definitionsById = definitions.ToDictionary(x => x.DefinitionId);
        return CatalogCategorySchemaResolver.ResolveEffectiveSchema(categoryId, categoriesById, bindings, definitionsById);
    }
}
