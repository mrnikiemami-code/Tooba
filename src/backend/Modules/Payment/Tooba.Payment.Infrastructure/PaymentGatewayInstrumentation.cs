using System.Diagnostics.Metrics;
using Tooba.BuildingBlocks;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// متریک‌های درگاه پرداخت بدون لاگ راز یا PAN.
/// </summary>
public sealed class PaymentGatewayInstrumentation
{
    private readonly Counter<long> _initiate;
    private readonly Counter<long> _verify;
    private readonly Counter<long> _webhook;
    private readonly Counter<long> _reconcile;

    /// <summary>
    /// متریک‌های in-process Payment gateway.
    /// </summary>
    public PaymentGatewayInstrumentation()
    {
        var meter = ToobaTelemetry.Meter;
        _initiate = meter.CreateCounter<long>("tooba.payment.gateway.initiate");
        _verify = meter.CreateCounter<long>("tooba.payment.gateway.verify");
        _webhook = meter.CreateCounter<long>("tooba.payment.webhook.received");
        _reconcile = meter.CreateCounter<long>("tooba.payment.reconcile.processed");
    }

    /// <summary>
    /// ثبت نتیجهٔ شروع درگاه.
    /// </summary>
    public void RecordInitiate(string outcome) => _initiate.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    /// <summary>
    /// ثبت نتیجهٔ Verify.
    /// </summary>
    public void RecordVerify(string outcome) => _verify.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    /// <summary>
    /// ثبت webhook دریافتی.
    /// </summary>
    public void RecordWebhook(string outcome) => _webhook.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    /// <summary>
    /// ثبت چرخهٔ reconciliation.
    /// </summary>
    public void RecordReconcile(int processed) => _reconcile.Add(processed);
}
