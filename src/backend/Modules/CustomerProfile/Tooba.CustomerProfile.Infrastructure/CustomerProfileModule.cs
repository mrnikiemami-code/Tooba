using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.CustomerProfile.Application;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.CustomerProfile.Infrastructure;

/// <summary>ماژول مستقل پروفایل توصیفی مشتری با schema، قرارداد و Outbox اختصاصی.</summary>
public sealed class CustomerProfileModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "CustomerProfile";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, CustomerProfileOutboxRegistration>();
        services.AddScoped<ICustomerProfileDirectory, CustomerProfileDirectory>();
        services.AddDbContext<CustomerProfileDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                CustomerProfileDbContext.Schema,
                typeof(CustomerProfileDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox پروفایل مشتری؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class CustomerProfileOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => CustomerProfileDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(CustomerProfileDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("CustomerProfile integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
