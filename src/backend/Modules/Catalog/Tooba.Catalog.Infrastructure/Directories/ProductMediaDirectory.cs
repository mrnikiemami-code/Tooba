using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductMedia.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for Admin product media editor, readiness, and mutations.</summary>
public sealed class ProductMediaDirectory : IProductMediaDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly ICatalogActorContext? _actor;

    /// <summary>Creates the directory.</summary>
    public ProductMediaDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        ICatalogActorContext? actor = null)
    {
        _db = db;
        _guard = guard;
        _actor = actor;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductMediaAssignment>>> ListAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<ProductMediaAssignment>>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        var items = await LoadOrderedMediaAssignmentsAsync(productId, cancellationToken);
        return Result.Success(items);
    }

    /// <inheritdoc />
    public async Task<Result<ProductMediaReadiness>> GetReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure<ProductMediaReadiness>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        var items = await LoadOrderedMediaAssignmentsAsync(productId, cancellationToken);
        return Result.Success(BuildMediaReadiness(items));
    }

    /// <inheritdoc />
    public async Task<Result> AttachReferenceAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (mediaAssetId == Guid.Empty)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaAssetMissing));
        }

        // Host Composer mapped all AttachMediaReference IOEs (including product missing) to attach.rejected.
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaAttachRejected));
        }

        if (await _db.MediaReferences.AnyAsync(
                x => x.ProductId == productId && x.MediaAssetId == mediaAssetId,
                cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaAttachRejected));
        }

        var existing = await _db.MediaReferences
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var maxOrder = existing.Count == 0 ? -1 : existing.Max(x => x.DisplayOrder);
        var isFirst = existing.Count == 0;
        var link = CatalogProductMediaReference.Link(
            productId,
            mediaAssetId,
            displayOrder: maxOrder + 1,
            isPrimary: isFirst,
            altText: altText);
        existing.Add(link);
        _db.MediaReferences.Add(link);
        EnforcePrimaryUniqueness(existing);
        TouchProductUpdatedAt(productId);
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventMediaChanged,
            ProductHistoryRules.SectionMedia,
            ProductHistoryRules.SummaryMediaFa,
            null,
            null);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> AttachPlaceholderAsync(
        Guid productId,
        string? altText,
        CancellationToken cancellationToken)
    {
        var assetId = UuidV7.New();
        var attached = await AttachReferenceAsync(productId, assetId, altText, cancellationToken);
        if (attached.IsFailure)
        {
            // Host Composer mapped every placeholder IOE to placeholder.rejected (400).
            return Result.Failure<Guid>(new SemanticError(CatalogErrorCodes.WorkspaceMediaPlaceholderRejected));
        }

        return Result.Success(assetId);
    }

    /// <inheritdoc />
    public async Task<Result> ReorderAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(orderedMediaAssetIds);
        // Host Composer mapped product-missing IOE (no Persian empty/order substring) to order.rejected.
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaOrderRejected));
        }

        var media = await _db.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        if (media.Count == 0)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaEmpty));
        }

        var existing = media.Select(x => x.MediaAssetId).ToHashSet();
        if (orderedMediaAssetIds.Count != existing.Count
            || orderedMediaAssetIds.Any(id => !existing.Contains(id))
            || orderedMediaAssetIds.Distinct().Count() != orderedMediaAssetIds.Count)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaOrderInvalid));
        }

        for (var i = 0; i < orderedMediaAssetIds.Count; i++)
        {
            media.Single(m => m.MediaAssetId == orderedMediaAssetIds[i]).DisplayOrder = i;
        }

        EnforcePrimaryUniqueness(media);
        TouchProductUpdatedAt(productId);
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventMediaChanged,
            ProductHistoryRules.SectionMedia,
            ProductHistoryRules.SummaryMediaFa,
            null,
            null);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SetPrimaryAsync(
        Guid productId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        // Host Composer mapped all SetPrimary IOEs (product or target) to workspace.media.missing.
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaMissing));
        }

        var media = await _db.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var target = media.SingleOrDefault(x => x.MediaAssetId == mediaAssetId);
        if (target is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaMissing));
        }

        foreach (var row in media)
        {
            row.IsPrimary = row.MediaAssetId == target.MediaAssetId;
        }

        TouchProductUpdatedAt(productId);
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventMediaChanged,
            ProductHistoryRules.SectionMedia,
            ProductHistoryRules.SummaryMediaPrimaryFa,
            null,
            null);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> PatchAltAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var row = await _db.MediaReferences.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.MediaAssetId == mediaAssetId,
            cancellationToken);
        if (row is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaMissing));
        }

        row.AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        TouchProductUpdatedAt(productId);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DetachAsync(
        Guid productId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        // Host Composer mapped all Detach IOEs (product or target) to workspace.media.missing.
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaMissing));
        }

        var media = await _db.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var row = media.SingleOrDefault(x => x.MediaAssetId == mediaAssetId);
        if (row is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceMediaMissing));
        }

        _db.MediaReferences.Remove(row);
        media.Remove(row);
        EnforcePrimaryUniqueness(media);
        TouchProductUpdatedAt(productId);
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventMediaChanged,
            ProductHistoryRules.SectionMedia,
            ProductHistoryRules.SummaryMediaFa,
            null,
            null);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<IReadOnlyList<ProductMediaAssignment>> LoadOrderedMediaAssignmentsAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var media = await _db.MediaReferences.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ReferenceId)
            .ToListAsync(cancellationToken);
        return media
            .Select(m => new ProductMediaAssignment(m.MediaAssetId, m.IsPrimary, m.DisplayOrder, m.AltText))
            .ToList();
    }

    private static ProductMediaReadiness BuildMediaReadiness(IReadOnlyList<ProductMediaAssignment> items)
    {
        var count = items.Count;
        var hasPrimary = items.Any(x => x.IsPrimary);
        var isReady = count > 0 && hasPrimary;
        string? messageFa;
        if (count == 0 || !hasPrimary)
        {
            messageFa = "تصویر اصلی تعیین نشده";
        }
        else if (isReady)
        {
            messageFa = "رسانه کامل است";
        }
        else
        {
            messageFa = null;
        }

        return new ProductMediaReadiness(hasPrimary, count, isReady, messageFa);
    }

    /// <summary>
    /// Exactly one IsPrimary when count&gt;0; otherwise first by DisplayOrder.
    /// </summary>
    private static void EnforcePrimaryUniqueness(IList<CatalogProductMediaReference> media)
    {
        if (media.Count == 0)
        {
            return;
        }

        var ordered = media.OrderBy(x => x.DisplayOrder).ThenBy(x => x.ReferenceId).ToList();
        var primaries = ordered.Where(x => x.IsPrimary).ToList();
        CatalogProductMediaReference keep;
        if (primaries.Count == 1)
        {
            keep = primaries[0];
        }
        else if (primaries.Count == 0)
        {
            keep = ordered[0];
        }
        else
        {
            keep = primaries[0];
        }

        foreach (var row in media)
        {
            row.IsPrimary = ReferenceEquals(row, keep) || row.ReferenceId == keep.ReferenceId;
        }
    }

    private void TouchProductUpdatedAt(Guid productId)
    {
        var product = _db.Products.Local.SingleOrDefault(x => x.ProductId == productId)
            ?? _db.Products.Single(x => x.ProductId == productId);
        product.UpdatedAt = DateTimeOffset.UtcNow;
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
