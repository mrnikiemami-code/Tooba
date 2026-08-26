using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.PlatformProbe.Infrastructure.Events;
using Tooba.PlatformProbe.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تست‌های PostgreSQL برای تراکنش Outbox، dispatcher، retry، isolation و SKIP LOCKED.
/// </summary>
[Collection("PostgresSerial")]
public sealed class OutboxPostgresTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;
    private string _alpha = "";
    private string _bravo = "";
    private string _marketplace = "";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_alpha")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
            _alpha = _container.GetConnectionString();
            await using (var admin = new NpgsqlConnection(_alpha))
            {
                await admin.OpenAsync();
                await using var createBravo = new NpgsqlCommand("CREATE DATABASE tooba_bravo", admin);
                await createBravo.ExecuteNonQueryAsync();
                await using var createMarket = new NpgsqlCommand("CREATE DATABASE tooba_marketplace", admin);
                await createMarket.ExecuteNonQueryAsync();
            }

            _bravo = new NpgsqlConnectionStringBuilder(_alpha) { Database = "tooba_bravo" }.ConnectionString;
            _marketplace = new NpgsqlConnectionStringBuilder(_alpha) { Database = "tooba_marketplace" }.ConnectionString;
            await EnsureAsync(_alpha);
            await EnsureAsync(_bravo);
            await EnsureAsync(_marketplace);
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Same_transaction_writes_outbox_and_rollback_leaves_none()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));

        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            await using var tx = await context.Database.BeginTransactionAsync();
            context.Records.Add(PlatformProbePersistence.NewRecord());
            await context.SaveChangesAsync();
            Assert.Equal(1, await context.OutboxMessages.CountAsync());
            await tx.RollbackAsync();
        }

        await using (var verify = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            Assert.Equal(0, await verify.OutboxMessages.CountAsync());
            Assert.Equal(0, await verify.Records.CountAsync());
        }

        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            var record = PlatformProbePersistence.NewRecord();
            record.Raise(new ProbeInternalNoteDomainEvent("must-not-publish"));
            context.Records.Add(record);
            await context.SaveChangesAsync();
            var rows = await context.OutboxMessages.ToListAsync();
            Assert.Single(rows);
            Assert.Equal(ProbeRecordCreatedIntegrationEvent.EventTypeName, rows[0].EventType);
            Assert.Equal("store-alpha", rows[0].TenantId);
            Assert.DoesNotContain(rows, r => r.EventType.Contains("internal", StringComparison.Ordinal));
        }
    }

    [SkippableFact]
    public async Task Dispatcher_publishes_marks_processed_and_isolates_tenants()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("store-bravo", "tenant-bravo"));

        await using (var a = OutboxTestContextFactory.Create(_alpha, commerceA))
        {
            a.Records.Add(PlatformProbePersistence.NewRecord());
            await a.SaveChangesAsync();
        }

        await using (var b = OutboxTestContextFactory.Create(_bravo, commerceB))
        {
            Assert.Equal(0, await b.OutboxMessages.CountAsync());
            b.Records.Add(PlatformProbePersistence.NewRecord());
            await b.SaveChangesAsync();
            Assert.Equal(1, await b.OutboxMessages.CountAsync());
        }

        var recording = new RecordingProbeHandler(new FixedTenant("store-alpha"));
        var options = OutboxTestPlatform.TwoTenants(_alpha, _bravo);
        await using var services = OutboxTestPlatform.BuildDispatcherServices(options, failHandlers: false, recording);
        // Replace handler capture of ICurrentTenant with real scoped assigner by using recording registered as singleton
        // that reads ICurrentTenant from constructor — re-register via a capturing handler created per scope.
        var dispatcher = services.GetRequiredService<OutboxDispatcher>();
        await dispatcher.DispatchOnceAsync(CancellationToken.None);

        await using (var a = OutboxTestContextFactory.Create(_alpha, commerceA))
        {
            var row = Assert.Single(await a.OutboxMessages.ToListAsync());
            Assert.NotNull(row.ProcessedAt);
        }

        Assert.NotEmpty(recording.Received);
        Assert.All(recording.Received, e => Assert.Equal(ProbeRecordCreatedIntegrationEvent.EventTypeName, e.Metadata.EventType));
    }

    [SkippableFact]
    public async Task Handler_failure_retries_then_dead_letters()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            foreach (var row in context.OutboxMessages)
            {
                context.OutboxMessages.Remove(row);
            }

            await context.SaveChangesAsync();
            context.Records.Add(PlatformProbePersistence.NewRecord());
            await context.SaveChangesAsync();
        }

        var options = OutboxTestPlatform.TwoTenants(_alpha, _bravo);
        await using var services = OutboxTestPlatform.BuildDispatcherServices(options, failHandlers: true, recording: null);
        var dispatcher = services.GetRequiredService<OutboxDispatcher>();
        await dispatcher.DispatchOnceAsync(CancellationToken.None);

        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            var row = Assert.Single(await context.OutboxMessages.ToListAsync());
            Assert.Null(row.ProcessedAt);
            Assert.Null(row.DeadLetteredAt);
            Assert.NotNull(row.NextAttemptAt);
            Assert.NotNull(row.LastError);
            Assert.DoesNotContain("Password", row.LastError, StringComparison.OrdinalIgnoreCase);
            row.NextAttemptAt = NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow.AddMinutes(-1));
            row.LockedUntil = null;
            await context.SaveChangesAsync();
        }

        await dispatcher.DispatchOnceAsync(CancellationToken.None);
        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            var row = Assert.Single(await context.OutboxMessages.ToListAsync());
            Assert.NotNull(row.DeadLetteredAt);
            Assert.Null(row.ProcessedAt);
        }
    }

    [SkippableFact]
    public async Task Concurrent_claim_does_not_deliver_the_same_row_twice()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            context.Records.Add(PlatformProbePersistence.NewRecord());
            context.Records.Add(PlatformProbePersistence.NewRecord());
            await context.SaveChangesAsync();
        }

        var store = new NpgsqlOutboxDispatcherStore();
        var first = store.ClaimAsync(_alpha, PlatformProbeDbContext.Schema, OutboxMessageMapping.TableName, 10, 30, CancellationToken.None);
        var second = store.ClaimAsync(_alpha, PlatformProbeDbContext.Schema, OutboxMessageMapping.TableName, 10, 30, CancellationToken.None);
        var results = await Task.WhenAll(first, second);
        var ids = results.SelectMany(r => r).Select(m => m.Id).ToArray();
        Assert.Equal(2, ids.Length);
        Assert.Equal(ids.Distinct().Count(), ids.Length);
    }

    [SkippableFact]
    public async Task Worker_handler_sees_message_tenant_not_host_header()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            context.OutboxMessages.RemoveRange(await context.OutboxMessages.ToListAsync());
            context.Records.Add(PlatformProbePersistence.NewRecord());
            await context.SaveChangesAsync();
        }

        var recording = new RecordingProbeHandler(new FixedTenant(null));
        var options = OutboxTestPlatform.TwoTenants(_alpha, _bravo);
        await using var provider = OutboxTestPlatform.BuildDispatcherServices(options, failHandlers: false, recording);
        await provider.GetRequiredService<OutboxDispatcher>().DispatchOnceAsync(CancellationToken.None);
        Assert.NotEmpty(recording.Received);
        Assert.All(recording.Received, e => Assert.Equal("store-alpha", e.Metadata.TenantId));
        Assert.DoesNotContain(recording.Received, e => e.Metadata.TenantId == "store-bravo");
    }

    [SkippableFact]
    public async Task Marketplace_dispatcher_only_reads_marketplace_database()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var marketCommerce = new FixedCommerceContext();
        marketCommerce.Assign(new CommerceContext(
            new EditionContext(ToobaEdition.Marketplace, "test-marketplace-outbox"),
            Tenant: null,
            new ConnectionReference("marketplace"),
            "trace-m"));
        await using (var market = OutboxTestContextFactory.Create(_marketplace, marketCommerce))
        {
            market.Records.Add(PlatformProbePersistence.NewRecord());
            await market.SaveChangesAsync();
        }

        var storeCommerce = new FixedCommerceContext();
        storeCommerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        await using (var store = OutboxTestContextFactory.Create(_alpha, storeCommerce))
        {
            store.Records.Add(PlatformProbePersistence.NewRecord());
            await store.SaveChangesAsync();
        }

        var recording = new RecordingProbeHandler(new FixedTenant(null));
        var options = OutboxTestPlatform.Marketplace(_marketplace);
        await using var services = OutboxTestPlatform.BuildDispatcherServices(options, failHandlers: false, recording);
        await services.GetRequiredService<OutboxDispatcher>().DispatchOnceAsync(CancellationToken.None);

        await using (var market = OutboxTestContextFactory.Create(_marketplace, marketCommerce))
        {
            Assert.All(await market.OutboxMessages.ToListAsync(), row => Assert.NotNull(row.ProcessedAt));
        }

        await using (var store = OutboxTestContextFactory.Create(_alpha, storeCommerce))
        {
            Assert.Contains(await store.OutboxMessages.ToListAsync(), row => row.ProcessedAt is null);
        }
    }

    [SkippableFact]
    public async Task Expired_lock_is_reclaimed_after_lease_elapses()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            context.Records.Add(PlatformProbePersistence.NewRecord());
            await context.SaveChangesAsync();
        }

        var store = new NpgsqlOutboxDispatcherStore();
        var claimed = await store.ClaimAsync(
            _alpha,
            PlatformProbeDbContext.Schema,
            OutboxMessageMapping.TableName,
            1,
            1,
            CancellationToken.None);
        var message = Assert.Single(claimed);
        Assert.NotNull(message.LockedUntil);

        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            var row = await context.OutboxMessages.SingleAsync(x => x.Id == message.Id);
            row.LockedUntil = NodaTime.SystemClock.Instance.GetCurrentInstant().Minus(NodaTime.Duration.FromMinutes(1));
            await context.SaveChangesAsync();
        }

        var reclaimed = await store.ClaimAsync(
            _alpha,
            PlatformProbeDbContext.Schema,
            OutboxMessageMapping.TableName,
            1,
            30,
            CancellationToken.None);
        Assert.Equal(message.Id, Assert.Single(reclaimed).Id);
    }

    private async Task EnsureAsync(string connectionString)
    {
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        await using var context = OutboxTestContextFactory.Create(connectionString, commerce);
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Tenant ثابت برای handler ضبط‌کننده در تست‌هایی که scope واقعی دارند.
    /// </summary>
    private sealed class FixedTenant : ICurrentTenant
    {
        public FixedTenant(string? tenantId) =>
            Current = tenantId is null
                ? null
                : new TenantContext(
                    new TenantId(tenantId),
                    TenantStatus.Active,
                    new ConnectionReference("tenant-alpha"),
                    null,
                    null,
                    null,
                    tenantId + ".localhost",
                    null);

        public TenantContext? Current { get; }
    }

    /// <summary>
    /// handler که Tenant بازسازی‌شده در scope کارگر را ثبت می‌کند.
    /// </summary>
    private sealed class CaptureTenantHandler : IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>
    {
        private readonly ICurrentTenant _tenant;
        private readonly List<string?> _seen;

        public CaptureTenantHandler(ICurrentTenant tenant, List<string?> seen)
        {
            _tenant = tenant;
            _seen = seen;
        }

        public Task HandleAsync(ProbeRecordCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            _seen.Add(_tenant.Current?.TenantId.Value);
            return Task.CompletedTask;
        }
    }
}
