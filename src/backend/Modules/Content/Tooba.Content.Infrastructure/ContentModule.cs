using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>ماژول مستقل Content و schema اختصاصی آن.</summary>
public sealed class ContentModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Content";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, ContentOutboxRegistration>();
        services.AddScoped<IContentDirectory, ContentDirectory>();
        services.AddScoped<IContentCategoryDirectory, ContentCategoryDirectory>();
        services.AddDbContext<ContentDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(sp.GetRequiredService<ICurrentCommerceContext>(), sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, ContentDbContext.Schema, typeof(ContentDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox Content؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class ContentOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => ContentDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(ContentDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) => throw new InvalidOperationException("Content integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
