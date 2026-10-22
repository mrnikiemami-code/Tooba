using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.DependencyInjection;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1 — the REAL module composition, not a hand-copied
/// adapter registration: <see cref="ShippingModule.AddServices"/> must resolve the single scoped catalog
/// adapter and its mandatory canonical tracer, and a read through that adapter must inherit the actual
/// parent activity and correlation identity.
/// </summary>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringTypeCatalogCompositionDiTests
{
    [Fact]
    public void The_module_registers_exactly_one_scoped_catalog_adapter_and_the_canonical_module_tracer()
    {
        var services = new ServiceCollection();
        Compose(services);

        var descriptors = services.Where(descriptor => descriptor.ServiceType == typeof(IShippingTypeCatalogReader)).ToArray();
        var descriptor = Assert.Single(descriptors);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(FulfillmentShippingTypeCatalogReader), descriptor.ImplementationType);

        // The tracer is supplied by the canonical foundation exactly once, never re-registered here.
        Assert.Single(services.Where(candidate => candidate.ServiceType == typeof(IModuleCallTracer)));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var a = first.ServiceProvider.GetRequiredService<IShippingTypeCatalogReader>();
        var b = first.ServiceProvider.GetRequiredService<IShippingTypeCatalogReader>();
        var c = second.ServiceProvider.GetRequiredService<IShippingTypeCatalogReader>();
        Assert.IsType<FulfillmentShippingTypeCatalogReader>(a);
        Assert.Same(a, b);
        Assert.NotSame(a, c);

        // A real handler resolves through this container and reads the controlled Contracts fixture.
        var handler = first.ServiceProvider
            .GetRequiredService<IRequestHandler<GetShippingAuthoringTypeChoicesQuery, Result<ShippingAuthoringTypeChoicesItem>>>();
        var result = handler.Handle(
            new GetShippingAuthoringTypeChoicesQuery(ShippingAuthoringOwnerScope.ForStore("store-a")),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Types);
        Assert.Equal("di-type", result.Value.Types[0].Code);
    }

    [Fact]
    public async Task A_read_resolved_through_real_di_inherits_the_parent_activity_and_correlation_identity()
    {
        var fixture = new SentinelReader();
        var services = new ServiceCollection();
        Compose(services, fixture);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<IShippingTypeCatalogReader>();

        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.DisplayName.StartsWith("module.Shipping.Fulfillment.", StringComparison.Ordinal))
                {
                    spans.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var correlation = Guid.NewGuid().ToString("N");
        using var correlationScope = CorrelationIdContext.BeginScope(correlation);
        using var parent = new ActivitySource("Tooba.Shipping.Tests.Parent").StartActivity("parent.request");
        Assert.NotNull(parent);

        var facts = await catalog.ListActiveAsync(fixture.Token);
        Assert.Same(parent, Activity.Current);
        var fact = Assert.Single(facts);
        Assert.Equal("di-type", fact.Code);
        // Identity of the caller token is preserved by the adapter (the fixture recorded what it received).
        Assert.Equal(fixture.Token, fixture.LastToken);
        Assert.True(fixture.Token.Equals(fixture.LastToken));

        var span = Assert.Single(spans);
        Assert.Equal(parent!.SpanId, span.ParentSpanId);
        Assert.Equal(parent.TraceId, span.TraceId);
        Assert.Equal("Shipping", span.GetTagItem(TracingTagNames.ModuleSource));
        Assert.Equal("Fulfillment", span.GetTagItem(TracingTagNames.ModuleTarget));
        Assert.Equal("ListActiveShippingTypes", span.GetTagItem(TracingTagNames.Operation));
        Assert.Equal(TracingRequestKinds.ModuleCall, span.GetTagItem(TracingTagNames.RequestKind));
        Assert.Equal(correlation, span.GetTagItem(TracingTagNames.CorrelationId));
        Assert.Equal(ActivityStatusCode.Ok, span.Status);

        // The SAME sentinel exception instance propagates (not a clone/wrapper) and only its TYPE is tagged.
        var sentinel = fixture.FailInstance!;
        fixture.ThrowOnList = true;
        var thrown = Assert.Throws<InvalidOperationException>(() => catalog.ListActiveAsync(CancellationToken.None).GetAwaiter().GetResult());
        Assert.Same(sentinel, thrown);
        var failed = spans[^1];
        Assert.Equal(ActivityStatusCode.Error, failed.Status);
        Assert.Equal(typeof(InvalidOperationException).FullName, failed.GetTagItem(TracingTagNames.ExceptionType));
        Assert.Equal(parent.SpanId, failed.ParentSpanId);
        Assert.Same(parent, Activity.Current);
        foreach (var activity in spans)
        {
            var values = string.Join("|", activity.TagObjects.Select(tag => Convert.ToString(tag.Value)));
            Assert.DoesNotContain("di-type", values, StringComparison.Ordinal);
            Assert.DoesNotContain("store-a", values, StringComparison.Ordinal);
            Assert.DoesNotContain(fixture.FailInstance!.Message, values, StringComparison.Ordinal);
        }

        listener.Dispose();
    }

    private static void Compose(ServiceCollection services, SentinelReader? reader = null)
    {
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
        services.AddToobaObservabilityFoundation();
        services.AddToobaCqrsFoundation(typeof(IShippingTypeCatalogReader).Assembly);
        services.AddSingleton<IShippingTypeReader>(reader ?? new SentinelReader());
        // Platform seams the module DbContext factory needs are never resolved by these facts; the DbContext
        // registration itself is the module's own and is not replaced by a hand-copied one here.
        services.AddSingleton<ICurrentCommerceContext>(new StubCommerceContext());
        services.AddSingleton<IDatabaseConnectionResolver>(new StubConnectionResolver());
        new ShippingModule().AddServices(
            services,
            new ConfigurationBuilder().Build(),
            new StubHostEnvironment());
    }

    private sealed class SentinelReader : IShippingTypeReader
    {
        public CancellationToken Token { get; } = new CancellationTokenSource().Token;

        public CancellationToken LastToken { get; private set; }

        public bool ThrowOnList { get; set; }

        public InvalidOperationException? FailInstance { get; } = new("catalog-sentinel-secret");

        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            if (ThrowOnList)
            {
                throw FailInstance!;
            }

            return Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>([
                new ShippingTypeSnapshot(
                    Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    "di-type",
                    true,
                    1,
                    1,
                    [new ShippingTypeTranslationSnapshot(Guid.Parse("33333333-3333-3333-3333-333333333333"), "دی", null)]),
            ]);
        }

        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken)
            => ListActiveAsync(cancellationToken);
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class StubCommerceContext : ICurrentCommerceContext
    {
        public CommerceContext? Current => null;
    }

    private sealed class StubConnectionResolver : IDatabaseConnectionResolver
    {
        public string Resolve(ConnectionReference reference) => "Host=127.0.0.1;Database=unused";
    }
}
