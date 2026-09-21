namespace Tooba.BuildingBlocks.Observability.Correlation;

/// <summary>Adapter فقط‌خواندنی برای لاگ و enrich بدون set.</summary>
public sealed class CorrelationContextAdapter : ICorrelationContext
{
    /// <inheritdoc />
    public string? CorrelationId => CorrelationIdContext.Current;
}
