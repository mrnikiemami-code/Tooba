namespace Tooba.BuildingBlocks.Observability.Correlation;

/// <summary>
/// قرارداد فقط‌خواندنی برای CorrelationId جاری بدون وابستگی HTTP.
/// </summary>
public interface ICorrelationContext
{
    /// <summary>شناسه همبستگی نرمال‌شده (فرمت N) یا تهی.</summary>
    string? CorrelationId { get; }
}
