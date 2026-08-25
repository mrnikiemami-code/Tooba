using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Reviews.Application;
using Tooba.Reviews.Infrastructure.Persistence;

namespace Tooba.Reviews.Infrastructure;

/// <summary>ماژول مستقل Reviews و schema اختصاصی آن.</summary>
public sealed class ReviewsModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Reviews";
    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, ReviewsOutboxRegistration>();
        services.AddScoped<IReviewDirectory, ReviewDirectory>();
        services.AddDbContext<ReviewsDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(sp.GetRequiredService<ICurrentCommerceContext>(), sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, ReviewsDbContext.Schema, typeof(ReviewsDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox Reviews؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class ReviewsOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => ReviewsDbContext.Schema;
    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;
    /// <inheritdoc />
    public Type DbContextType => typeof(ReviewsDbContext);
    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;
    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) => throw new InvalidOperationException("Reviews integration event is not registered.");
    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
