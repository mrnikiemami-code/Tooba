using System.Collections.Concurrent;
using System.Diagnostics;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.PlatformProbe.Infrastructure;
using Tooba.PlatformProbe.Infrastructure.Events;
using Tooba.PlatformProbe.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تست واقعی MassTransit PostgreSQL SQL Transport با Testcontainers؛ mock جایگزین transport نیست.
/// </summary>
[Collection("PostgresSerial")]
public sealed class MassTransitPostgresTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;
    private string _alpha = "";
    private string _bravo = "";
    private string _messaging = "";

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
                await using var createMessaging = new NpgsqlCommand("CREATE DATABASE tooba_messaging", admin);
                await createMessaging.ExecuteNonQueryAsync();
            }

            _bravo = new NpgsqlConnectionStringBuilder(_alpha) { Database = "tooba_bravo" }.ConnectionString;
            _messaging = new NpgsqlConnectionStringBuilder(_alpha) { Database = "tooba_messaging" }.ConnectionString;
            await EnsureBusinessAsync(_alpha);
            await EnsureBusinessAsync(_bravo);
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
    public async Task Outbox_publishes_into_sql_transport_and_consumer_keeps_tenant()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        Guid recordId;
        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            var record = PlatformProbePersistence.NewRecord();
            recordId = record.Id;
            context.Records.Add(record);
            await context.SaveChangesAsync();
        }

        var sink = new ProbeHandlerSink();
        var logs = new ListLoggerProvider();
        using var host = BuildHost(sink, failHandlers: false, logs);
        await host.StartAsync();
        try
        {
            await host.Services.GetRequiredService<OutboxDispatcher>().DispatchOnceAsync(CancellationToken.None);
            await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
            {
                var row = Assert.Single(await context.OutboxMessages.ToListAsync());
                Assert.True(row.ProcessedAt is not null, row.LastError ?? "outbox was not published to transport");
            }

            try
            {
                await WaitUntilAsync(() => sink.Received.Count > 0);
            }
            catch (TimeoutException)
            {
                var sql = host.Services.GetRequiredService<IOptions<SqlTransportOptions>>().Value;
                var health = host.Services.GetRequiredService<IBusControl>().CheckHealth();
                await using var conn = new NpgsqlConnection(_messaging);
                await conn.OpenAsync();
                await using var tables = new NpgsqlCommand(
                    "SELECT COALESCE(string_agg(schemaname || '.' || tablename, ','), '') FROM pg_tables WHERE schemaname IN ('transport','public') AND tablename LIKE '%message%' OR schemaname = 'transport'",
                    conn);
                var tableList = (string?)await tables.ExecuteScalarAsync();
                throw new TimeoutException(
                    $"consumer timeout. bus={health.Status} sqlHost={sql.Host} sqlDb={sql.Database} sqlSchema={sql.Schema} tables={tableList} logs={string.Join(';', logs.Messages.Take(20))}");
            }

            await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
            {
                var row = Assert.Single(await context.OutboxMessages.ToListAsync());
                Assert.NotNull(row.ProcessedAt);
                Assert.Null(row.DeadLetteredAt);
            }

            var received = Assert.Single(sink.Received);
            Assert.Equal(ProbeRecordCreatedIntegrationEvent.EventTypeName, received.Metadata.EventType);
            Assert.Equal("store-alpha", received.Metadata.TenantId);
            Assert.Equal("test-outbox", received.Metadata.DeploymentId);
            Assert.Equal(ToobaEdition.SingleStore, received.Metadata.Edition);
            Assert.Equal("store-alpha", Assert.Single(sink.Tenants));
            Assert.DoesNotContain(sink.Tenants, t => t == "store-bravo");
            Assert.NotEqual(Guid.Empty, received.Metadata.EventId);
            Assert.Equal(recordId, received.RecordId);

            var joined = string.Join('\n', logs.Messages);
            Assert.DoesNotContain("dev-placeholder", joined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password=", joined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PayloadJson", joined, StringComparison.Ordinal);
            Assert.DoesNotContain(recordId.ToString(), joined, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [SkippableFact]
    public async Task Tenant_a_message_does_not_run_as_tenant_b()
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
            b.Records.Add(PlatformProbePersistence.NewRecord());
            await b.SaveChangesAsync();
        }

        var sink = new ProbeHandlerSink();
        using var host = BuildHost(sink, failHandlers: false, new ListLoggerProvider());
        await host.StartAsync();
        try
        {
            await host.Services.GetRequiredService<OutboxDispatcher>().DispatchOnceAsync(CancellationToken.None);
            await WaitUntilAsync(() => sink.Received.Count >= 2);
            Assert.Equal(sink.Received.Count, sink.Tenants.Count);
            foreach (var pair in sink.Received.Zip(sink.Tenants))
            {
                Assert.Equal(pair.First.Metadata.TenantId, pair.Second);
            }

            Assert.Contains(sink.Tenants, t => t == "store-alpha");
            Assert.Contains(sink.Tenants, t => t == "store-bravo");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [SkippableFact]
    public async Task Consumer_failure_uses_transport_retry_not_outbox_dead_letter()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
        {
            context.Records.Add(PlatformProbePersistence.NewRecord());
            await context.SaveChangesAsync();
        }

        var sink = new ProbeHandlerSink();
        using var host = BuildHost(sink, failHandlers: true, new ListLoggerProvider());
        await host.StartAsync();
        try
        {
            await host.Services.GetRequiredService<OutboxDispatcher>().DispatchOnceAsync(CancellationToken.None);
            await WaitUntilAsync(() => sink.Failures >= 3);
            await Task.Delay(1500);

            await using (var context = OutboxTestContextFactory.Create(_alpha, commerce))
            {
                var row = Assert.Single(await context.OutboxMessages.ToListAsync());
                Assert.NotNull(row.ProcessedAt);
                Assert.Null(row.DeadLetteredAt);
            }

            await using var conn = new NpgsqlConnection(_messaging);
            await conn.OpenAsync();
            await using var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM transport.message_delivery", conn);
            var deliveries = Convert.ToInt64(await countCmd.ExecuteScalarAsync());
            Assert.True(sink.Failures >= 3);
            Assert.True(deliveries >= 1, "consumer retries must leave SQL Transport deliveries");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [SkippableFact]
    public async Task Publisher_adapter_is_masstransit_and_handler_has_no_masstransit_type()
    {
        Skip.If(!_dockerAvailable, "Docker/Testcontainers PostgreSQL is not available.");
        using var host = BuildHost(new ProbeHandlerSink(), failHandlers: false, new ListLoggerProvider());
        await host.StartAsync();
        try
        {
            await using var scope = host.Services.CreateAsyncScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            Assert.IsType<MassTransitIntegrationEventPublisher>(publisher);
            Assert.DoesNotContain(typeof(FailingProbeHandler).GetInterfaces(), i => i.Namespace?.Contains("MassTransit") == true);
            Assert.Contains(
                host.Services.GetRequiredService<IOptions<SqlTransportOptions>>().Value.Schema,
                new[] { "transport" },
                StringComparer.Ordinal);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private IHost BuildHost(ProbeHandlerSink sink, bool failHandlers, ListLoggerProvider logs)
    {
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        });
        var platform = OutboxTestPlatform.TwoTenants(_alpha, _bravo);
        platform.PostgreSQL.ConnectionReferences["messaging"] = _messaging;

        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("MassTransit", LogLevel.Warning);
                logging.AddProvider(logs);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(sink);
                services.AddLogging();
                services.AddSingleton<IOptions<ToobaPlatformOptions>>(Options.Create(platform));
                services.AddSingleton(PlatformOptionsValidator.BuildRegistry(platform));
                services.AddSingleton<IDatabaseConnectionResolver, DatabaseConnectionResolver>();
                services.AddSingleton<IOutboxModuleRegistration, PlatformProbeOutboxRegistration>();
                services.AddSingleton<IIntegrationEventSerializer, JsonIntegrationEventSerializer>();
                services.AddSingleton<IOutboxDispatcherStore, NpgsqlOutboxDispatcherStore>();
                services.AddSingleton<IOutboxPollTargetSource, ConfiguredOutboxPollTargetSource>();
                services.AddSingleton<WorkerCommerceContextFactory>();
                services.AddSingleton<IOptions<OutboxHostOptions>>(Options.Create(new OutboxHostOptions
                {
                    Enabled = true,
                    BatchSize = 20,
                    MaxAttempts = 5,
                    RetryBaseDelaySeconds = 1,
                    LockSeconds = 30,
                }));
                services.AddSingleton<OutboxDispatcher>();
                services.AddSingleton<IHttpContextAccessor, SpoofHostAccessor>();
                services.AddScoped<HttpCommerceContextAccessor>();
                services.AddScoped<ICurrentCommerceContext>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
                services.AddScoped<ICurrentEdition>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
                services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
                services.AddScoped<ICommerceContextAssigner>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
                services.AddOptions<MessagingHostOptions>().Configure(o =>
                {
                    o.Enabled = true;
                    o.ConnectionReference = "messaging";
                    o.Schema = "transport";
                });
                services.AddToobaMassTransitMessaging();
                if (failHandlers)
                {
                    services.AddScoped<IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>, CountingFailingProbeHandler>();
                }
                else
                {
                    services.AddScoped<IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>, SinkingProbeHandler>();
                }
            })
            .Build();
    }

    private async Task EnsureBusinessAsync(string connectionString)
    {
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("store-alpha", "tenant-alpha"));
        await using var context = OutboxTestContextFactory.Create(connectionString, commerce);
        await context.Database.EnsureCreatedAsync();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > TimeSpan.FromSeconds(30))
            {
                throw new TimeoutException("Timed out waiting for MassTransit SQL Transport consumer.");
            }

            await Task.Delay(100);
        }
    }

    /// <summary>
    /// سینک thread-safe برای handler نمونه در تست transport.
    /// </summary>
    private sealed class ProbeHandlerSink
    {
        public ConcurrentBag<ProbeRecordCreatedIntegrationEvent> Received { get; } = [];
        public ConcurrentBag<string?> Tenants { get; } = [];
        public ConcurrentBag<string?> TraceIds { get; } = [];
        public ConcurrentBag<string?> HttpHosts { get; } = [];
        public int Failures;
    }

    /// <summary>
    /// handler Tooba بدون وابستگی MassTransit؛ Tenant را از زمینهٔ Assignشده می‌خواند.
    /// </summary>
    private sealed class SinkingProbeHandler : IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>
    {
        private readonly ProbeHandlerSink _sink;
        private readonly ICurrentTenant _tenant;
        private readonly IHttpContextAccessor _http;

        public SinkingProbeHandler(ProbeHandlerSink sink, ICurrentTenant tenant, IHttpContextAccessor http)
        {
            _sink = sink;
            _tenant = tenant;
            _http = http;
        }

        public Task HandleAsync(ProbeRecordCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            _sink.Received.Add(integrationEvent);
            _sink.Tenants.Add(_tenant.Current?.TenantId.Value);
            _sink.TraceIds.Add(Activity.Current?.TraceId.ToString());
            _sink.HttpHosts.Add(_http.HttpContext?.Request.Host.Value);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// شکست مصرف‌کننده برای اثبات جدا بودن retry transport از dead-letter Outbox.
    /// </summary>
    private sealed class CountingFailingProbeHandler : IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>
    {
        private readonly ProbeHandlerSink _sink;

        public CountingFailingProbeHandler(ProbeHandlerSink sink)
        {
            _sink = sink;
        }

        public Task HandleAsync(ProbeRecordCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _sink.Failures);
            throw new InvalidOperationException("probe-consumer-failed");
        }
    }

    /// <summary>
    /// Host جعلی bravo تا ثابت شود مصرف‌کننده Tenant را از HTTP Host نمی‌گیرد.
    /// </summary>
    private sealed class SpoofHostAccessor : IHttpContextAccessor
    {
        public SpoofHostAccessor()
        {
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("bravo.localhost");
            HttpContext = context;
        }

        public HttpContext? HttpContext { get; set; }
    }

    /// <summary>
    /// جمع‌آوری لاگ بدون payload و secret.
    /// </summary>
    private sealed class ListLoggerProvider : ILoggerProvider
    {
        public ConcurrentBag<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new ListLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class ListLogger(ConcurrentBag<string> messages) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                messages.Add(formatter(state, exception) + (exception is null ? string.Empty : " :: " + exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
