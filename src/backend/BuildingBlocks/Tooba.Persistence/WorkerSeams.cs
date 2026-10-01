namespace Tooba.Persistence;

/// <summary>
/// Generic platform worker seams consumed by module-owned background workers.
/// Host supplies the process-level implementations; modules stay free of Host references.
/// </summary>
public interface IWorkerCommerceContextFactory
{
    /// <summary>
    /// Rebuilds a <see cref="Tooba.BuildingBlocks.CommerceContext"/> for a poll target without
    /// reading HTTP headers. Host owns the control-plane registry behind this seam.
    /// </summary>
    Tooba.BuildingBlocks.CommerceContext FromPollTarget(OutboxPollTarget target, string traceId);
}
