using System.Diagnostics.Metrics;
using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>
/// متریک‌های fulfillment بدون PII.
/// </summary>
public sealed class FulfillmentInstrumentation
{
    private readonly Counter<long> _created;
    private readonly Counter<long> _transitions;
    private readonly Counter<long> _shipments;
    private readonly Counter<long> _tracking;
    private readonly Counter<long> _dispatched;
    private readonly Counter<long> _delivered;

    /// <summary>
    /// متریک‌های in-process Fulfillment.
    /// </summary>
    public FulfillmentInstrumentation()
    {
        var meter = ToobaTelemetry.Meter;
        _created = meter.CreateCounter<long>("tooba.fulfillment.created");
        _transitions = meter.CreateCounter<long>("tooba.fulfillment.transition");
        _shipments = meter.CreateCounter<long>("tooba.fulfillment.shipment.created");
        _tracking = meter.CreateCounter<long>("tooba.fulfillment.tracking.assigned");
        _dispatched = meter.CreateCounter<long>("tooba.fulfillment.dispatched");
        _delivered = meter.CreateCounter<long>("tooba.fulfillment.delivered");
    }

    /// <summary>
    /// ثبت ایجاد fulfillment.
    /// </summary>
    public void RecordCreated() => _created.Add(1);

    /// <summary>
    /// ثبت انتقال وضعیت fulfillment.
    /// </summary>
    public void RecordTransition(string outcome) => _transitions.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    /// <summary>
    /// ثبت ایجاد محموله.
    /// </summary>
    public void RecordShipmentCreated() => _shipments.Add(1);

    /// <summary>
    /// ثبت تخصیص tracking.
    /// </summary>
    public void RecordTrackingAssigned() => _tracking.Add(1);

    /// <summary>
    /// ثبت dispatch محموله.
    /// </summary>
    public void RecordDispatched() => _dispatched.Add(1);

    /// <summary>
    /// ثبت تحویل محموله.
    /// </summary>
    public void RecordDelivered() => _delivered.Add(1);
}
