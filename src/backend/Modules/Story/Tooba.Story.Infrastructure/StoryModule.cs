using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Story.Application;
using Tooba.Story.Infrastructure.Persistence;

namespace Tooba.Story.Infrastructure;

/// <summary>ماژول مستقل Story و schema اختصاصی آن.</summary>
public sealed class StoryModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Story";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, StoryOutboxRegistration>();
        services.AddScoped<IStoryDirectory, StoryDirectory>();
        services.AddDbContext<StoryDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                StoryDbContext.Schema,
                typeof(StoryDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox Story؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class StoryOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => StoryDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(StoryDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("Story integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
