using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.ProductQnA.Application;
using Tooba.ProductQnA.Infrastructure.Persistence;

namespace Tooba.ProductQnA.Infrastructure;

/// <summary>ماژول مستقل ProductQnA و schema اختصاصی آن.</summary>
public sealed class ProductQnAModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "ProductQnA";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, ProductQnAOutboxRegistration>();
        services.AddScoped<IProductQaDirectory, ProductQaDirectory>();
        services.AddDbContext<ProductQnADbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(sp.GetRequiredService<ICurrentCommerceContext>(), sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, ProductQnADbContext.Schema, typeof(ProductQnADbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox ProductQnA؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class ProductQnAOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => ProductQnADbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(ProductQnADbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) => throw new InvalidOperationException("ProductQnA integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
