using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.OperatorProfile.Application;
using Tooba.OperatorProfile.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.OperatorProfile.Infrastructure;

/// <summary>ماژول مستقل پروفایل توصیفی اپراتور با schema، قرارداد و Outbox اختصاصی.</summary>
public sealed class OperatorProfileModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "OperatorProfile";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, OperatorProfileOutboxRegistration>();
        services.AddScoped<IOperatorProfileDirectory, OperatorProfileDirectory>();
        services.AddDbContext<OperatorProfileDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                OperatorProfileDbContext.Schema,
                typeof(OperatorProfileDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox پروفایل اپراتور؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class OperatorProfileOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => OperatorProfileDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(OperatorProfileDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("OperatorProfile integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
