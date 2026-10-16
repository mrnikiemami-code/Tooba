using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Host.Transport;
using Tooba.Payment.Contracts.Events;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-139 — تحویل رویداد سفارش به همهٔ ماژول‌های هم‌قرارداد.
/// شکست handler یک ماژول نباید تحویل به ماژول دیگر را قطع کند، ولی شکست باید دیده شود تا
/// retry/dead-letter حمل‌ونقل حفظ شود.
/// </summary>
public sealed class IntegrationHandlerDeliveryFanOutTests
{
    [Fact]
    public async Task Every_registered_module_handler_receives_the_same_payment_succeeded_event_once()
    {
        var order = new RecordingModuleHandler();
        var fulfillment = new RecordingModuleHandler();
        await using var provider = Build(order, fulfillment);

        var invoked = await ToobaIntegrationTransportConsumer.InvokeRegisteredHandlersAsync(
            provider,
            Event(),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(2, invoked);
        Assert.Equal(1, order.Invocations);
        Assert.Equal(1, fulfillment.Invocations);
        Assert.NotEqual(Guid.Empty, order.SeenEventId);
        Assert.Equal(order.SeenEventId, fulfillment.SeenEventId);
    }

    [Fact]
    public async Task Failure_in_one_module_handler_does_not_starve_the_other_module_handlers()
    {
        var order = new RecordingModuleHandler();
        var fulfillment = new ThrowingModuleHandler();
        var settlement = new RecordingModuleHandler();
        await using var provider = Build(order, fulfillment, settlement);

        var error = await Assert.ThrowsAsync<IntegrationHandlerDeliveryException>(() =>
            ToobaIntegrationTransportConsumer.InvokeRegisteredHandlersAsync(
                provider,
                Event(),
                NullLogger.Instance,
                CancellationToken.None));

        Assert.Equal(1, order.Invocations);
        Assert.Equal(1, settlement.Invocations);
        Assert.Single(error.Failures);
        Assert.Equal(nameof(ThrowingModuleHandler), error.Failures[0].Handler);
        Assert.Contains(nameof(ThrowingModuleHandler), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handler_failure_is_still_surfaced_so_transport_retry_is_preserved()
    {
        var fulfillment = new ThrowingModuleHandler();
        await using var provider = Build(fulfillment);

        await Assert.ThrowsAsync<IntegrationHandlerDeliveryException>(() =>
            ToobaIntegrationTransportConsumer.InvokeRegisteredHandlersAsync(
                provider,
                Event(),
                NullLogger.Instance,
                CancellationToken.None));
    }

    [Fact]
    public async Task Consume_cancellation_is_not_swallowed_as_a_handler_failure()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var fulfillment = new CancellingModuleHandler();
        await using var provider = Build(fulfillment);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ToobaIntegrationTransportConsumer.InvokeRegisteredHandlersAsync(
                provider,
                Event(),
                NullLogger.Instance,
                cts.Token));
    }

    [Fact]
    public async Task Legacy_sequential_loop_starved_the_module_handlers_that_came_after_a_failing_handler()
    {
        var order = new RecordingModuleHandler();
        var fulfillment = new ThrowingModuleHandler();
        var settlement = new RecordingModuleHandler();
        await using var provider = Build(order, fulfillment, settlement);

        // Characterization of the pre-139 loop: first throw aborted the remaining handlers of the same event.
        var handlers = provider.GetServices<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>>();
        var starved = false;
        try
        {
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(Event(), CancellationToken.None);
            }
        }
        catch (InvalidOperationException)
        {
            starved = true;
        }

        Assert.True(starved);
        Assert.Equal(1, order.Invocations);
        Assert.Equal(0, settlement.Invocations);

        // The owner-correct seam delivers to every module handler and still surfaces the failure.
        await Assert.ThrowsAsync<IntegrationHandlerDeliveryException>(() =>
            ToobaIntegrationTransportConsumer.InvokeRegisteredHandlersAsync(
                provider,
                Event(),
                NullLogger.Instance,
                CancellationToken.None));

        Assert.Equal(2, order.Invocations);
        Assert.Equal(1, settlement.Invocations);
    }

    private static PaymentSucceededIntegrationEvent Event() => new()
    {
        PaymentId = Guid.NewGuid(),
        CheckoutId = Guid.NewGuid(),
        Amount = 3_977_000m,
        Currency = "IRR",
        SellerOrderIds = [Guid.NewGuid()],
    };

    private static ServiceProvider Build(params IIntegrationEventHandler<PaymentSucceededIntegrationEvent>[] handlers)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        foreach (var handler in handlers)
        {
            services.AddSingleton<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>>(handler);
        }

        return services.BuildServiceProvider();
    }

    private class RecordingModuleHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
    {
        public int Invocations { get; private set; }

        public Guid SeenEventId { get; private set; }

        public Task HandleAsync(PaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Invocations++;
            SeenEventId = integrationEvent.Metadata.EventId;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingModuleHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
    {
        public Task HandleAsync(PaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("module handler could not complete the event");
    }

    private sealed class CancellingModuleHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
    {
        public Task HandleAsync(PaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
