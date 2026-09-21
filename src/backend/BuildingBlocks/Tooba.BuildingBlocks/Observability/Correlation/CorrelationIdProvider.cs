namespace Tooba.BuildingBlocks.Observability.Correlation;

/// <summary>Facade قابل تزریق روی <see cref="CorrelationIdContext"/>.</summary>
public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    /// <inheritdoc />
    public string? GetCorrelationId() => CorrelationIdContext.Current;

    /// <inheritdoc />
    public Guid? GetCorrelationGuid() => CorrelationIdContext.CurrentGuid;

    /// <inheritdoc />
    public string EnsureCorrelationId(string? incoming = null) => CorrelationIdContext.Ensure(incoming);

    /// <inheritdoc />
    public void SetCorrelationId(string correlationId) => CorrelationIdContext.Set(correlationId);
}
