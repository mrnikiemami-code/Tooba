using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.PlatformProbe.Infrastructure;
using Tooba.PlatformProbe.Infrastructure.Events;
using Tooba.PlatformProbe.Infrastructure.Persistence;

namespace Tooba.Host.Tests;

/// <summary>
/// زمینهٔ تجارت ثابت برای تست interceptor بدون HTTP Host.
/// </summary>
internal sealed class FixedCommerceContext : ICurrentCommerceContext, ICurrentEdition, ICurrentTenant, ICommerceContextAssigner
{
    /// <inheritdoc />
    public CommerceContext? Current { get; private set; }

    /// <inheritdoc />
    EditionContext? ICurrentEdition.Current => Current?.Edition;

    /// <inheritdoc />
    TenantContext? ICurrentTenant.Current => Current?.Tenant;

    /// <inheritdoc />
    public void Assign(CommerceContext context) => Current = context;
}

/// <summary>
/// ثبت handler نمونه برای اثبات انتشار درون‌فرآیندی.
/// </summary>
internal sealed class RecordingProbeHandler : IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>
{
    /// <summary>
    /// رویدادهای دریافت‌شده به ترتیب انتشار.
    /// </summary>
    public List<ProbeRecordCreatedIntegrationEvent> Received { get; } = [];

    /// <summary>
    /// Tenant دیده‌شده از زمینهٔ کارگر در لحظهٔ Handle؛ نباید از Host بیاید.
    /// </summary>
    public List<string?> SeenTenantIds { get; } = [];

    private readonly ICurrentTenant _tenant;

    /// <summary>
    /// handler را به Tenant جاری scope وصل می‌کند.
    /// </summary>
    public RecordingProbeHandler(ICurrentTenant tenant)
    {
        _tenant = tenant;
    }

    /// <inheritdoc />
    public Task HandleAsync(ProbeRecordCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        Received.Add(integrationEvent);
        SeenTenantIds.Add(_tenant.Current?.TenantId.Value);
        return Task.CompletedTask;
    }
}

/// <summary>
/// handler شکست‌خورده برای retry و dead-letter.
/// </summary>
internal sealed class FailingProbeHandler : IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(ProbeRecordCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("probe-handler-failed");
}

/// <summary>
/// ساخت DbContext با interceptor Outbox روی یک connection string مشخص.
/// </summary>
internal static class OutboxTestContextFactory
{
    /// <summary>
    /// PlatformProbeDbContext با interceptor و زمینهٔ تجارت تزریقی.
    /// </summary>
    public static PlatformProbeDbContext Create(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PlatformProbeOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PlatformProbeDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            PlatformProbeDbContext.Schema,
            typeof(PlatformProbeDbContext));
        options.AddInterceptors(interceptor);
        return new PlatformProbeDbContext(options.Options);
    }

    /// <summary>
    /// زمینهٔ Single-Store نمونه برای Tenant داده‌شده.
    /// </summary>
    public static CommerceContext SingleStore(string tenantId, string connectionReference, string deploymentId = "test-outbox") =>
        new(
            new EditionContext(ToobaEdition.SingleStore, deploymentId),
            new TenantContext(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference(connectionReference),
                DisplayName: tenantId,
                ThemeReference: null,
                DefaultMarketReference: null,
                ResolvedHost: tenantId + ".localhost",
                PrimaryDomain: tenantId + ".localhost"),
            new ConnectionReference(connectionReference),
            TraceId: "trace-outbox");
}

/// <summary>
/// پیکربندی حداقل Single-Store با دو Tenant فعال برای تست کارگر.
/// </summary>
internal static class OutboxTestPlatform
{
    /// <summary>
    /// options تست با دو اتصال متمایز.
    /// </summary>
    public static ToobaPlatformOptions TwoTenants(string alphaCs, string bravoCs) => new()
    {
        Edition = "SingleStore",
        DeploymentId = "test-outbox",
        SingleStore = new SingleStoreOptions
        {
            Tenants =
            [
                new TenantRecordOptions
                {
                    TenantId = "store-alpha",
                    Status = "Active",
                    ConnectionReference = "tenant-alpha",
                    Hosts = ["alpha.localhost"],
                },
                new TenantRecordOptions
                {
                    TenantId = "store-bravo",
                    Status = "Active",
                    ConnectionReference = "tenant-bravo",
                    Hosts = ["bravo.localhost"],
                },
                new TenantRecordOptions
                {
                    TenantId = "store-disabled",
                    Status = "Disabled",
                    ConnectionReference = "tenant-disabled",
                    Hosts = ["disabled.localhost"],
                },
            ],
        },
        PostgreSQL = new PostgreSqlOptions
        {
            ConnectionReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant-alpha"] = alphaCs,
                ["tenant-bravo"] = bravoCs,
                ["tenant-disabled"] = "Host=127.0.0.1;Username=tooba;Password=x;Database=tooba_disabled",
            },
        },
    };

    /// <summary>
    /// options Marketplace فقط با یک پایگاه.
    /// </summary>
    public static ToobaPlatformOptions Marketplace(string cs) => new()
    {
        Edition = "Marketplace",
        DeploymentId = "test-marketplace-outbox",
        Marketplace = new MarketplaceOptions { ConnectionReference = "marketplace" },
        PostgreSQL = new PostgreSqlOptions
        {
            ConnectionReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["marketplace"] = cs,
            },
        },
    };

    /// <summary>
    /// ServiceProvider برای اجرای یک دور dispatcher.
    /// </summary>
    public static ServiceProvider BuildDispatcherServices(
        ToobaPlatformOptions options,
        bool failHandlers,
        RecordingProbeHandler? recording)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<ToobaPlatformOptions>>(Options.Create(options));
        services.AddSingleton(PlatformOptionsValidator.BuildRegistry(options));
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
            MaxAttempts = 2,
            RetryBaseDelaySeconds = 1,
            LockSeconds = 30,
            PollIntervalSeconds = 60,
        }));
        services.AddSingleton<BackgroundWorkerRegistry>();
        services.AddSingleton<OutboxDispatcher>();
        services.AddScoped<HttpCommerceContextAccessor>();
        services.AddScoped<ICurrentCommerceContext>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICurrentEdition>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddScoped<ICommerceContextAssigner>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
        services.AddHttpContextAccessor();
        services.AddScoped<IIntegrationEventPublisher, InProcessIntegrationEventPublisher>();
        if (failHandlers)
        {
            services.AddScoped<IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>, FailingProbeHandler>();
        }
        else if (recording is not null)
        {
            services.AddSingleton(recording);
            services.AddScoped<IIntegrationEventHandler<ProbeRecordCreatedIntegrationEvent>>(sp => recording);
        }

        return services.BuildServiceProvider();
    }
}
