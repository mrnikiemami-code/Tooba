using MassTransit;
using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// ترکیب MassTransit 8.5.10 + PostgreSQL SQL Transport پشت مرزهای Tooba. یک bus برای استقرار، نه per-tenant.
/// </summary>
internal static class MessagingRegistration
{
    /// <summary>
    /// نام پایدار receive endpoint مشترک استقرار؛ از hostname ماشین یا Tenant ساخته نمی‌شود.
    /// </summary>
    public const string IntegrationEndpointName = "tooba-integration";

    /// <summary>
    /// نام منبع Activity MassTransit 8.5.10 برای اتصال به OpenTelemetry موجود.
    /// </summary>
    public const string MassTransitActivitySource = "MassTransit";

    /// <summary>
    /// SQL Transport، migration زیرساخت، ناشر و مصرف‌کننده را ثبت می‌کند. EF Outbox اضافه نمی‌شود.
    /// </summary>
    public static IServiceCollection AddToobaMassTransitMessaging(this IServiceCollection services)
    {
        services.AddOptions<SqlTransportOptions>()
            .Configure<IDatabaseConnectionResolver, IOptions<MessagingHostOptions>>((sql, resolver, messaging) =>
            {
                var options = messaging.Value;
                var connectionString = resolver.Resolve(new ConnectionReference(options.ConnectionReference));
                SqlTransportOptionsMapper.Apply(sql, connectionString, options.Schema);
            });

        services.AddPostgresMigrationHostedService(options =>
        {
            options.CreateDatabase = false;
            options.CreateInfrastructure = true;
        });

        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.StopTimeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton(sp =>
        {
            var sql = sp.GetRequiredService<IOptions<SqlTransportOptions>>().Value;
            var connectionString = sql.ConnectionString
                ?? throw new InvalidOperationException("Messaging SQL Transport connection string is missing.");
            return new NpgsqlDataSourceBuilder(connectionString).Build();
        });

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ToobaIntegrationTransportConsumer>()
                .Endpoint(e => e.Name = IntegrationEndpointName);
            x.UsingPostgres(
                context => context.GetRequiredService<NpgsqlDataSource>(),
                (context, cfg) =>
                {
                    cfg.AutoStart = true;
                    cfg.UseMessageRetry(MessagingRetryConfigurator.ApplyConsumerRetry);
                    cfg.ConfigureEndpoints(context);
                });
        });

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        return services;
    }

    /// <summary>
    /// ناشر تولید یا دابل تست را بر اساس پیکربندی صریح انتخاب می‌کند. fallback خاموش ندارد.
    /// </summary>
    public static IServiceCollection AddToobaIntegrationPublisher(
        this IServiceCollection services,
        IHostEnvironment environment,
        MessagingHostOptions messaging)
    {
        if (messaging.UseInProcessTestDouble)
        {
            if (!environment.IsEnvironment("Testing"))
            {
                throw new InvalidOperationException(
                    "Tooba:Messaging:UseInProcessTestDouble is only allowed in the Testing environment.");
            }

            services.AddScoped<IIntegrationEventPublisher, InProcessIntegrationEventPublisher>();
            return services;
        }

        if (messaging.Enabled)
        {
            return services.AddToobaMassTransitMessaging();
        }

        services.AddScoped<IIntegrationEventPublisher, MessagingDisabledPublisher>();
        return services;
    }
}
