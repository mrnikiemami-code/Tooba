using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.ProductPublishing.Ports;

/// <summary>Catalog-owned product lifecycle mutations (publish / unpublish / archive / restore).</summary>
public interface IProductLifecycleDirectory
{
    /// <summary>Publishes a product when readiness allows; idempotent if already published.</summary>
    Task<Result> PublishAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>Unpublishes a published product to draft.</summary>
    Task<Result> UnpublishAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>Archives a product.</summary>
    Task<Result> ArchiveAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>Restores an archived product to draft.</summary>
    Task<Result> RestoreAsync(Guid productId, CancellationToken cancellationToken);
}
