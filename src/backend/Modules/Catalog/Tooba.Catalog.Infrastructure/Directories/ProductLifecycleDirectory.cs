using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductPublishing.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for Admin product lifecycle mutations.</summary>
public sealed class ProductLifecycleDirectory : IProductLifecycleDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly IProductPublishReadinessReader _readiness;
    private readonly ICatalogActorContext? _actor;

    /// <summary>Creates the directory.</summary>
    public ProductLifecycleDirectory(
        CatalogDbContext db,
        ICatalogUseCaseGuard guard,
        IProductPublishReadinessReader readiness,
        ICatalogActorContext? actor = null)
    {
        _db = db;
        _guard = guard;
        _readiness = readiness;
        _actor = actor;
    }

    /// <inheritdoc />
    public async Task<Result> PublishAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        if (product.Status == CatalogPublicationStatus.Published)
        {
            return Result.Success();
        }

        if (product.Status == CatalogPublicationStatus.Archived)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductPublishRejected));
        }

        var readiness = await _readiness.GetAsync(productId, "fa-IR", cancellationToken);
        if (readiness.IsFailure)
        {
            return Result.Failure(readiness.Errors);
        }

        if (!readiness.Value.IsReady)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductPublishRejected));
        }

        try
        {
            product.Publish(DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductPublishRejected));
        }

        QueueProductHistory(
            productId,
            ProductHistoryRules.EventPublished,
            ProductHistoryRules.SectionLifecycle,
            ProductHistoryRules.SummaryPublishedFa,
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft),
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Published));
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UnpublishAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        try
        {
            product.Unpublish(DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductUnpublishRejected));
        }

        QueueProductHistory(
            productId,
            ProductHistoryRules.EventUnpublished,
            ProductHistoryRules.SectionLifecycle,
            ProductHistoryRules.SummaryUnpublishedFa,
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Published),
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft));
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ArchiveAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        var before = ProductPublishRules.LifecycleLabelFa(product.Status);
        try
        {
            product.Archive(DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductArchiveRejected));
        }

        QueueProductHistory(
            productId,
            ProductHistoryRules.EventArchived,
            ProductHistoryRules.SectionLifecycle,
            ProductHistoryRules.SummaryArchivedFa,
            before,
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Archived));
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RestoreAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        try
        {
            product.RestoreFromArchive(DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceProductRestoreRejected));
        }

        QueueProductHistory(
            productId,
            ProductHistoryRules.EventRestored,
            ProductHistoryRules.SectionLifecycle,
            ProductHistoryRules.SummaryRestoredFa,
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Archived),
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft));
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
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
