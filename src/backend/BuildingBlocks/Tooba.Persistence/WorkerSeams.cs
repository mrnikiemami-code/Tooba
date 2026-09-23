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

/// <summary>
/// Generic platform registry for the last outcome of a background worker cycle.
/// Modules record by stable worker name; Host owns process-level storage and exposure.
/// </summary>
public interface IBackgroundWorkerRegistry
{
    /// <summary>Records a successful worker cycle.</summary>
    void RecordSuccess(string workerName, int processedCount);

    /// <summary>Records a failed worker cycle.</summary>
    void RecordFailure(string workerName, string errorType);
}
