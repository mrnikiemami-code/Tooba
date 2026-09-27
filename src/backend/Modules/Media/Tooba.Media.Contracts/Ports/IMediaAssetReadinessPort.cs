namespace Tooba.Media.Contracts.Ports;

/// <summary>Narrow Media readiness check for foreign modules (Content cover/gallery refs).</summary>
public interface IMediaAssetReadinessPort
{
    /// <summary>Ensures a Ready media asset exists; throws when missing (stable Media/Content code at call site).</summary>
    Task EnsureReadyAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}
