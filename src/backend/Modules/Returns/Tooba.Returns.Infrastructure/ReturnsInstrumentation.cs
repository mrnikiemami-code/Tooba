using System.Diagnostics.Metrics;
using Tooba.BuildingBlocks;

namespace Tooba.Returns.Infrastructure;

/// <summary>
/// متریک‌های مرجوعی بدون PII.
/// </summary>
public sealed class ReturnsInstrumentation
{
    private readonly Counter<long> _created;
    private readonly Counter<long> _approved;
    private readonly Counter<long> _rejected;
    private readonly Counter<long> _retry;
    private readonly Counter<long> _refundSucceeded;
    private readonly Counter<long> _refundFailed;

    /// <summary>
    /// متریک‌های in-process Returns.
    /// </summary>
    public ReturnsInstrumentation()
    {
        var meter = ToobaTelemetry.Meter;
        _created = meter.CreateCounter<long>("tooba.returns.created");
        _approved = meter.CreateCounter<long>("tooba.returns.approved");
        _rejected = meter.CreateCounter<long>("tooba.returns.rejected");
        _retry = meter.CreateCounter<long>("tooba.returns.refund.retry");
        _refundSucceeded = meter.CreateCounter<long>("tooba.returns.refund.succeeded");
        _refundFailed = meter.CreateCounter<long>("tooba.returns.refund.failed");
    }

    /// <summary>ثبت ایجاد درخواست.</summary>
    public void RecordCreated() => _created.Add(1);

    /// <summary>ثبت تأیید.</summary>
    public void RecordApproved() => _approved.Add(1);

    /// <summary>ثبت رد.</summary>
    public void RecordRejected() => _rejected.Add(1);

    /// <summary>ثبت retry refund.</summary>
    public void RecordRetry() => _retry.Add(1);

    /// <summary>ثبت refund موفق.</summary>
    public void RecordRefundSucceeded() => _refundSucceeded.Add(1);

    /// <summary>ثبت refund شکست.</summary>
    public void RecordRefundFailed() => _refundFailed.Add(1);
}
