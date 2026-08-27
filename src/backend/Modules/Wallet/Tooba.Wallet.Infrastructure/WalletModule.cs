using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Wallet.Application;
using Tooba.Wallet.Infrastructure.Persistence;

namespace Tooba.Wallet.Infrastructure;

/// <summary>ماژول مستقل Wallet و schema اختصاصی آن.</summary>
public sealed class WalletModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Wallet";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, WalletOutboxRegistration>();
        services.AddScoped<IWalletDirectory, WalletDirectory>();
        services.AddDbContext<WalletDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, WalletDbContext.Schema, typeof(WalletDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox Wallet؛ رویداد بیرونی در این نسخه منتشر نمی‌شود.</summary>
public sealed class WalletOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => WalletDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(WalletDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("Wallet integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
