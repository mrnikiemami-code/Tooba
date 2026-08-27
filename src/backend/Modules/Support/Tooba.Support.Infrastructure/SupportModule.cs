using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Support.Application;
using Tooba.Support.Infrastructure.Persistence;

namespace Tooba.Support.Infrastructure;

/// <summary>ماژول مستقل Support و schema اختصاصی آن.</summary>
public sealed class SupportModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Support";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, SupportOutboxRegistration>();
        services.AddScoped<ISupportDirectory, SupportDirectory>();
        services.AddDbContext<SupportDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, SupportDbContext.Schema, typeof(SupportDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox Support؛ رویداد بیرونی در این نسخه منتشر نمی‌شود.</summary>
public sealed class SupportOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => SupportDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(SupportDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("Support integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
