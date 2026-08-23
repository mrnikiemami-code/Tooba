using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// دابل تست صریح: handlerها را همان‌جا صدا می‌زند. پیش‌فرض تولید نیست و جایگزین SQL Transport نمی‌شود.
/// </summary>
internal sealed class InProcessIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IServiceProvider _services;
    private static readonly Counter<long> Published = ToobaTelemetry.Meter.CreateCounter<long>("tooba.outbox.published");

    /// <summary>
    /// ناشر را به scope جاری وصل می‌کند تا handlerها همان زمینهٔ کارگر را ببینند.
    /// </summary>
    public InProcessIntegrationEventPublisher(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        using var activity = ToobaTelemetry.ActivitySource.StartActivity("tooba.outbox.publish");
        activity?.SetTag("tooba.event_type", integrationEvent.Metadata.EventType);
        activity?.SetTag("tooba.tenant_id", integrationEvent.Metadata.TenantId ?? string.Empty);
        activity?.SetTag("tooba.edition", integrationEvent.Metadata.Edition.ToString());

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType());
        var handlers = _services.GetServices(handlerType);
        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))
                ?? throw new InvalidOperationException("Integration handler contract is missing HandleAsync.");
            var task = (Task?)method.Invoke(handler, [integrationEvent, cancellationToken])
                ?? throw new InvalidOperationException("Integration handler returned no task.");
            await task.ConfigureAwait(false);
        }

        Published.Add(1);
    }
}
