using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Wishlist.Application;
using Tooba.Wishlist.Infrastructure.Persistence;

namespace Tooba.Wishlist.Infrastructure;

/// <summary>ماژول مستقل Wishlist با schema، قرارداد و Outbox اختصاصی.</summary>
public sealed class WishlistModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Wishlist";
    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, WishlistOutboxRegistration>();
        services.AddScoped<IWishlistDirectory, WishlistDirectory>();
        services.AddDbContext<WishlistDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(sp.GetRequiredService<ICurrentCommerceContext>(), sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, WishlistDbContext.Schema, typeof(WishlistDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox Wishlist؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class WishlistOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => WishlistDbContext.Schema;
    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;
    /// <inheritdoc />
    public Type DbContextType => typeof(WishlistDbContext);
    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;
    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) => throw new InvalidOperationException("Wishlist integration event is not registered.");
    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
