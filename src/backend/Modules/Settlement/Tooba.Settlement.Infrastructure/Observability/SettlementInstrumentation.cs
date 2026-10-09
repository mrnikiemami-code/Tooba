using System.Diagnostics.Metrics;
using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Infrastructure.Observability;

/// <summary>
/// متریک‌های سبک Settlement روی Meter واحد ToobaTelemetry (بدون PII و بدون pipeline موازی).
/// </summary>
public sealed class SettlementInstrumentation
{
    private readonly Counter<long> _entryPosted;
    private readonly Counter<long> _payoutSucceeded;
    private readonly Counter<long> _payoutFailed;

    /// <summary>
    /// متریک‌های in-process Settlement.
    /// </summary>
    public SettlementInstrumentation()
    {
        var meter = ToobaTelemetry.Meter;
        _entryPosted = meter.CreateCounter<long>("tooba.settlement.entry.posted");
        _payoutSucceeded = meter.CreateCounter<long>("tooba.settlement.payout.succeeded");
        _payoutFailed = meter.CreateCounter<long>("tooba.settlement.payout.failed");
    }

    /// <summary>posted entry ثبت می‌کند.</summary>
    public void RecordEntryPosted() => _entryPosted.Add(1);

    /// <summary>payout موفق ثبت می‌کند.</summary>
    public void RecordPayoutSucceeded() => _payoutSucceeded.Add(1);

    /// <summary>payout شکست‌خورده ثبت می‌کند.</summary>
    public void RecordPayoutFailed() => _payoutFailed.Add(1);
}
