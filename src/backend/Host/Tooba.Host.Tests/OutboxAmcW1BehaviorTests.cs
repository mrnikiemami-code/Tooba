using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using Tooba.BuildingBlocks;
using Tooba.Host.Configuration;
using Tooba.Host.MultiTenancy;
using Tooba.Host.Outbox;
using Tooba.Persistence;
using Tooba.StoreContext.Contracts.Current;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-HOST-OUTBOX-AMC-001-W1 — options validator and cancellation/retry focused tests.
/// </summary>
public sealed class OutboxAmcW1BehaviorTests
{
    [Fact]
    public void Options_validator_accepts_defaults_and_rejects_non_positive()
    {
        var validator = new OutboxHostOptionsValidator();
        Assert.True(validator.Validate(null, new OutboxHostOptions()).Succeeded);

        Assert.False(validator.Validate(null, new OutboxHostOptions { PollIntervalSeconds = 0 }).Succeeded);
        Assert.False(validator.Validate(null, new OutboxHostOptions { BatchSize = 0 }).Succeeded);
        Assert.False(validator.Validate(null, new OutboxHostOptions { RetryBaseDelaySeconds = -1 }).Succeeded);
        Assert.False(validator.Validate(null, new OutboxHostOptions { MaxAttempts = 0 }).Succeeded);
        Assert.False(validator.Validate(null, new OutboxHostOptions { LockSeconds = 0 }).Succeeded);
        Assert.True(validator.Validate(null, new OutboxHostOptions { Enabled = false }).Succeeded);
    }

    [Fact]
    public async Task Requested_cancellation_on_claim_propagates_without_retry_or_dead_letter()
    {
        var store = new RecordingStore { ClaimThrowsCanceled = true };
        var dispatcher = CreateDispatcher(store, targets: OneTarget(), modules: OneModule());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.DispatchOnceAsync(cts.Token));
        Assert.Equal(0, store.MarkRetryCalls);
        Assert.Equal(0, store.MarkDeadLetterCalls);
    }

    [Fact]
    public async Task Requested_cancellation_during_publish_does_not_mark_retry_or_dead_letter()
    {
        var message = SampleMessage(attemptCount: 1);
        var store = new RecordingStore { Claimed = [message] };
        using var cts = new CancellationTokenSource();
        var publisher = new CancelingPublisher(cts);
        var dispatcher = CreateDispatcher(store, targets: OneTarget(), modules: OneModule(), publisher: publisher);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.DispatchOnceAsync(cts.Token));
        Assert.Equal(0, store.MarkRetryCalls);
        Assert.Equal(0, store.MarkDeadLetterCalls);
        Assert.Equal(0, store.MarkProcessedCalls);
    }

    [Fact]
    public async Task Non_cancellation_publish_failure_still_marks_retry()
    {
        var message = SampleMessage(attemptCount: 1);
        var store = new RecordingStore { Claimed = [message] };
        var publisher = new FailingPublisher();
        var dispatcher = CreateDispatcher(store, targets: OneTarget(), modules: OneModule(), publisher: publisher);

        await dispatcher.DispatchOnceAsync(CancellationToken.None);
        Assert.Equal(1, store.MarkRetryCalls);
        Assert.Equal(0, store.MarkDeadLetterCalls);
    }

    [Fact]
    public async Task Max_attempts_publish_failure_still_marks_dead_letter()
    {
        var message = SampleMessage(attemptCount: 5);
        var store = new RecordingStore { Claimed = [message] };
        var publisher = new FailingPublisher();
        var dispatcher = CreateDispatcher(
            store,
            targets: OneTarget(),
            modules: OneModule(),
            publisher: publisher,
            options: new OutboxHostOptions
            {
                MaxAttempts = 5,
                RetryBaseDelaySeconds = 2,
                BatchSize = 20,
                LockSeconds = 30,
                PollIntervalSeconds = 2,
            });

        await dispatcher.DispatchOnceAsync(CancellationToken.None);
        Assert.Equal(1, store.MarkDeadLetterCalls);
        Assert.Equal(0, store.MarkRetryCalls);
    }

    [Fact]
    public async Task Claim_non_cancellation_failure_isolates_module_and_continues()
    {
        var store = new RecordingStore
        {
            ClaimThrows = new InvalidOperationException("claim-boom"),
            ClaimedBySchema = new Dictionary<string, IReadOnlyList<OutboxMessage>>(StringComparer.Ordinal)
            {
                ["ok"] = [SampleMessage(attemptCount: 1)],
            },
        };
        var publisher = new NoopPublisher();
        var dispatcher = CreateDispatcher(
            store,
            targets: OneTarget(),
            modules: [new FixedModule("bad"), new FixedModule("ok")],
            publisher: publisher);

        await dispatcher.DispatchOnceAsync(CancellationToken.None);
        Assert.Equal(1, store.MarkProcessedCalls);
        Assert.Equal(0, store.MarkRetryCalls);
    }

    [Fact]
    public async Task Target_non_cancellation_failure_isolates_target_and_continues()
    {
        var store = new RecordingStore { Claimed = [SampleMessage(attemptCount: 1)] };
        var publisher = new NoopPublisher();
        var dispatcher = CreateDispatcher(
            store,
            targets:
            [
                new OutboxPollTarget(ToobaEdition.Marketplace, null, new ConnectionReference("bad-target"), "dep-1"),
                new OutboxPollTarget(ToobaEdition.Marketplace, null, new ConnectionReference("marketplace"), "dep-1"),
            ],
            modules: OneModule(),
            publisher: publisher,
            connections: new FixedConnections(failRefs: ["bad-target"]));

        await dispatcher.DispatchOnceAsync(CancellationToken.None);
        Assert.Equal(1, store.MarkProcessedCalls);
    }

    [Fact]
    public void Missing_or_inactive_tenant_fails_closed_from_worker_factory()
    {
        var options = OutboxTestPlatform.TwoTenants("Host=a;", "Host=b;");
        var registry = PlatformOptionsValidator.BuildRegistry(options);
        var factory = new WorkerCommerceContextFactory(registry);
        var message = SampleMessage(attemptCount: 1);
        message.TenantId = "missing-tenant";
        Assert.Throws<InvalidOperationException>(() => factory.FromOutbox(message, "trace"));

        message.TenantId = "store-disabled";
        Assert.Throws<InvalidOperationException>(() => factory.FromOutbox(message, "trace"));
    }

    private static OutboxDispatcher CreateDispatcher(
        RecordingStore store,
        IReadOnlyList<OutboxPollTarget> targets,
        IEnumerable<IOutboxModuleRegistration> modules,
        IIntegrationEventPublisher? publisher = null,
        OutboxHostOptions? options = null,
        IDatabaseConnectionResolver? connections = null)
    {
        options ??= new OutboxHostOptions
        {
            Enabled = true,
            PollIntervalSeconds = 2,
            BatchSize = 20,
            RetryBaseDelaySeconds = 2,
            MaxAttempts = 5,
            LockSeconds = 30,
        };

        var platform = OutboxTestPlatform.Marketplace("Host=fake;");
        var registry = PlatformOptionsValidator.BuildRegistry(platform);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICommerceContextAssigner, FixedAssigner>();
        services.AddSingleton<IStoreCommerceContextAssigner, FixedStoreAssigner>();
        services.AddSingleton<IIntegrationEventPublisher>(publisher ?? new NoopPublisher());
        var provider = services.BuildServiceProvider();

        return new OutboxDispatcher(
            new FixedTargets(targets),
            modules,
            store,
            new StubSerializer(),
            connections ?? new FixedConnections(),
            new WorkerCommerceContextFactory(registry),
            new WorkerStoreCommerceContextFactory(registry),
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<OutboxDispatcher>.Instance);
    }

    private static IReadOnlyList<OutboxPollTarget> OneTarget() =>
    [
        new OutboxPollTarget(ToobaEdition.Marketplace, null, new ConnectionReference("marketplace"), "dep-1"),
    ];

    private static IEnumerable<IOutboxModuleRegistration> OneModule() =>
        [new FixedModule("probe")];

    private static OutboxMessage SampleMessage(int attemptCount) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventType = "probe.event",
            Payload = "{}",
            CorrelationId = Guid.NewGuid().ToString("N"),
            Version = 1,
            TenantId = null,
            DeploymentId = "dep-1",
            Edition = "Marketplace",
            AttemptCount = attemptCount,
            OccurredAt = SystemClock.Instance.GetCurrentInstant(),
        };

    private sealed class FixedTargets(IReadOnlyList<OutboxPollTarget> targets) : IOutboxPollTargetSource
    {
        public IReadOnlyList<OutboxPollTarget> GetTargets() => targets;
    }

    private sealed class FixedModule(string schema) : IOutboxModuleRegistration
    {
        public string Schema => schema;
        public string TableName => "outbox_messages";
        public Type DbContextType => typeof(object);
        public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;
        public string GetEventTypeName(Type integrationEventType) => "probe.event";
        public Type? ResolveEventClrType(string eventTypeName) => typeof(StubEvent);
    }

    private sealed class FixedConnections(IEnumerable<string>? failRefs = null) : IDatabaseConnectionResolver
    {
        private readonly HashSet<string> _failRefs = new(failRefs ?? [], StringComparer.Ordinal);

        public string Resolve(ConnectionReference reference)
        {
            if (_failRefs.Contains(reference.Value))
            {
                throw new InvalidOperationException("resolve-boom");
            }

            return "Host=fake;";
        }
    }

    private sealed class StubSerializer : IIntegrationEventSerializer
    {
        public string SerializePayload(IIntegrationEvent integrationEvent) => "{}";

        public IIntegrationEvent Deserialize(OutboxMessage message) => new StubEvent();
    }

    private sealed class StubEvent : IIntegrationEvent
    {
        public EventMetadata Metadata { get; set; } = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "probe.event",
            null,
            1,
            null,
            "dep-1",
            ToobaEdition.Marketplace);
    }

    private sealed class FixedAssigner : ICommerceContextAssigner
    {
        public void Assign(CommerceContext context)
        {
        }
    }

    private sealed class FixedStoreAssigner : IStoreCommerceContextAssigner
    {
        public void Assign(StoreCommerceContext context)
        {
        }
    }

    private sealed class NoopPublisher : IIntegrationEventPublisher
    {
        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FailingPublisher : IIntegrationEventPublisher
    {
        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class CancelingPublisher(CancellationTokenSource cts) : IIntegrationEventPublisher
    {
        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        }
    }

    private sealed class RecordingStore : IOutboxDispatcherStore
    {
        public bool ClaimThrowsCanceled { get; init; }
        public Exception? ClaimThrows { get; init; }
        public IReadOnlyList<OutboxMessage> Claimed { get; init; } = [];
        public Dictionary<string, IReadOnlyList<OutboxMessage>>? ClaimedBySchema { get; init; }
        public int MarkRetryCalls { get; private set; }
        public int MarkDeadLetterCalls { get; private set; }
        public int MarkProcessedCalls { get; private set; }

        public Task<IReadOnlyList<OutboxMessage>> ClaimAsync(
            string connectionString,
            string schema,
            string tableName,
            int batchSize,
            int lockSeconds,
            CancellationToken cancellationToken)
        {
            if (ClaimThrowsCanceled)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (ClaimThrows is not null && (ClaimedBySchema is null || !ClaimedBySchema.ContainsKey(schema)))
            {
                throw ClaimThrows;
            }

            if (ClaimedBySchema is not null)
            {
                return Task.FromResult(ClaimedBySchema.TryGetValue(schema, out var rows) ? rows : Array.Empty<OutboxMessage>());
            }

            return Task.FromResult(Claimed);
        }

        public Task MarkProcessedAsync(
            string connectionString,
            string schema,
            string tableName,
            Guid id,
            CancellationToken cancellationToken)
        {
            MarkProcessedCalls++;
            return Task.CompletedTask;
        }

        public Task MarkRetryAsync(
            string connectionString,
            string schema,
            string tableName,
            Guid id,
            Instant nextAttemptAt,
            string lastError,
            CancellationToken cancellationToken)
        {
            MarkRetryCalls++;
            return Task.CompletedTask;
        }

        public Task MarkDeadLetterAsync(
            string connectionString,
            string schema,
            string tableName,
            Guid id,
            string lastError,
            CancellationToken cancellationToken)
        {
            MarkDeadLetterCalls++;
            return Task.CompletedTask;
        }
    }
}
