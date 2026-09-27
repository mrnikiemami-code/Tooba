using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Variants.Models;
using Tooba.Catalog.Application.Variants.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Catalog persistence for Product Variant Axes + Matrix Admin operations.</summary>
public sealed class ProductVariantDirectory : IProductVariantDirectory
{
    private const int MaxVariantCombinations = 200;

    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly ICatalogActorContext? _actor;

    /// <summary>Creates the directory.</summary>
    public ProductVariantDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        ICatalogActorContext? actor = null)
    {
        _db = db;
        _guard = guard;
        _actor = actor;
    }

    /// <inheritdoc />
    public async Task<Result> SetAxesAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(orderedDefinitionIds);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.ProductMissing));
        }

        if (orderedDefinitionIds.Distinct().Count() != orderedDefinitionIds.Count)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.VariantAxesDuplicate));
        }

        foreach (var definitionId in orderedDefinitionIds)
        {
            var definition = await _db.AttributeDefinitions
                .SingleOrDefaultAsync(x => x.DefinitionId == definitionId, cancellationToken);
            if (definition is null)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeMissing));
            }

            if (!definition.IsVariantAxisAllowed)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeVariantAxisCapabilityDisabled));
            }

            if (!definition.IsActive)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.AttributeDefinitionInactive));
            }
        }

        var primaryCategoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        if (primaryCategoryId is Guid schemaCategoryId)
        {
            var schema = await ResolveEffectiveBindingsAsync(schemaCategoryId, cancellationToken);
            foreach (var definitionId in orderedDefinitionIds)
            {
                if (!schema.Any(x => x.DefinitionId == definitionId && x.IsVariantAxis))
                {
                    return Result.Failure(new SemanticError(CatalogErrorCodes.VariantAxisSchemaNotEnabled));
                }
            }
        }

        var existing = await _db.ProductVariantAxes.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        _db.ProductVariantAxes.RemoveRange(existing);
        for (var i = 0; i < orderedDefinitionIds.Count; i++)
        {
            _db.ProductVariantAxes.Add(CatalogProductVariantAxis.Create(productId, orderedDefinitionIds[i], i));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ProductVariantEditorState>> GetEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<ProductVariantEditorState>(
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

        var axisBindings = schema
            .Where(x => x.IsVariantAxis && x.Definition.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Definition.Code, StringComparer.Ordinal)
            .ToList();

        string? messageFa = null;
        if (axisBindings.Count == 0)
        {
            messageFa = "برای این دسته‌بندی ویژگی تنوع تعریف نشده است.";
        }

        var axisDefIds = axisBindings.Select(x => x.DefinitionId).ToArray();
        var names = await GetAttributeDefinitionNamesAsync(axisDefIds, normalizedLocale, cancellationToken);
        var options = axisDefIds.Length == 0
            ? new List<CatalogAttributeOption>()
            : await _db.AttributeOptions.AsNoTracking()
                .Where(x => axisDefIds.Contains(x.DefinitionId))
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Code)
                .ToListAsync(cancellationToken);
        var optionNames = await GetAttributeOptionNamesAsync(
            options.Select(x => x.OptionId).ToArray(),
            normalizedLocale,
            cancellationToken);
        var optionsByDef = options.GroupBy(x => x.DefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var variants = await _db.Variants.AsNoTracking()
            .Include(x => x.AttributeValues)
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var selectedByDef = new Dictionary<Guid, HashSet<Guid>>();
        var labelDefIds = axisDefIds.ToHashSet();
        var labelOptionIds = options.Select(x => x.OptionId).ToHashSet();
        foreach (var variant in variants)
        {
            foreach (var av in variant.AttributeValues)
            {
                labelDefIds.Add(av.DefinitionId);
                if (Guid.TryParseExact(av.CanonicalValue, "N", out var optionId)
                    || Guid.TryParse(av.CanonicalValue, out optionId))
                {
                    labelOptionIds.Add(optionId);
                    if (variant.Status == CatalogPublicationStatus.Archived)
                    {
                        continue;
                    }

                    if (!selectedByDef.TryGetValue(av.DefinitionId, out var set))
                    {
                        set = [];
                        selectedByDef[av.DefinitionId] = set;
                    }

                    set.Add(optionId);
                }
            }
        }

        names = await GetAttributeDefinitionNamesAsync(labelDefIds.ToArray(), normalizedLocale, cancellationToken);
        optionNames = await GetAttributeOptionNamesAsync(labelOptionIds.ToArray(), normalizedLocale, cancellationToken);

        var axes = new List<ProductVariantAxisEditorField>();
        foreach (var binding in axisBindings)
        {
            optionsByDef.TryGetValue(binding.DefinitionId, out var defOptions);
            defOptions ??= [];
            var optionViews = defOptions.Select(o => new ProductVariantAxisOption(
                o.OptionId,
                optionNames.GetValueOrDefault(o.OptionId) ?? o.Code,
                o.Code,
                o.IsActive)).ToList();
            selectedByDef.TryGetValue(binding.DefinitionId, out var selected);
            axes.Add(new ProductVariantAxisEditorField(
                binding.DefinitionId,
                binding.Definition.Code,
                names.GetValueOrDefault(binding.DefinitionId) ?? binding.Definition.Code,
                binding.Definition.ValueKind,
                optionViews,
                selected?.ToList() ?? []));
        }

        var listItems = await MapVariantListItemsAsync(variants, names, optionNames, cancellationToken);
        var readinessResult = await GetReadinessAsync(productId, cancellationToken);
        if (readinessResult.IsFailure)
        {
            return Result.Failure<ProductVariantEditorState>(readinessResult.Errors);
        }

        return Result.Success(new ProductVariantEditorState(
            productId,
            categoryPath,
            axes,
            listItems,
            readinessResult.Value,
            MaxVariantCombinations,
            messageFa));
    }

    /// <inheritdoc />
    public async Task<Result<ProductVariantPreviewResult>> PreviewCombinationsAsync(
        Guid productId,
        IReadOnlyList<ProductVariantSelectedAxisInput> selectedAxes,
        string locale,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedAxes);
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var built = await BuildDesiredCombinationsAsync(productId, selectedAxes, normalizedLocale, cancellationToken);
        if (built.Error is not null)
        {
            return Result.Failure<ProductVariantPreviewResult>(built.Error);
        }

        var existing = await _db.Variants.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var byFingerprint = existing
            .GroupBy(x => x.CombinationFingerprint)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).First());

        var desiredSet = built.Combinations.Select(c => c.Fingerprint).ToHashSet(StringComparer.Ordinal);
        var previews = new List<ProductVariantCombinationPreview>();
        foreach (var combo in built.Combinations)
        {
            byFingerprint.TryGetValue(combo.Fingerprint, out var match);
            var action = match is null
                ? ProductVariantCombinationAction.New
                : match.Status == CatalogPublicationStatus.Archived
                    ? ProductVariantCombinationAction.New
                    : ProductVariantCombinationAction.Unchanged;
            previews.Add(new ProductVariantCombinationPreview(
                combo.Fingerprint,
                combo.Labels,
                match?.VariantId,
                action,
                null));
        }

        foreach (var variant in existing.Where(v => v.Status != CatalogPublicationStatus.Archived))
        {
            if (desiredSet.Contains(variant.CombinationFingerprint))
            {
                continue;
            }

            var labels = built.LabelLookup.TryGetValue(variant.CombinationFingerprint, out var cached)
                ? cached
                : await ResolveVariantAxisLabelsAsync(variant.VariantId, normalizedLocale, cancellationToken);
            previews.Add(new ProductVariantCombinationPreview(
                variant.CombinationFingerprint,
                labels,
                variant.VariantId,
                ProductVariantCombinationAction.Deactivate,
                null));
        }

        var unchanged = previews.Count(x => x.Action == ProductVariantCombinationAction.Unchanged);
        var neu = previews.Count(x => x.Action == ProductVariantCombinationAction.New);
        var deactivate = previews.Count(x => x.Action == ProductVariantCombinationAction.Deactivate);
        var messageFa =
            $"{ToPersianDigits(unchanged)} تنوع بدون تغییر · {ToPersianDigits(neu)} تنوع جدید · {ToPersianDigits(deactivate)} تنوع دیگر انتخاب نشده است";

        return Result.Success(new ProductVariantPreviewResult(
            previews,
            unchanged,
            neu,
            deactivate,
            built.Combinations.Count,
            built.Capped,
            built.WarningFa,
            messageFa));
    }

    /// <inheritdoc />
    public async Task<Result<ProductVariantApplyResult>> ApplyMatrixAsync(
        Guid productId,
        ProductVariantApplyInput input,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.SelectedAxes);

        var locale = string.IsNullOrWhiteSpace(input.Locale) ? "fa-IR" : input.Locale.Trim();
        var built = await BuildDesiredCombinationsAsync(productId, input.SelectedAxes, locale, cancellationToken);
        if (built.Error is not null)
        {
            return Result.Failure<ProductVariantApplyResult>(built.Error);
        }

        if (built.Capped)
        {
            return Result.Failure<ProductVariantApplyResult>(
                new SemanticError(CatalogErrorCodes.VariantCombinationLimitExceeded));
        }

        var now = DateTimeOffset.UtcNow;
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var axisDefIds = built.OrderedAxes.Select(x => x.DefinitionId).ToList();
            var existingAxes = await _db.ProductVariantAxes.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
            _db.ProductVariantAxes.RemoveRange(existingAxes);
            for (var i = 0; i < axisDefIds.Count; i++)
            {
                _db.ProductVariantAxes.Add(CatalogProductVariantAxis.Create(productId, axisDefIds[i], i));
            }

            var variants = await _db.Variants
                .Include(x => x.AttributeValues)
                .Where(x => x.ProductId == productId)
                .ToListAsync(cancellationToken);
            var byFingerprint = variants
                .GroupBy(x => x.CombinationFingerprint)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).First());

            var desiredSet = built.Combinations.Select(c => c.Fingerprint).ToHashSet(StringComparer.Ordinal);
            var created = 0;
            var unchanged = 0;
            var deactivated = 0;
            var sort = 0;

            foreach (var combo in built.Combinations)
            {
                if (byFingerprint.TryGetValue(combo.Fingerprint, out var existing))
                {
                    if (existing.Status == CatalogPublicationStatus.Archived)
                    {
                        existing.SetStatus(CatalogPublicationStatus.Draft, now);
                    }

                    existing.SetSortOrder(sort++, now);
                    unchanged++;
                    continue;
                }

                var variant = CatalogVariant.Create(productId, combo.Fingerprint, null, now);
                variant.SetSortOrder(sort++, now);
                foreach (var axis in combo.Axes)
                {
                    variant.AttributeValues.Add(
                        CatalogVariantAttributeValue.Create(variant.VariantId, axis.DefinitionId, axis.Canonical));
                }

                _db.Variants.Add(variant);
                variants.Add(variant);
                byFingerprint[combo.Fingerprint] = variant;
                created++;
            }

            foreach (var variant in variants.Where(v => v.Status != CatalogPublicationStatus.Archived).ToList())
            {
                if (desiredSet.Contains(variant.CombinationFingerprint))
                {
                    continue;
                }

                variant.SetStatus(CatalogPublicationStatus.Archived, now);
                deactivated++;
            }

            if (input.VariantPatches is { Count: > 0 })
            {
                var byId = variants.ToDictionary(x => x.VariantId);
                foreach (var patch in input.VariantPatches)
                {
                    if (!byId.TryGetValue(patch.VariantId, out var target))
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return Result.Failure<ProductVariantApplyResult>(
                            new SemanticError(CatalogErrorCodes.VariantPatchTargetMissing));
                    }

                    if (patch.Status is CatalogPublicationStatus status)
                    {
                        target.SetStatus(status, now);
                    }

                    if (patch.CatalogCodeSeam is not null)
                    {
                        target.UpdateCatalogCodeSeam(patch.CatalogCodeSeam, now);
                    }

                    if (patch.SortOrder is int order)
                    {
                        target.SetSortOrder(order, now);
                    }

                    if (patch.IsDefault is bool isDefault)
                    {
                        if (isDefault)
                        {
                            ClearDefaultFlags(variants, now);
                            target.SetDefault(true, now);
                        }
                        else
                        {
                            target.SetDefault(false, now);
                        }
                    }
                }
            }

            if (input.DefaultVariantId is Guid defaultId)
            {
                var target = variants.SingleOrDefault(x => x.VariantId == defaultId);
                if (target is null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return Result.Failure<ProductVariantApplyResult>(
                        new SemanticError(CatalogErrorCodes.VariantDefaultMissing));
                }

                if (target.Status == CatalogPublicationStatus.Archived)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return Result.Failure<ProductVariantApplyResult>(
                        new SemanticError(CatalogErrorCodes.VariantArchivedCannotBeDefault));
                }

                ClearDefaultFlags(variants, now);
                target.SetDefault(true, now);
            }
            else
            {
                EnforceSingleDefault(variants, now);
            }

            QueueProductHistory(
                productId,
                ProductHistoryRules.EventVariantsChanged,
                ProductHistoryRules.SectionVariants,
                ProductHistoryRules.SummaryVariantsFa,
                null,
                null);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var editor = await GetEditorStateAsync(productId, locale, cancellationToken);
            if (editor.IsFailure)
            {
                return Result.Failure<ProductVariantApplyResult>(editor.Errors);
            }

            return Result.Success(new ProductVariantApplyResult(
                created,
                unchanged,
                deactivated,
                editor.Value.Variants));
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<ProductVariantReadiness>> GetReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<ProductVariantReadiness>(
                new SemanticError(CatalogErrorCodes.ProductMissing));
        }

        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        var missingAxes = new List<string>();
        var invalidVariants = new List<string>();
        var duplicates = new List<string>();

        IReadOnlyList<CatalogEffectiveSchemaBinding> schema = Array.Empty<CatalogEffectiveSchemaBinding>();
        if (categoryId is Guid cid)
        {
            schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        }

        var effectiveAxes = schema.Where(x => x.IsVariantAxis && x.Definition.IsActive).ToList();
        var productAxes = await _db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

        if (effectiveAxes.Count > 0 && productAxes.Count == 0)
        {
            var hasActiveVariants = await _db.Variants.AsNoTracking()
                .AnyAsync(x => x.ProductId == productId && x.Status != CatalogPublicationStatus.Archived, cancellationToken);
            if (!hasActiveVariants)
            {
                // Axes available but matrix not applied yet — not a hard readiness failure until variants exist.
            }
        }

        foreach (var productAxis in productAxes)
        {
            var binding = effectiveAxes.FirstOrDefault(x => x.DefinitionId == productAxis.DefinitionId);
            if (binding is null)
            {
                missingAxes.Add(productAxis.DefinitionId.ToString("N"));
                continue;
            }

            if (binding.Definition.ValueKind != CatalogAttributeValueKind.Enumeration)
            {
                missingAxes.Add($"{binding.Definition.Code}:محور غیرگزینه‌ای");
            }
        }

        var variants = await _db.Variants.AsNoTracking()
            .Include(x => x.AttributeValues)
            .Where(x => x.ProductId == productId && x.Status != CatalogPublicationStatus.Archived)
            .ToListAsync(cancellationToken);

        var effectiveAxisIds = effectiveAxes.Select(x => x.DefinitionId).ToHashSet();
        foreach (var group in variants.GroupBy(x => x.CombinationFingerprint))
        {
            if (group.Count() > 1)
            {
                duplicates.Add(group.Key);
            }
        }

        foreach (var variant in variants)
        {
            var defs = variant.AttributeValues.Select(x => x.DefinitionId).ToHashSet();
            if (effectiveAxisIds.Count > 0 && !defs.SetEquals(effectiveAxisIds) && productAxes.Count > 0)
            {
                var selected = productAxes.Select(x => x.DefinitionId).ToHashSet();
                if (!defs.SetEquals(selected))
                {
                    invalidVariants.Add(variant.VariantId.ToString("N"));
                }
            }

            foreach (var av in variant.AttributeValues)
            {
                if (!Guid.TryParseExact(av.CanonicalValue, "N", out _)
                    && !Guid.TryParse(av.CanonicalValue, out _))
                {
                    invalidVariants.Add(variant.VariantId.ToString("N"));
                    break;
                }
            }
        }

        bool? noDefault = null;
        if (variants.Count > 0)
        {
            noDefault = !variants.Any(x => x.IsDefault);
        }

        var isValid = missingAxes.Count == 0
            && invalidVariants.Count == 0
            && duplicates.Count == 0
            && noDefault != true;

        return Result.Success(new ProductVariantReadiness(
            isValid,
            missingAxes,
            invalidVariants.Distinct().ToList(),
            duplicates,
            noDefault));
    }

    /// <inheritdoc />
    public async Task<Result<VariantReference>> CreateWorkspaceVariantAsync(
        Guid productId,
        string? catalogCodeSeam,
        IReadOnlyList<(Guid DefinitionId, string RawValue, Guid? EnumOptionId)> axes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(axes);
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<VariantReference>(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        if (axes.Count == 0)
        {
            return Result.Failure<VariantReference>(new SemanticError(CatalogErrorCodes.WorkspaceVariantAxesMissing));
        }

        try
        {
            var selectedAxes = await _db.ProductVariantAxes.AsNoTracking()
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.DefinitionId)
                .ToListAsync(cancellationToken);
            if (selectedAxes.Count > 0)
            {
                var selectedSet = selectedAxes.ToHashSet();
                var axisDefs = axes.Select(a => a.DefinitionId).ToHashSet();
                if (!axisDefs.SetEquals(selectedSet))
                {
                    return Result.Failure<VariantReference>(
                        new SemanticError(CatalogErrorCodes.WorkspaceVariantCreateRejected));
                }
            }

            var normalized = new List<(Guid DefinitionId, string Canonical)>();
            foreach (var axis in axes)
            {
                var definition = await _db.AttributeDefinitions
                    .SingleOrDefaultAsync(x => x.DefinitionId == axis.DefinitionId, cancellationToken);
                if (definition is null)
                {
                    return Result.Failure<VariantReference>(
                        new SemanticError(CatalogErrorCodes.WorkspaceVariantCreateRejected));
                }

                if (!definition.IsVariantAxis)
                {
                    return Result.Failure<VariantReference>(
                        new SemanticError(CatalogErrorCodes.WorkspaceVariantCreateRejected));
                }

                if (definition.ValueKind == CatalogAttributeValueKind.Enumeration && axis.EnumOptionId is Guid optionId)
                {
                    var option = await _db.AttributeOptions.SingleOrDefaultAsync(
                        x => x.OptionId == optionId && x.DefinitionId == definition.DefinitionId,
                        cancellationToken);
                    if (option is null || !option.IsActive)
                    {
                        return Result.Failure<VariantReference>(
                            new SemanticError(CatalogErrorCodes.WorkspaceVariantCreateRejected));
                    }
                }

                var canonical = CatalogAttributeCanonicalizer.Canonicalize(
                    definition.ValueKind,
                    axis.RawValue,
                    axis.EnumOptionId);
                CatalogAttributeCanonicalizer.EnforceValidationBounds(definition, canonical);
                normalized.Add((definition.DefinitionId, canonical));
            }

            var fingerprint = CatalogVariant.ComputeFingerprint(normalized);
            if (await _db.Variants.AnyAsync(
                    x => x.ProductId == productId && x.CombinationFingerprint == fingerprint,
                    cancellationToken))
            {
                return Result.Failure<VariantReference>(
                    new SemanticError(CatalogErrorCodes.WorkspaceVariantCreateRejected));
            }

            var variant = CatalogVariant.Create(productId, fingerprint, catalogCodeSeam, DateTimeOffset.UtcNow);
            foreach (var item in normalized)
            {
                variant.AttributeValues.Add(
                    CatalogVariantAttributeValue.Create(variant.VariantId, item.DefinitionId, item.Canonical));
            }

            _db.Variants.Add(variant);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success(new VariantReference(
                variant.VariantId,
                variant.ProductId,
                variant.CombinationFingerprint,
                variant.Status));
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<VariantReference>(
                new SemanticError(CatalogErrorCodes.WorkspaceVariantCreateRejected));
        }
    }

    /// <inheritdoc />
    public async Task<Result> PatchWorkspaceVariantAsync(
        Guid productId,
        Guid variantId,
        string? status,
        string? catalogCodeSeam,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        var variant = await _db.Variants.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.VariantId == variantId,
            cancellationToken);
        if (variant is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceVariantMissing));
        }

        if (!ProductVariantPatchStatusMapper.TryParse(status, out var parsedStatus))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceVariantStatusInvalid));
        }

        var now = DateTimeOffset.UtcNow;
        if (parsedStatus is CatalogPublicationStatus domainStatus)
        {
            variant.SetStatus(domainStatus, now);
        }

        if (catalogCodeSeam is not null)
        {
            variant.UpdateCatalogCodeSeam(catalogCodeSeam, now);
        }

        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        product.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private sealed record DesiredAxisValue(Guid DefinitionId, string Canonical, string DefinitionName, string ValueLabel);

    private sealed record DesiredCombination(
        string Fingerprint,
        IReadOnlyList<DesiredAxisValue> Axes,
        IReadOnlyList<ProductVariantAxisLabel> Labels);

    private sealed record DesiredCombinationBuild(
        IReadOnlyList<(Guid DefinitionId, int DisplayOrder, CatalogAttributeDefinition Definition)> OrderedAxes,
        IReadOnlyList<DesiredCombination> Combinations,
        IReadOnlyDictionary<string, IReadOnlyList<ProductVariantAxisLabel>> LabelLookup,
        bool Capped,
        string? WarningFa,
        SemanticError? Error);

    private async Task<DesiredCombinationBuild> BuildDesiredCombinationsAsync(
        Guid productId,
        IReadOnlyList<ProductVariantSelectedAxisInput> selectedAxes,
        string locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return FailBuild(CatalogErrorCodes.ProductMissing);
        }

        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        if (categoryId is not Guid cid)
        {
            return FailBuild(CatalogErrorCodes.VariantEffectiveAxesMissing);
        }

        var schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        var effectiveAxes = schema
            .Where(x => x.IsVariantAxis && x.Definition.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Definition.Code, StringComparer.Ordinal)
            .ToList();
        if (effectiveAxes.Count == 0)
        {
            return FailBuild(CatalogErrorCodes.VariantEffectiveAxesMissing);
        }

        var effectiveById = effectiveAxes.ToDictionary(x => x.DefinitionId);
        var orderedInputs = new List<(CatalogEffectiveSchemaBinding Binding, IReadOnlyList<Guid> OptionIds)>();
        foreach (var input in selectedAxes)
        {
            if (!effectiveById.TryGetValue(input.DefinitionId, out var binding))
            {
                return FailBuild(CatalogErrorCodes.VariantAxisSchemaNotEnabled);
            }

            if (binding.Definition.ValueKind != CatalogAttributeValueKind.Enumeration)
            {
                return FailBuild(CatalogErrorCodes.AttributeVariantAxisValueKindInvalid);
            }

            var optionIds = (input.OptionIds ?? Array.Empty<Guid>()).Distinct().ToList();
            if (optionIds.Count == 0)
            {
                continue;
            }

            orderedInputs.Add((binding, optionIds));
        }

        orderedInputs = orderedInputs
            .OrderBy(x => x.Binding.DisplayOrder)
            .ThenBy(x => x.Binding.Definition.Code, StringComparer.Ordinal)
            .ToList();

        if (orderedInputs.Count == 0)
        {
            return new DesiredCombinationBuild(
                effectiveAxes.Select(x => (x.DefinitionId, x.DisplayOrder, x.Definition)).ToList(),
                [],
                new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(),
                false,
                null,
                null);
        }

        var allOptionIds = orderedInputs.SelectMany(x => x.OptionIds).Distinct().ToArray();
        var options = await _db.AttributeOptions.AsNoTracking()
            .Where(x => allOptionIds.Contains(x.OptionId))
            .ToListAsync(cancellationToken);
        var optionsById = options.ToDictionary(x => x.OptionId);
        var optionNames = await GetAttributeOptionNamesAsync(allOptionIds, locale, cancellationToken);
        var defNames = await GetAttributeDefinitionNamesAsync(
            orderedInputs.Select(x => x.Binding.DefinitionId).ToArray(),
            locale,
            cancellationToken);

        foreach (var (binding, optionIds) in orderedInputs)
        {
            foreach (var optionId in optionIds)
            {
                if (!optionsById.TryGetValue(optionId, out var option) || option.DefinitionId != binding.DefinitionId)
                {
                    return FailBuild(CatalogErrorCodes.AttributeEnumOptionMismatch);
                }

                if (!option.IsActive)
                {
                    return FailBuild(CatalogErrorCodes.AttributeEnumOptionInactive);
                }
            }
        }

        var axisOptionLists = orderedInputs.Select(entry =>
        {
            var orderedOptions = entry.OptionIds
                .Select(id => optionsById[id])
                .OrderBy(o => o.DisplayOrder)
                .ThenBy(o => o.Code, StringComparer.Ordinal)
                .ToList();
            return (entry.Binding, Options: orderedOptions);
        }).ToList();

        long total = 1;
        foreach (var axis in axisOptionLists)
        {
            total *= axis.Options.Count;
            if (total > MaxVariantCombinations)
            {
                break;
            }
        }

        var capped = total > MaxVariantCombinations;
        var warningFa = capped
            ? $"تعداد ترکیب‌ها ({ToPersianDigits((int)Math.Min(total, int.MaxValue))}) از سقف امن {ToPersianDigits(MaxVariantCombinations)} بیشتر است. لطفاً گزینه‌های کمتری انتخاب کنید."
            : null;

        var combinations = new List<DesiredCombination>();
        if (!capped)
        {
            IEnumerable<IReadOnlyList<CatalogAttributeOption>> seed = [Array.Empty<CatalogAttributeOption>()];
            foreach (var axis in axisOptionLists)
            {
                seed = seed.SelectMany(prefix => axis.Options.Select(opt =>
                {
                    var next = new List<CatalogAttributeOption>(prefix.Count + 1);
                    next.AddRange(prefix);
                    next.Add(opt);
                    return (IReadOnlyList<CatalogAttributeOption>)next;
                }));
            }

            foreach (var comboOptions in seed)
            {
                var axes = new List<DesiredAxisValue>();
                for (var i = 0; i < comboOptions.Count; i++)
                {
                    var binding = axisOptionLists[i].Binding;
                    var option = comboOptions[i];
                    var canonical = option.OptionId.ToString("N");
                    axes.Add(new DesiredAxisValue(
                        binding.DefinitionId,
                        canonical,
                        defNames.GetValueOrDefault(binding.DefinitionId) ?? binding.Definition.Code,
                        optionNames.GetValueOrDefault(option.OptionId) ?? option.Code));
                }

                var fingerprint = CatalogVariant.ComputeFingerprint(axes.Select(a => (a.DefinitionId, a.Canonical)));
                var labels = axes.Select(a => new ProductVariantAxisLabel(a.DefinitionName, a.ValueLabel)).ToList();
                combinations.Add(new DesiredCombination(fingerprint, axes, labels));
            }
        }

        var labelLookup = combinations.ToDictionary(
            c => c.Fingerprint,
            c => c.Labels,
            StringComparer.Ordinal);

        return new DesiredCombinationBuild(
            orderedInputs.Select(x => (x.Binding.DefinitionId, x.Binding.DisplayOrder, x.Binding.Definition)).ToList(),
            combinations,
            labelLookup,
            capped,
            warningFa,
            null);
    }

    private static DesiredCombinationBuild FailBuild(string code) =>
        new([], [], new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(), false, null, new SemanticError(code));

    private async Task<IReadOnlyList<ProductVariantListItem>> MapVariantListItemsAsync(
        IReadOnlyList<CatalogVariant> variants,
        IReadOnlyDictionary<Guid, string> definitionNames,
        IReadOnlyDictionary<Guid, string> optionNames,
        CancellationToken cancellationToken)
    {
        var items = new List<ProductVariantListItem>();
        foreach (var variant in variants)
        {
            var labels = new List<ProductVariantAxisLabel>();
            foreach (var av in variant.AttributeValues.OrderBy(x => x.DefinitionId))
            {
                var defName = definitionNames.GetValueOrDefault(av.DefinitionId) ?? av.DefinitionId.ToString("N");
                string valueLabel = av.CanonicalValue;
                if (Guid.TryParseExact(av.CanonicalValue, "N", out var oid)
                    || Guid.TryParse(av.CanonicalValue, out oid))
                {
                    valueLabel = optionNames.GetValueOrDefault(oid) ?? av.CanonicalValue;
                }

                labels.Add(new ProductVariantAxisLabel(defName, valueLabel));
            }

            items.Add(new ProductVariantListItem(
                variant.VariantId,
                variant.CombinationFingerprint,
                variant.Status,
                variant.SortOrder,
                variant.IsDefault,
                variant.CatalogCodeSeam,
                labels,
                null));
        }

        await Task.CompletedTask;
        return items;
    }

    private async Task<IReadOnlyList<ProductVariantAxisLabel>> ResolveVariantAxisLabelsAsync(
        Guid variantId,
        string locale,
        CancellationToken cancellationToken)
    {
        var values = await _db.Set<CatalogVariantAttributeValue>().AsNoTracking()
            .Where(x => x.VariantId == variantId)
            .ToListAsync(cancellationToken);
        var defIds = values.Select(x => x.DefinitionId).ToArray();
        var names = await GetAttributeDefinitionNamesAsync(defIds, locale, cancellationToken);
        var optionIds = new List<Guid>();
        foreach (var v in values)
        {
            if (Guid.TryParseExact(v.CanonicalValue, "N", out var oid)
                || Guid.TryParse(v.CanonicalValue, out oid))
            {
                optionIds.Add(oid);
            }
        }

        var optionNames = await GetAttributeOptionNamesAsync(optionIds, locale, cancellationToken);
        return values
            .OrderBy(x => x.DefinitionId)
            .Select(v =>
            {
                var defName = names.GetValueOrDefault(v.DefinitionId) ?? v.DefinitionId.ToString("N");
                var valueLabel = v.CanonicalValue;
                if (Guid.TryParseExact(v.CanonicalValue, "N", out var oid)
                    || Guid.TryParse(v.CanonicalValue, out oid))
                {
                    valueLabel = optionNames.GetValueOrDefault(oid) ?? v.CanonicalValue;
                }

                return new ProductVariantAxisLabel(defName, valueLabel);
            })
            .ToList();
    }

    private static void ClearDefaultFlags(IEnumerable<CatalogVariant> variants, DateTimeOffset now)
    {
        foreach (var variant in variants.Where(x => x.IsDefault))
        {
            variant.SetDefault(false, now);
        }
    }

    private static void EnforceSingleDefault(IReadOnlyList<CatalogVariant> variants, DateTimeOffset now)
    {
        var activeDefaults = variants
            .Where(x => x.IsDefault && x.Status != CatalogPublicationStatus.Archived)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToList();
        if (activeDefaults.Count <= 1)
        {
            return;
        }

        foreach (var extra in activeDefaults.Skip(1))
        {
            extra.SetDefault(false, now);
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

    private static string ToPersianDigits(int value)
    {
        var s = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.Create(s.Length, s, static (span, src) =>
        {
            for (var i = 0; i < src.Length; i++)
            {
                var c = src[i];
                span[i] = c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c;
            }
        });
    }
}
