using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.AddressBook.Application;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.AddressBook.Infrastructure;

/// <summary>ماژول مستقل دفترچهٔ آدرس مشتری با schema، قرارداد و Outbox اختصاصی.</summary>
public sealed class AddressBookModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "AddressBook";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, AddressBookOutboxRegistration>();
        services.AddScoped<IAddressBookDirectory, AddressBookDirectory>();
        services.AddDbContext<AddressBookDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, AddressBookDbContext.Schema, typeof(AddressBookDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox دفترچهٔ آدرس؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class AddressBookOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => AddressBookDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(AddressBookDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("AddressBook integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
