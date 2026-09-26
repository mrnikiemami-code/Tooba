using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.AddressBook.Application.Ports;
using Tooba.AddressBook.Infrastructure.Adapters;
using Tooba.AddressBook.Infrastructure.Outbox;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.AddressBook.Infrastructure.DependencyInjection;

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
