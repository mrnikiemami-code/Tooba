using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Logging;
using Tooba.BuildingBlocks.Observability.Messaging;
using Tooba.BuildingBlocks.Observability.Tracing;
using Xunit;

namespace Tooba.BuildingBlocks.Tests.Observability;

public sealed class TracingAndMessagingFoundationTests
{
    public TracingAndMessagingFoundationTests() => CorrelationIdContext.Clear();

    [Fact]
    public async Task TracingBehavior_creates_command_span_with_tags_and_marks_error()
    {
        var activities = new List<Activity>();
        using var listener = CreateListener(activities);
        CorrelationIdContext.Set(Guid.NewGuid().ToString("N"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation();
        var sp = services.BuildServiceProvider();
        var sender = sp.GetRequiredService<ISender>();

        await sender.Send(new FoundationPingCommand("ok"));
        var ok = Assert.Single(activities, a => a.OperationName.Contains("FoundationPingCommand", StringComparison.Ordinal));
        Assert.Equal("BuildingBlocks", ok.GetTagItem(TracingTagNames.Module)?.ToString());
        Assert.Equal(TracingRequestKinds.Command, ok.GetTagItem(TracingTagNames.RequestKind)?.ToString());
        Assert.Equal(CorrelationIdContext.Current, ok.GetTagItem(TracingTagNames.CorrelationId)?.ToString());
        Assert.Null(ok.GetTagItem("request.payload"));

        activities.Clear();
        await Assert.ThrowsAsync<ValidationException>(() => sender.Send(new FoundationPingCommand("")));
        var failed = Assert.Single(activities, a => a.OperationName.Contains("FoundationPingCommand", StringComparison.Ordinal));
        Assert.Equal(ActivityStatusCode.Error, failed.Status);
        Assert.Equal(typeof(ValidationException).FullName, failed.GetTagItem(TracingTagNames.ExceptionType)?.ToString());
        Assert.Null(failed.GetTagItem("exception.message"));
    }

    [Fact]
    public void ModuleCallTracer_creates_child_span_with_source_target_and_inherits_correlation()
    {
        var activities = new List<Activity>();
        using var listener = CreateListener(activities);
        var correlation = Guid.NewGuid().ToString("N");
        CorrelationIdContext.Set(correlation);

        var tracer = new ModuleCallTracer();
        using (var trace = tracer.Begin("Offer", "Catalog", "LookupVariant"))
        {
            trace.SetOk();
        }

        var span = Assert.Single(activities);
        Assert.Equal("Offer", span.GetTagItem(TracingTagNames.ModuleSource)?.ToString());
        Assert.Equal("Catalog", span.GetTagItem(TracingTagNames.ModuleTarget)?.ToString());
        Assert.Equal(TracingRequestKinds.ModuleCall, span.GetTagItem(TracingTagNames.RequestKind)?.ToString());
        Assert.Equal(correlation, span.GetTagItem(TracingTagNames.CorrelationId)?.ToString());
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
    }

    [Fact]
    public void MessagingCorrelation_consume_scope_restores_previous_value()
    {
        var outer = Guid.NewGuid().ToString("N");
        CorrelationIdContext.Set(outer);
        var incoming = Guid.NewGuid();
        using (MessagingCorrelation.BeginConsumeScope(incoming.ToString("D"), null, Guid.NewGuid()))
        {
            Assert.Equal(incoming.ToString("N"), CorrelationIdContext.Current);
        }

        Assert.Equal(outer, CorrelationIdContext.Current);
    }

    [Fact]
    public void MessagingCorrelation_publish_prefers_metadata_then_ambient_then_event_id()
    {
        CorrelationIdContext.Clear();
        var eventId = Guid.NewGuid();
        Assert.Equal(eventId.ToString("N"), MessagingCorrelation.ResolveForPublish(null, eventId));

        var ambient = Guid.NewGuid().ToString("N");
        CorrelationIdContext.Set(ambient);
        Assert.Equal(ambient, MessagingCorrelation.ResolveForPublish(null, eventId));

        var meta = Guid.NewGuid().ToString("D");
        Assert.Equal(Guid.Parse(meta).ToString("N"), MessagingCorrelation.ResolveForPublish(meta, eventId));
    }

    [Fact]
    public void MessagingCorrelation_enriches_existing_activity_without_duplicate()
    {
        var activities = new List<Activity>();
        using var listener = CreateListener(activities);
        using var existing = new Activity("MassTransit Receive").SetIdFormat(ActivityIdFormat.W3C);
        existing.Start();
        // Source name check uses activity.Source.Name — default Activity has empty source.
        // Simulate non-null current: BeginConsumeActivity should enrich Current and return null owned fallback.
        var owned = MessagingCorrelation.BeginConsumeActivity("demo.event", Guid.NewGuid().ToString("N"));
        Assert.Null(owned);
        existing.Stop();
    }

    [Fact]
    public void ObservabilityLogScope_omits_missing_tenant_store_actor_and_keeps_correlation()
    {
        var correlation = Guid.NewGuid().ToString("N");
        var state = ObservabilityLogScope.CreateState(correlation, requestId: "req-1", httpMethod: "GET", httpPath: "/v1/x");
        var dict = state.ToDictionary();
        Assert.Equal(correlation, dict[ObservabilityLogScopeKeys.CorrelationId]);
        Assert.Equal("GET", dict[ObservabilityLogScopeKeys.HttpMethod]);
        Assert.False(dict.ContainsKey(ObservabilityLogScopeKeys.TenantId));
        Assert.False(dict.ContainsKey(ObservabilityLogScopeKeys.ActorId));
        Assert.False(dict.ContainsKey("Authorization"));
        Assert.False(dict.ContainsKey("email"));
    }

    [Fact]
    public void TracingBehavior_registered_exactly_once_in_cqrs_foundation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation();
        var tracing = services.Where(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>)
            && d.ImplementationType == typeof(TracingBehavior<,>)).ToList();
        Assert.Single(tracing);
    }

    private static ActivityListener CreateListener(List<Activity> sink)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ToobaTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => sink.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
